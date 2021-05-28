using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;
using SharpDX.Direct3D;

namespace Kiva
{
    class ShaderManager : IDisposable
    {
        public ShaderBytecode vertexShaderByteCode;
        public VertexShader vertexShader;
        public ShaderBytecode pixelShaderByteCode;
        public PixelShader pixelShader;
        public ShaderBytecode geometryShaderByteCode;
        public GeometryShader geometryShader;

        DisposeGroup dispose = new DisposeGroup();

        public ShaderManager(Device device, ShaderBytecode vertexShaderByteCode, ShaderBytecode pixelShaderByteCode, ShaderBytecode geometryShaderByteCode)
        {
            this.vertexShaderByteCode = dispose.Add(vertexShaderByteCode);
            this.pixelShaderByteCode = dispose.Add(pixelShaderByteCode);
            this.geometryShaderByteCode = dispose.Add(geometryShaderByteCode);
            vertexShader = dispose.Add(new VertexShader(device, vertexShaderByteCode));
            pixelShader = dispose.Add(new PixelShader(device, pixelShaderByteCode));
            geometryShader = dispose.Add(new GeometryShader(device, geometryShaderByteCode));
        }

        public void SetShaders(DeviceContext ctx)
        {
            ctx.VertexShader.Set(vertexShader);
            ctx.PixelShader.Set(pixelShader);
            ctx.GeometryShader.Set(geometryShader);
        }

        public void Dispose()
        {
            dispose.Dispose();
        }
    }
}
