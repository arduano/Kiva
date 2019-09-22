using System;
using System.Runtime.InteropServices;
using SharpDX.Direct3D9;

namespace Kiva_MIDI
{
	public class D3D9 : D3D
	{
		protected D3D9(bool b) { /* do nothing constructor */ }

		public D3D9()
			: this(null)
		{
		}

		public D3D9(DeviceEx device) 
		{
			if (device != null)
			{
				//context = ???
				throw new NotSupportedException("dunno how to get the context");

				//this.device = device;
				//device.AddReference();
			}
			else
			{
				context = new Direct3DEx();

				PresentParameters presentparams = new PresentParameters();
				presentparams.Windowed = true;
				presentparams.SwapEffect = SwapEffect.Discard;
				presentparams.DeviceWindowHandle = GetDesktopWindow();
				presentparams.PresentationInterval = PresentInterval.Default;
				this.device = new DeviceEx(context, 0, DeviceType.Hardware, IntPtr.Zero, CreateFlags.HardwareVertexProcessing | CreateFlags.Multithreaded | CreateFlags.FpuPreserve, presentparams);
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				Set(ref device, null);
				Set(ref context, null);
			}
		}

		public bool IsDisposed { get { return device == null; } }

		[DllImport("user32.dll", SetLastError = false)]
		static extern IntPtr GetDesktopWindow();

		protected Direct3DEx context;
		protected DeviceEx device;

		public DeviceEx Device { get { return device.GetOrThrow(); } }

		Texture renderTarget;

		public override void Reset(int w, int h)
		{
			device.GetOrThrow();

			if (w < 1)
				throw new ArgumentOutOfRangeException("w");
			if (h < 1)
				throw new ArgumentOutOfRangeException("h");

			Set(ref renderTarget, new Texture(this.device, w, h, 1, Usage.RenderTarget, Format.A8R8G8B8, Pool.Default));

			// TODO test that...
			using (var surface = renderTarget.GetSurfaceLevel(0))
				device.SetRenderTarget(0, surface);
		}

		protected T Prepared<T>(ref T property)
		{
			device.GetOrThrow();
			if (property == null)
				Reset(1, 1);
			return property;
		}

		public Texture RenderTarget { get { return Prepared(ref renderTarget); } }

		public override void SetBackBuffer(DXImageSource dximage) { dximage.SetBackBuffer(RenderTarget); }

		public override System.Windows.Media.Imaging.WriteableBitmap ToImage() { throw new NotImplementedException(); }
	}
}
