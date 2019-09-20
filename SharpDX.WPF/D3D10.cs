using System;
using System.Collections.Generic;
using SharpDX.Direct3D10;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D10.Device;

namespace SharpDX.WPF
{
	public class D3D10 : D3D
	{
		protected Device device;

		protected D3D10(bool b) { /* do nothing constructor */ }

		public D3D10()
			: this(null)
		{
		}

		public D3D10(Direct3D10.FeatureLevel minLevel)
		{
			device = DeviceUtil.Create10(DeviceCreationFlags.BgraSupport, minLevel);
			if (device == null)
				throw new NotSupportedException();
		}

		public D3D10(Device dev)
		{
			if (dev != null)
			{
				dev.AddReference();
				this.device = dev;
			}
			else
			{
				device = DeviceUtil.Create10(DeviceCreationFlags.BgraSupport);
				if (device == null)
					throw new NotSupportedException();
			}
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			// NOTE: SharpDX 1.3 requires explicit Dispose() of everything
			Set(ref device, null);
			Set(ref renderTarget, null);
			Set(ref renderTargetView, null);
		}

		public Device Device { get { return device.GetOrThrow(); } }

		public bool IsDisposed { get { return device == null; } }

		public  override void SetBackBuffer(DXImageSource dximage) { dximage.SetBackBuffer(RenderTarget); }

		protected Texture2D renderTarget;
		protected RenderTargetView renderTargetView;
		protected Texture2D depthStencil;
		protected DepthStencilView depthStencilView;

		public override void Reset(int w, int h)
		{
			device.GetOrThrow();

			if (w < 1)
				throw new ArgumentOutOfRangeException("w");
			if (h < 1)
				throw new ArgumentOutOfRangeException("h");

			var colordesc = new Texture2DDescription
			{
				BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
				Format = Format.B8G8R8A8_UNorm,
				Width = w,
				Height = h,
				MipLevels = 1,
				SampleDescription = new SampleDescription(1, 0),
				Usage = ResourceUsage.Default,
				OptionFlags = ResourceOptionFlags.Shared,
				CpuAccessFlags = CpuAccessFlags.None,
				ArraySize = 1
			};

			Set(ref renderTarget, new Texture2D(this.device, colordesc));
			Set(ref renderTargetView, new RenderTargetView(this.device, this.renderTarget));

			Set(ref depthStencil, DXUtils.CreateTexture2D(this.device, w, h, BindFlags.DepthStencil, Format.D24_UNorm_S8_UInt));
			Set(ref depthStencilView, new DepthStencilView(this.device, depthStencil));

			device.Rasterizer.SetViewports(new Viewport(0, 0, w, h, 0.0f, 1.0f));
			device.OutputMerger.SetRenderTargets(1, new RenderTargetView[] { renderTargetView }, depthStencilView);
		}

		public override void BeginRender(DrawEventArgs args)
		{
			device.GetOrThrow();
			device.ClearDepthStencilView(this.DepthStencilView, DepthStencilClearFlags.Depth, 1.0f, 0);
		}

		public override void EndRender(DrawEventArgs args)
		{
			Device.Flush();
		}

		protected T Prepared<T>(ref T property)
		{
			device.GetOrThrow();
			if (property == null)
				Reset(1, 1);
			return property;
		}

		public Texture2D RenderTarget { get { return Prepared(ref renderTarget); } }
		public RenderTargetView RenderTargetView { get { return Prepared(ref renderTargetView); } }

		public Texture2D DepthStencil { get { return Prepared(ref depthStencil); } }
		public DepthStencilView DepthStencilView { get { return Prepared(ref depthStencilView); } }

		public override System.Windows.Media.Imaging.WriteableBitmap ToImage() { return RenderTarget.GetBitmap(); }
	}
}
