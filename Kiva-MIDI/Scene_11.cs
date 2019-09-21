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
    struct RenderCol
    {
        public float r, g, b, a;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct RenderPos
    {
        public float x, y, z, w;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct RenderQuad
    {
        public RenderPos pos1;
        public RenderCol col1;
        public RenderPos pos2;
        public RenderCol col2;
        public RenderPos pos3;
        public RenderCol col3;
        public RenderPos pos4;
        public RenderCol col4;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct RenderNote
    {
        public float start;
        public float end;
        public NoteCol color;
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
        ShaderBytecode geometryShaderByteCode;
        GeometryShader geometryShader;
        InputLayout layout;
        Buffer globalConstants;

        protected void Attach()
        {
            if (Renderer == null)
                return;
            var device = Renderer.Device;
            var context = device.ImmediateContext;

            // Compile Vertex and Pixel shaders
            vertexShaderByteCode = ShaderBytecode.CompileFromFile("MiniTri.fx", "VS_Note", "vs_4_0", ShaderFlags.None, EffectFlags.None);
            vertexShader = new VertexShader(device, vertexShaderByteCode);
            pixelShaderByteCode = ShaderBytecode.CompileFromFile("MiniTri.fx", "PS", "ps_4_0", ShaderFlags.None, EffectFlags.None);
            pixelShader = new PixelShader(device, pixelShaderByteCode);
            geometryShaderByteCode = ShaderBytecode.CompileFromFile("MiniTri.fx", "GS_Note", "gs_4_0", ShaderFlags.None, EffectFlags.None);
            geometryShader = new GeometryShader(device, geometryShaderByteCode);

            // Layout from VertexShader input signature
            //layout = new InputLayout(device, ShaderSignature.GetInputSignature(vertexShaderByteCode), new[] {
            //    new InputElement("POSITION",0,Format.R32G32B32A32_Float,0,0),
            //    new InputElement("COLOR",0,Format.R32G32B32A32_Float,16,0)
            //});
            layout = new InputLayout(device, ShaderSignature.GetInputSignature(vertexShaderByteCode), new[] {
                new InputElement("START",0,Format.R32_Float,0,0),
                new InputElement("END",0,Format.R32_Float,4,0),
                new InputElement("COLORL",0,Format.R32G32B32A32_Float,8,0),
                new InputElement("COLORR",0,Format.R32G32B32A32_Float,24,0),
            });

            // Write vertex data to a datastream
            var stream = new DataStream(32 * 4, true, true);

            //var quad = new RenderQuad();
            //quad.pos1 = new RenderPos() { x = 0.0f, y = 0.5f, z = 0f, w = 1.0f };
            //quad.pos2 = new RenderPos() { x = 0.5f, y = -0.5f, z = 0f, w = 1.0f };
            //quad.pos3 = new RenderPos() { x = -0.5f, y = -0.5f, z = 0f, w = 1.0f };
            //quad.pos4 = new RenderPos() { x = -0.5f, y = 0.5f, z = 0f, w = 1.0f };
            //quad.col1 = new RenderCol() { r = 1, g = 1, b = 0, a = 1 };
            //quad.col2 = new RenderCol() { r = 1, g = 1, b = 0, a = 1 };
            //quad.col3 = new RenderCol() { r = 1, g = 1, b = 0, a = 1 };
            //quad.col4 = new RenderCol() { r = 0, g = 1, b = 0, a = 1 };
            //stream.Write(quad);

            var note = new RenderNote()
            {
                start = -0.5f,
                end = 0.5f,
                color = new NoteCol() {
                    r = 1, g = 0, b = 1, a = 1,
                    r2 = 0, g2 = 1, b2 = 1, a2 = 1
                }
            };
            stream.Write(note);

            //stream.WriteRange(new[]
            //                      {
            //                          new Vector4(0.0f, 0.5f, 0f, 1.0f), new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
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
                SizeInBytes = 32 * 4,
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
            context.InputAssembler.SetPrimitiveTopology(PrimitiveTopology.PointList);
            context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertices, 12, 0));
            context.VertexShader.Set(vertexShader);
            context.GeometryShader.Set(geometryShader);
            context.PixelShader.Set(pixelShader);
        }

        protected void Detach()
        {
            Disposer.SafeDispose(ref vertexShaderByteCode);
            Disposer.SafeDispose(ref vertexShader);
            Disposer.SafeDispose(ref pixelShaderByteCode);
            Disposer.SafeDispose(ref pixelShader);
            Disposer.SafeDispose(ref geometryShaderByteCode);
            Disposer.SafeDispose(ref geometryShader);
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
            context.Draw(1, 0);
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
