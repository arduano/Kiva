using SharpDX.Direct3D9;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kiva_MIDI
{
    static class DXSharing
    {
        public static Texture GetSharedD3D9(this DeviceEx device, SharpDX.Direct3D11.Texture2D renderTarget)
        {
            if (renderTarget == null)
                return null;

            if ((renderTarget.Description.OptionFlags & SharpDX.Direct3D11.ResourceOptionFlags.Shared) == 0)
                throw new ArgumentException("Texture must be created with ResourceOptionFlags.Shared");

            Format format = ToD3D9(renderTarget.Description.Format);
            if (format == Format.Unknown)
                throw new ArgumentException("Texture format is not compatible with OpenSharedResource");

            using (var resource = renderTarget.QueryInterface<SharpDX.DXGI.Resource>())
            {
                IntPtr handle = resource.SharedHandle;
                if (handle == IntPtr.Zero)
                    throw new ArgumentNullException("Handle");
                return new Texture(device, renderTarget.Description.Width, renderTarget.Description.Height, 1, Usage.RenderTarget, format, Pool.Default, ref handle);
            }
        }

        public static SharpDX.Direct3D9.Format ToD3D9(this SharpDX.DXGI.Format dxgiformat)
        {
            switch (dxgiformat)
            {
                case SharpDX.DXGI.Format.R10G10B10A2_UNorm:
                    return SharpDX.Direct3D9.Format.A2B10G10R10;
                case SharpDX.DXGI.Format.B8G8R8A8_UNorm:
                    return SharpDX.Direct3D9.Format.A8R8G8B8;
                case SharpDX.DXGI.Format.R16G16B16A16_Float:
                    return SharpDX.Direct3D9.Format.A16B16G16R16F;

                // not sure those one below will work...

                case SharpDX.DXGI.Format.R32G32B32A32_Float:
                    return SharpDX.Direct3D9.Format.A32B32G32R32F;

                case SharpDX.DXGI.Format.R16G16B16A16_UNorm:
                    return SharpDX.Direct3D9.Format.A16B16G16R16;
                case SharpDX.DXGI.Format.R32G32_Float:
                    return SharpDX.Direct3D9.Format.G32R32F;

                case SharpDX.DXGI.Format.R8G8B8A8_UNorm:
                    return SharpDX.Direct3D9.Format.A8R8G8B8;

                case SharpDX.DXGI.Format.R16G16_UNorm:
                    return SharpDX.Direct3D9.Format.G16R16;

                case SharpDX.DXGI.Format.R16G16_Float:
                    return SharpDX.Direct3D9.Format.G16R16F;
                case SharpDX.DXGI.Format.R32_Float:
                    return SharpDX.Direct3D9.Format.R32F;

                case SharpDX.DXGI.Format.R16_Float:
                    return SharpDX.Direct3D9.Format.R16F;

                case SharpDX.DXGI.Format.A8_UNorm:
                    return SharpDX.Direct3D9.Format.A8;
                case SharpDX.DXGI.Format.R8_UNorm:
                    return SharpDX.Direct3D9.Format.L8;

                default:
                    return SharpDX.Direct3D9.Format.Unknown;
            }
        }
    }
}
