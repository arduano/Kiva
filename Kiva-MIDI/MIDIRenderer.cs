using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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
    class MIDIRenderer : IDisposable
    {
        [StructLayout(LayoutKind.Sequential, Pack = 16)]
        struct NotesGlobalConstants
        {
            public float NoteLeft;
            public float NoteRight;
            public float NoteBorder;
            public float ScreenAspect;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct RenderNote
        {
            public float start;
            public float end;
            public NoteCol color;
        }

        public MIDIFile File { get; set; }
        public PlayingState Time { get; set; }

        ShaderManager notesShader;
        InputLayout noteLayout;
        InputLayout keyLayout;
        Buffer globalNoteConstants;
        Buffer noteBuffer;

        public MIDIRenderer(Device device)
        {
            notesShader = new ShaderManager(
                device,
                ShaderBytecode.CompileFromFile("Notes.fx", "VS_Note", "vs_4_0", ShaderFlags.None, EffectFlags.None),
                ShaderBytecode.CompileFromFile("Notes.fx", "PS", "ps_4_0", ShaderFlags.None, EffectFlags.None),
                ShaderBytecode.CompileFromFile("Notes.fx", "GS_Note", "gs_4_0", ShaderFlags.None, EffectFlags.None)
            );

            noteLayout = new InputLayout(device, ShaderSignature.GetInputSignature(notesShader.vertexShaderByteCode), new[] {
                new InputElement("START",0,Format.R32_Float,0,0),
                new InputElement("END",0,Format.R32_Float,4,0),
                new InputElement("COLORL",0,Format.R32G32B32A32_Float,8,0),
                new InputElement("COLORR",0,Format.R32G32B32A32_Float,24,0),
            });

            var stream = new DataStream(32 * 4, true, true);
            var note = new RenderNote()
            {
                start = -0.5f,
                end = 0.5f,
                color = new NoteCol()
                {
                    r = 0,
                    g = 1,
                    b = 1,
                    a = 1,
                    r2 = 0,
                    g2 = 1,
                    b2 = 1,
                    a2 = 1
                }
            };
            stream.Write(note);
            stream.Position = 0;
            noteBuffer = new Buffer(device, stream, new BufferDescription()
            {
                BindFlags = BindFlags.VertexBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None,
                SizeInBytes = 32 * 4,
                Usage = ResourceUsage.Default,
                StructureByteStride = 0
            });
            stream.Release();

            globalNoteConstants = new Buffer(device, new BufferDescription()
            {
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.Write,
                OptionFlags = ResourceOptionFlags.None,
                SizeInBytes = 16,
                Usage = ResourceUsage.Dynamic,
                StructureByteStride = 0
            });
        }

        void SetNoteShaderConstants(DeviceContext context, NotesGlobalConstants constants)
        {
            var data = context.MapSubresource(globalNoteConstants, 0, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None).Data;
            data.Position = 0;
            data.Write(constants);
            context.UnmapSubresource(globalNoteConstants, 0);
            context.VertexShader.SetConstantBuffer(0, globalNoteConstants);
            context.GeometryShader.SetConstantBuffer(0, globalNoteConstants);
        }

        public void Render(Device device, RenderTargetView target, DrawEventArgs args)
        {
            var context = device.ImmediateContext;
            context.InputAssembler.SetInputLayout(noteLayout);

            notesShader.SetShaders(context);
            SetNoteShaderConstants(context, new NotesGlobalConstants()
            {
                NoteBorder = 0.02f,
                NoteLeft=-0.2f,
                NoteRight=0.0f,
                ScreenAspect = (float)(args.RenderSize.Height / args.RenderSize.Width)
            });
            context.InputAssembler.SetPrimitiveTopology(PrimitiveTopology.PointList);
            context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(noteBuffer, 12, 0));

            context.ClearRenderTargetView(target, new Color4(0.6f, 0, 0, 0));
            context.Draw(1, 0);
        }

        public void Dispose()
        {
            Disposer.SafeDispose(ref noteLayout);
            Disposer.SafeDispose(ref keyLayout);
            Disposer.SafeDispose(ref notesShader);
            Disposer.SafeDispose(ref globalNoteConstants);
            Disposer.SafeDispose(ref noteBuffer);
        }
    }
}
