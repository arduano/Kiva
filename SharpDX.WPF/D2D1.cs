using System.Collections.Generic;
using SharpDX.Direct2D1;
using SharpDX.DXGI;
using FactoryDW = SharpDX.DirectWrite.Factory;
using Factory = SharpDX.Direct2D1.Factory;
namespace SharpDX.WPF
{
	/// <summary>
	/// This supports both D3D10 and D2D1 rendering
	/// </summary>
	public class D2D1 : D3D10
	{
		Factory factory2D;
		FactoryDW factoryDW;

		public D2D1()
			: this(null)
		{
		}

		public D2D1(SharpDX.Direct3D10.Device1 device)
			: base(device)
		{
			factory2D = new SharpDX.Direct2D1.Factory();
			factoryDW = new FactoryDW();
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			// NOTE: SharpDX 1.3 requires explicit Dispose() of everything
			Set(ref renderTarget2D, null);
			Set(ref factory2D, null);
			Set(ref factoryDW, null);
		}

		public FactoryDW FactoryDW { get { return factoryDW; } }

		RenderTarget renderTarget2D;

		public override void Reset(int w, int h)
		{
			base.Reset(w, h);

			using(Surface surface = RenderTarget.QueryInterface<Surface>())
				Set(ref renderTarget2D, new RenderTarget(
					factory2D, 
					surface,
					new RenderTargetProperties(new PixelFormat(Format.Unknown, AlphaMode.Premultiplied))
				));
			renderTarget2D.AntialiasMode = AntialiasMode.PerPrimitive;
			Device.OutputMerger.SetTargets(RenderTargetView);
		}

		public override void BeginRender(DrawEventArgs args)
		{
			base.BeginRender(args);
			renderTarget2D.BeginDraw();
		}

		public override void EndRender(DrawEventArgs args)
		{
			renderTarget2D.EndDraw();
			base.EndRender(args);
		}

		public RenderTarget RenderTarget2D { get { return Prepared(ref renderTarget2D); } }
	}
}
