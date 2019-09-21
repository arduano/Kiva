using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.WPF;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;
using SharpDX.Direct3D;

namespace Kiva_MIDI
{
    class ShaderManager : IDisposable
    {
        public ShaderBytecode vertexShaderByteCode;
        public VertexShader vertexShader;
        public ShaderBytecode pixelShaderByteCode;
        public PixelShader pixelShader;
        public ShaderBytecode geometryShaderByteCode;
        public GeometryShader geometryShader;

        public ShaderManager(Device device, ShaderBytecode vertexShaderByteCode, ShaderBytecode pixelShaderByteCode, ShaderBytecode geometryShaderByteCode)
        {
            this.vertexShaderByteCode = vertexShaderByteCode;
            this.pixelShaderByteCode = pixelShaderByteCode;
            this.geometryShaderByteCode = geometryShaderByteCode;
            vertexShader = new VertexShader(device, vertexShaderByteCode);
            pixelShader = new PixelShader(device, pixelShaderByteCode);
            geometryShader = new GeometryShader(device, geometryShaderByteCode);
        }

        public void SetShaders(DeviceContext ctx)
        {
            ctx.VertexShader.Set(vertexShader);
            ctx.PixelShader.Set(pixelShader);
            ctx.GeometryShader.Set(geometryShader);
        }

        public void Dispose()
        {
            Disposer.SafeDispose(ref vertexShaderByteCode);
            Disposer.SafeDispose(ref vertexShader);
            Disposer.SafeDispose(ref pixelShaderByteCode);
            Disposer.SafeDispose(ref pixelShader);
            Disposer.SafeDispose(ref geometryShaderByteCode);
            Disposer.SafeDispose(ref geometryShader);
        }
    }
}
