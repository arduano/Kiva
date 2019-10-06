using System;
using System.Collections.Generic;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;
using SharpDX.Mathematics.Interop;

namespace Kiva_MIDI
{
    public class D3D11 : D3D
    {
        protected Device device;

        protected D3D11(bool b) { /* do nothing constructor */ }

        public D3D11(FeatureLevel minLevel)
        {
            device = DeviceUtil.Create11(DeviceCreationFlags.BgraSupport, minLevel);
            if (device == null)
                throw new NotSupportedException();
        }

        public D3D11()
        {
            Device dev = null;
            device = DeviceUtil.Create11(DeviceCreationFlags.BgraSupport);
            if (device == null)
                throw new NotSupportedException();
        }

        public D3D11(Adapter a)
        {
            if (a == null)
            {
                device = DeviceUtil.Create11(DeviceCreationFlags.BgraSupport, FeatureLevel.Level_11_0);
                if (device == null)
                    throw new NotSupportedException();
            }
            device = new Device(a);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            // NOTE: SharpDX 1.3 requires explicit Dispose() of everything
            Set(ref device, null);
            Set(ref renderTarget, null);
            Set(ref renderTargetView, null);
            //Set(ref depthStencil, null);
            //Set(ref depthStencilView, null);
        }

        public Device Device { get { return device.GetOrThrow(); } }

        public bool IsDisposed { get { return device == null; } }

        public override void SetBackBuffer(DXImageSource dximage) { dximage.SetBackBuffer(RenderTarget); }

        protected Texture2D renderTarget;
        protected RenderTargetView renderTargetView;
        protected Texture2D renderTargetCopy;
        //protected Texture2D depthStencil;
        //protected DepthStencilView depthStencilView;

        #region RenderTargetOptionFlags

        public ResourceOptionFlags RenderTargetOptionFlags
        {
            get { return mRenderTargetOptionFlags; }
            set
            {
                if (value == mRenderTargetOptionFlags)
                    return;
                mRenderTargetOptionFlags = value;
            }
        }
        // must be shared to be displayed in a D3DImage
        ResourceOptionFlags mRenderTargetOptionFlags = ResourceOptionFlags.Shared;

        #endregion

        public override void Reset(int w, int h)
        {
            device.GetOrThrow();

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
                OptionFlags = RenderTargetOptionFlags,
                CpuAccessFlags = CpuAccessFlags.None,
                ArraySize = 1
            };
            Set(ref renderTarget, new Texture2D(this.device, desc));
            Set(ref renderTargetView, new RenderTargetView(this.device, this.renderTarget));
            Set(ref renderTargetCopy, new Texture2D(this.device, desc));

            //Set(ref depthStencil, DXUtils.CreateTexture2D(this.device, w, h, BindFlags.DepthStencil, Format.D24_UNorm_S8_UInt));
            //Set(ref depthStencilView, new DepthStencilView(this.device, depthStencil));
            //device.ImmediateContext.OutputMerger.SetRenderTargets(depthStencilView, renderTargetView);
        }

        public override void BeginRender(DrawEventArgs args)
        {
            device.ImmediateContext.Rasterizer.SetViewports(new RawViewportF[] { new RawViewportF() {
                X = 0,
                Y = 0,
                Width = (int)args.RenderSize.Width,
                Height = (int)args.RenderSize.Height,
                MinDepth = 0.0f,
                MaxDepth = 1.0f
            } });

            device.ImmediateContext.OutputMerger.SetRenderTargets(renderTargetView);

            device.GetOrThrow();
            //Device.ImmediateContext.ClearDepthStencilView(this.DepthStencilView, DepthStencilClearFlags.Depth, 1.0f, 0);
        }

        public override void EndRender(DrawEventArgs args)
        {
            Device.ImmediateContext.Flush();
            Device.ImmediateContext.CopyResource(renderTarget, renderTargetCopy);
        }

        protected T Prepared<T>(ref T property)
        {
            device.GetOrThrow();
            if (property == null)
                Reset(1, 1);
            return property;
        }

        public Texture2D RenderTarget { get { return Prepared(ref renderTargetCopy); } }
        public RenderTargetView RenderTargetView { get { return Prepared(ref renderTargetView); } }

        //public Texture2D DepthStencil { get { return Prepared(ref depthStencil); } }
        //public DepthStencilView DepthStencilView { get { return Prepared(ref depthStencilView); } }

        public override System.Windows.Media.Imaging.WriteableBitmap ToImage() { return null; }
    }
}
