#if IT_WOULD_ONLY_WORKS
using System.Collections.Generic;
using SharpDX.Direct2D1;
using SharpDX.DXGI;
using SharpDX.Direct3D11;
using System;

using FactoryDW = SharpDX.DirectWrite.Factory;
using Factory2D = SharpDX.Direct2D1.Factory;
using Device11 = SharpDX.Direct3D11.Device;
using Device10 = SharpDX.Direct3D10.Device1;
using DeviceXGI = SharpDX.DXGI.Device;
using RenderTarget2D = SharpDX.Direct2D1.RenderTarget;

namespace SharpDX.WPF
{
	/// <summary>
	/// This supports both D3D11 and D2D1 rendering
	/// </summary>
	public class D3D11_2D1 : D3D11
	{
		Device10 device10;
		Factory2D factory2D;
		FactoryDW factoryDW;

		#region ctor()

		public D3D11_2D1()
			: this(null, null)
		{
		}

		public D3D11_2D1(Adapter a)
			: base(false) // nothing!
		{
			device = new Device11(a, DeviceCreationFlags.BgraSupport | DeviceCreationFlags.SingleThreaded | DeviceCreationFlags.Debug);
			device10 = new Device10(a, Direct3D10.DeviceCreationFlags.BgraSupport | Direct3D10.DeviceCreationFlags.Singlethreaded | Direct3D10.DeviceCreationFlags.Debug);
		}

		public static Adapter GetCompatibleAdapter(DisposeGroup dg)
		{
			foreach (var item in DeviceUtil.GetAdapters(dg))
			{
				if (Device11.GetSupportedFeatureLevel(item) < Direct3D.FeatureLevel.Level_10_1)
					continue;
				if (!item.IsInterfaceSupported<Device10>())
					continue;
				return item;
			}
			return null;
		}

		public D3D11_2D1(Device11 drawdevice, Device10 textdevice)
			: base(false) // nothing!
		{
			if (drawdevice == null || textdevice == null)
			{
				using (var dg = new DisposeGroup())
				{
					if (drawdevice == null && textdevice == null)
					{
						Adapter a = null;
						foreach (var item in DeviceUtil.GetAdapters(dg))
						{
							if (!item.IsInterfaceSupported<Device10>())
								continue;
							if (Device11.GetSupportedFeatureLevel(item) < Direct3D.FeatureLevel.Level_10_1)
								continue;
							a = item;
							break;
						}
						device = new Device11(a, DeviceCreationFlags.BgraSupport | DeviceCreationFlags.SingleThreaded | DeviceCreationFlags.Debug);
						device10 = new Device10(a, Direct3D10.DeviceCreationFlags.BgraSupport | Direct3D10.DeviceCreationFlags.Singlethreaded | Direct3D10.DeviceCreationFlags.Debug);
					}
					else
					{
						if (drawdevice == null)
						{
							using (var xgidtext = textdevice.QueryInterface<DeviceXGI>())
								device = new Device11(xgidtext.Adapter, DeviceCreationFlags.BgraSupport | DeviceCreationFlags.SingleThreaded | DeviceCreationFlags.Debug);
							textdevice.AddReference();
							device10 = textdevice;
						}
						else
						{
							using (var xgiddraw = drawdevice.QueryInterface<DeviceXGI>())
								device10 = new Device10(xgiddraw.Adapter, Direct3D10.DeviceCreationFlags.BgraSupport | Direct3D10.DeviceCreationFlags.Singlethreaded | Direct3D10.DeviceCreationFlags.Debug);
							drawdevice.AddReference();
							device = drawdevice;
						}
					}
				}
			}
			else
			{
				using (var xgidev10 = device10.QueryInterface<DeviceXGI>())
				using (var xgidev11 = device.QueryInterface<DeviceXGI>())
				{
					if (xgidev10.Adapter.NativePointer != xgidev11.Adapter.NativePointer)
						throw new ArgumentException("drawdevice.Adapter.NativePointer != textdevice.Adapter.NativePointer");
				}
				textdevice.AddReference();
				drawdevice.AddReference();
				device = drawdevice;
				device10 = textdevice;
			}
			
			factory2D = new SharpDX.Direct2D1.Factory();
			factoryDW = new FactoryDW();
		}

		#endregion

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			// NOTE: SharpDX 1.3 requires explicit Dispose() of everything
			Set(ref device10, null);
			Set(ref factory2D, null);
			Set(ref factoryDW, null);
			Set(ref renderTarget2D, null);
		}

		public Device10 Device2D { get { return device10; } }
		public FactoryDW FactoryDW { get { return factoryDW; } }

		RenderTarget renderTarget2D;

		public override void Reset(int w, int h)
		{
			//base.Reset(w, h); // no base.Reset() let's do it our self!

			// work in progress... inspired by
			// http://www.gamedev.net/topic/547920-how-to-use-d2d-with-d3d11/

			if (w < 1)
				throw new ArgumentOutOfRangeException("w");
			if (h < 1)
				throw new ArgumentOutOfRangeException("h");

			var desc = new Texture2DDescription
			{
				BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
				Format = Format.B8G8R8A8_UNorm,
				Width = w,
				Height = h,
				MipLevels = 1,
				SampleDescription = new SampleDescription(1, 0),
				Usage = ResourceUsage.Default,
				OptionFlags = ResourceOptionFlags.SharedKeyedmutex,
				CpuAccessFlags = CpuAccessFlags.None,
				ArraySize = 1
			};
			Set(ref renderTarget, new Texture2D(this.Device, desc));
			Set(ref renderTargetView, new RenderTargetView(this.Device, this.renderTarget));

			using (var res11 = RenderTarget.QueryInterface<SharpDX.DXGI.Resource>())
			using (var res10 = device10.OpenSharedResource<SharpDX.DXGI.Resource>(res11.SharedHandle))
			using (var surface = res10.QueryInterface<Surface>())
			{
				Set(ref renderTarget2D, new RenderTarget(
					factory2D,
					surface,
					new RenderTargetProperties(new PixelFormat(Format.Unknown, AlphaMode.Premultiplied))
				));
			}
			renderTarget2D.AntialiasMode = AntialiasMode.PerPrimitive;

			Device.ImmediateContext.Rasterizer.SetViewports(new Viewport(0, 0, w, h, 0.0f, 1.0f));
			Device.ImmediateContext.OutputMerger.SetTargets(RenderTargetView);
		}

		public override void BeginRender(RenderArgs args)
		{
			renderTarget2D.BeginDraw();
		}

		public override void EndRender(RenderArgs args)
		{
			renderTarget2D.EndDraw();
		}

		public RenderTarget2D RenderTarget2D { get { return Prepared(ref renderTarget2D); } }

		public override System.Windows.Media.Imaging.WriteableBitmap ToImage() { return RenderTarget.GetBitmap(); }
		public override void SetBackBuffer(DXImageSource dximage) { dximage.SetBackBuffer(RenderTarget); }
	}
}
#endif