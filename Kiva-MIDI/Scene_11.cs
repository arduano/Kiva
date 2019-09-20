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
using System.Runtime.InteropServices;

namespace Kiva_MIDI
{
    [StructLayout(LayoutKind.Sequential, Pack = 16)]
    struct GlobalConstants
    {
        public float Spin;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct RenderQuad
    {
        public float x1, y1, z1, w1;
        public float r1, g1, b1, a1;
        public float x2, y2, z2, w2;
        public float r2, g2, b2, a2;
        public float x3, y3, z3, w3;
        public float r3, g3, b3, a3;
        //public float x4, y4, z4, w4;
        //public float r4, g4, b4, a4;
    }

    public class Scene_11 : IDirect3D
    {
        public virtual D3D11 Renderer
        {
            get { return context; }
            set
            {
                if (Renderer != null)
                {
                    Renderer.Rendering -= ContextRendering;
                    Detach();
                }
                context = value;
                if (Renderer != null)
                {
                    Renderer.Rendering += ContextRendering;
                    Attach();
                }
            }
        }
        D3D11 context;

        void ContextRendering(object aCtx, DrawEventArgs args) { RenderScene(args); }
        Buffer vertices;
        ShaderBytecode vertexShaderByteCode;
        VertexShader vertexShader;
        ShaderBytecode pixelShaderByteCode;
        PixelShader pixelShader;
        InputLayout layout;
        Buffer globalConstants;

        protected void Attach()
        {
            if (Renderer == null)
                return;
            var device = Renderer.Device;
            var context = device.ImmediateContext;

            // Compile Vertex and Pixel shaders
            vertexShaderByteCode = ShaderBytecode.CompileFromFile("MiniTri.fx", "VS", "vs_4_0", ShaderFlags.None, EffectFlags.None);
            vertexShader = new VertexShader(device, vertexShaderByteCode);
            pixelShaderByteCode = ShaderBytecode.CompileFromFile("MiniTri.fx", "PS", "ps_4_0", ShaderFlags.None, EffectFlags.None);
            pixelShader = new PixelShader(device, pixelShaderByteCode);

            // Layout from VertexShader input signature
            layout = new InputLayout(device, ShaderSignature.GetInputSignature(vertexShaderByteCode), new[] {
                new InputElement("POSITION",0,Format.R32G32B32A32_Float,0,0),
                new InputElement("COLOR",0,Format.R32G32B32A32_Float,16,0)
            });

            // Write vertex data to a datastream
            var stream = new DataStream(32 * 4, true, true);

            var quad = new RenderQuad();
            quad.x1 = 0.0f; quad.y1 = 0.5f; quad.z1 = 0.0f; quad.w1 = 1.0f;
            quad.x2 = 0.5f; quad.y2 = -0.5f; quad.z2 = 0.0f; quad.w2 = 1.0f;
            quad.x3 = -0.5f; quad.y3 = -0.5f; quad.z3 = 0.0f; quad.w3 = 1.0f;
            //quad.x4 = 0.5f; quad.y4 = 0.5f; quad.z4 = 0.0f; quad.w4 = 1.0f;
            quad.r1 = 1; quad.g1 = 1; quad.b1 = 0; quad.a1 = 1;
            quad.r2 = 1; quad.g2 = 1; quad.b2 = 0; quad.a2 = 1;
            quad.r3 = 1; quad.g3 = 1; quad.b3 = 0; quad.a3 = 1;
            //quad.r4 = 1; quad.g4 = 1; quad.b4 = 0; quad.a4 = 1;

            stream.Write(quad);
            //stream.Write(new RenderPos() { x = 0.0f, y = 0.5f, z = 0f, w = 1.0f });
            //stream.WriteRange(new[]
            //                      {
            //                          new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
            //                          new Vector4(0.5f, -0.5f, 0f, 1.0f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f),
            //                          new Vector4(-0.5f, -0.5f, 0f, 1.0f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f)
            //                      });
            stream.Position = 0;

            // Instantiate Vertex buiffer from vertex data 
            vertices = new Buffer(device, stream, new BufferDescription()
            {
                BindFlags = BindFlags.VertexBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None,
                SizeInBytes = 32 * 3,
                Usage = ResourceUsage.Default,
                StructureByteStride = 0
            });
            stream.Release();

            stream = new DataStream(16, true, true);
            stream.Write(1.0f);
            globalConstants = new Buffer(Renderer.Device, stream, new BufferDescription()
            {
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.Write,
                OptionFlags = ResourceOptionFlags.None,
                SizeInBytes = 16,
                Usage = ResourceUsage.Dynamic,
                StructureByteStride = 0
            });
            stream.Release();

            // Prepare All the stages
            context.InputAssembler.SetInputLayout(layout);
            context.InputAssembler.SetPrimitiveTopology(PrimitiveTopology.TriangleList);
            context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertices, 32, 0));
            context.VertexShader.Set(vertexShader);
            context.PixelShader.Set(pixelShader);
        }

        protected void Detach()
        {
            Disposer.SafeDispose(ref vertexShaderByteCode);
            Disposer.SafeDispose(ref vertexShader);
            Disposer.SafeDispose(ref pixelShaderByteCode);
            Disposer.SafeDispose(ref pixelShader);
            Disposer.SafeDispose(ref vertices);
            Disposer.SafeDispose(ref layout);
            Disposer.SafeDispose(ref globalConstants);
        }

        public void RenderScene(DrawEventArgs args)
        {
            var context = Renderer.Device.ImmediateContext;

            var data = context.MapSubresource(globalConstants, 0, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None).Data;
            data.Position = 0;
            data.Write(new GlobalConstants() { Spin = args.TotalTime.Ticks * 0.0000001f });
            context.UnmapSubresource(globalConstants, 0);
            context.VertexShader.SetConstantBuffer(0, globalConstants);

            context.ClearRenderTargetView(Renderer.RenderTargetView, new Color4(0.6f, 0, 0, 0));
            context.Draw(3, 0);
        }

        void IDirect3D.Reset(DrawEventArgs args)
        {
            if (Renderer != null)
                Renderer.Reset(args);
        }

        void IDirect3D.Render(DrawEventArgs args)
        {
            if (Renderer != null)
                Renderer.Render(args);
        }
    }
}
