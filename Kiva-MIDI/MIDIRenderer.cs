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
using System.Collections.Concurrent;
using IO = System.IO;
using System.Reflection;

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
            public float KeyboardHeight;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 16)]
        struct KeyboardGlobalConstants
        {
            public float Height;
            public float Left;
            public float Right;
            public float Aspect;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct RenderNote
        {
            public float start;
            public float end;
            public NoteCol color;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct RenderKey
        {
            public uint colorl;
            public uint colorr;
            public float left;
            public float right;
            public float distance;
            public uint meta;

            public void MarkPressed(bool ispressed)
            {
                meta = (uint)(meta & 0b10111111);
                if (ispressed)
                    meta = (uint)(meta | 0b01);
            }

            public void MarkBlack(bool black)
            {
                meta = (uint)(meta & 0b01111111);
                if (black)
                    meta = (uint)(meta | 0b1);
            }
        }

        public MIDIFile File { get; set; }
        public PlayingState Time { get; set; } = new PlayingState();

        public long LastRenderedNoteCount { get; private set; } = 0;

        ShaderManager notesShader;
        ShaderManager SmallWhiteKeyShader;
        ShaderManager SmallBlackKeyShader;
        ShaderManager BigWhiteKeyShader;
        ShaderManager BigBlackKeyShader;
        InputLayout noteLayout;
        InputLayout keyLayout;
        Buffer globalNoteConstants;
        Buffer keyBuffer;

        NotesGlobalConstants noteConstants;

        int noteBufferLength = 1 << 10;
        Buffer noteBuffer;

        VelocityEase dynamicState = new VelocityEase(0) { Duration = 0.7, Slope = 3, Supress = 2 };
        bool dynamicState88 = false;

        bool[] blackKeys = new bool[257];
        int[] keynum = new int[257];

        RenderKey[] renderKeys = new RenderKey[257];
        VelocityEase[] keyEases = new VelocityEase[256];

        Settings settings;

        public MIDIRenderer(Device device, Settings settings)
        {
            this.settings = settings;
            string noteShaderData;
            if (IO.File.Exists("Notes.fx"))
            {
                noteShaderData = IO.File.ReadAllText("Notes.fx");
            }
            else
            {
                var assembly = Assembly.GetExecutingAssembly();
                var names = assembly.GetManifestResourceNames();
                using (var stream = assembly.GetManifestResourceStream("Kiva_MIDI.Notes.fx"))
                using (var reader = new IO.StreamReader(stream))
                    noteShaderData = reader.ReadToEnd();
            }
            notesShader = new ShaderManager(
                device,
                ShaderBytecode.Compile(noteShaderData, "VS_Note", "vs_4_0", ShaderFlags.None, EffectFlags.None),
                ShaderBytecode.Compile(noteShaderData, "PS", "ps_4_0", ShaderFlags.None, EffectFlags.None),
                ShaderBytecode.Compile(noteShaderData, "GS_Note", "gs_4_0", ShaderFlags.None, EffectFlags.None)
            );

            if (IO.File.Exists("KeyboardSmall.fx"))
            {
                noteShaderData = IO.File.ReadAllText("KeyboardSmall.fx");
            }
            else
            {
                var assembly = Assembly.GetExecutingAssembly();
                var names = assembly.GetManifestResourceNames();
                using (var stream = assembly.GetManifestResourceStream("Kiva_MIDI.KeyboardSmall.fx"))
                using (var reader = new IO.StreamReader(stream))
                    noteShaderData = reader.ReadToEnd();
            }
            SmallWhiteKeyShader = new ShaderManager(
                device,
                ShaderBytecode.Compile(noteShaderData, "VS", "vs_4_0", ShaderFlags.None, EffectFlags.None),
                ShaderBytecode.Compile(noteShaderData, "PS", "ps_4_0", ShaderFlags.None, EffectFlags.None),
                ShaderBytecode.Compile(noteShaderData, "GS_White", "gs_4_0", ShaderFlags.None, EffectFlags.None)
            );
            SmallBlackKeyShader = new ShaderManager(
                device,
                ShaderBytecode.Compile(noteShaderData, "VS", "vs_4_0", ShaderFlags.None, EffectFlags.None),
                ShaderBytecode.Compile(noteShaderData, "PS", "ps_4_0", ShaderFlags.None, EffectFlags.None),
                ShaderBytecode.Compile(noteShaderData, "GS_Black", "gs_4_0", ShaderFlags.None, EffectFlags.None)
            );

            noteLayout = new InputLayout(device, ShaderSignature.GetInputSignature(notesShader.vertexShaderByteCode), new[] {
                new InputElement("START",0,Format.R32_Float,0,0),
                new InputElement("END",0,Format.R32_Float,4,0),
                new InputElement("COLORL",0,Format.R32_UInt,8,0),
                new InputElement("COLORR",0,Format.R32_UInt,12,0),
            });

            keyLayout = new InputLayout(device, ShaderSignature.GetInputSignature(SmallWhiteKeyShader.vertexShaderByteCode), new[] {
                new InputElement("COLORL",0,Format.R32_UInt,0,0),
                new InputElement("COLORR",0,Format.R32_UInt,4,0),
                new InputElement("LEFT",0,Format.R32_Float,8,0),
                new InputElement("RIGHT",0,Format.R32_Float,12,0),
                new InputElement("DISTANCE",0,Format.R32_Float,16,0),
                new InputElement("META",0,Format.R32_UInt,20,0),
            });

            noteConstants = new NotesGlobalConstants()
            {
                NoteBorder = 0.002f,
                NoteLeft = -0.2f,
                NoteRight = 0.0f,
                ScreenAspect = 1f
            };

            noteBuffer = new Buffer(device, new BufferDescription()
            {
                BindFlags = BindFlags.VertexBuffer,
                CpuAccessFlags = CpuAccessFlags.Write,
                OptionFlags = ResourceOptionFlags.None,
                SizeInBytes = 40 * noteBufferLength,
                Usage = ResourceUsage.Dynamic,
                StructureByteStride = 0
            });

            keyBuffer = new Buffer(device, new BufferDescription()
            {
                BindFlags = BindFlags.VertexBuffer,
                CpuAccessFlags = CpuAccessFlags.Write,
                OptionFlags = ResourceOptionFlags.None,
                SizeInBytes = 24 * renderKeys.Length,
                Usage = ResourceUsage.Dynamic,
                StructureByteStride = 0
            });

            globalNoteConstants = new Buffer(device, new BufferDescription()
            {
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.Write,
                OptionFlags = ResourceOptionFlags.None,
                SizeInBytes = 32,
                Usage = ResourceUsage.Dynamic,
                StructureByteStride = 0
            });

            for (int i = 0; i < blackKeys.Length; i++) blackKeys[i] = isBlackNote(i);
            int b = 0;
            int w = 0;
            for (int i = 0; i < keynum.Length; i++)
            {
                if (blackKeys[i]) keynum[i] = b++;
                else keynum[i] = w++;
            }

            int firstNote = 0;
            int lastNote = 256;

            double wdth;

            double knmfn = keynum[firstNote];
            double knmln = keynum[lastNote - 1];
            if (blackKeys[firstNote]) knmfn = keynum[firstNote - 1] + 0.5;
            if (blackKeys[lastNote - 1]) knmln = keynum[lastNote] - 0.5;
            for (int i = 0; i < 257; i++)
            {
                if (!blackKeys[i])
                {
                    x1array[i] = (float)(keynum[i] - knmfn) / (knmln - knmfn + 1);
                    wdtharray[i] = 1.0f / (knmln - knmfn + 1);
                    renderKeys[i].MarkBlack(false);
                }
                else
                {
                    int _i = i + 1;
                    wdth = 0.6f / (knmln - knmfn + 1);
                    int bknum = keynum[i] % 5;
                    double offset = wdth / 2;
                    if (bknum == 0 || bknum == 2)
                    {
                        offset *= 1.3;
                    }
                    else if (bknum == 1 || bknum == 4)
                    {
                        offset *= 0.7;
                    }
                    x1array[i] = (float)(keynum[_i] - knmfn) / (knmln - knmfn + 1) - offset;
                    wdtharray[i] = wdth;
                    renderKeys[i].MarkBlack(true);
                }
                renderKeys[i].left = (float)x1array[i];
                renderKeys[i].right = (float)(x1array[i] + wdtharray[i]);
            }

            for (int i = 0; i < keyEases.Length; i++)
            {
                keyEases[i] = new VelocityEase(0) { Duration = 0.05, Slope = 2, Supress = 3 };
            }

            var renderTargetDesc = new RenderTargetBlendDescription();
            renderTargetDesc.IsBlendEnabled = true;
            renderTargetDesc.SourceBlend = BlendOption.SourceAlpha;
            renderTargetDesc.DestinationBlend = BlendOption.InverseSourceAlpha;
            renderTargetDesc.BlendOperation = BlendOperation.Add;
            renderTargetDesc.SourceAlphaBlend = BlendOption.One;
            renderTargetDesc.DestinationAlphaBlend = BlendOption.One;
            renderTargetDesc.AlphaBlendOperation = BlendOperation.Add;
            renderTargetDesc.RenderTargetWriteMask = ColorWriteMaskFlags.All;

            BlendStateDescription desc = new BlendStateDescription();
            desc.AlphaToCoverageEnable = false;
            desc.IndependentBlendEnable = false;
            desc.RenderTarget[0] = renderTargetDesc;

            var blendStateEnabled = new BlendState(device, desc);

            device.ImmediateContext.OutputMerger.SetBlendState(blendStateEnabled);
        }

        void SetNoteShaderConstants(DeviceContext context, NotesGlobalConstants constants)
        {
            DataStream data;
            context.MapSubresource(globalNoteConstants, 0, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out data);
            data.Write(constants);
            context.UnmapSubresource(globalNoteConstants, 0);
            context.VertexShader.SetConstantBuffer(0, globalNoteConstants);
            context.GeometryShader.SetConstantBuffer(0, globalNoteConstants);
        }

        void SetKeyboardShaderConstants(DeviceContext context, KeyboardGlobalConstants constants)
        {
            DataStream data;
            context.MapSubresource(globalNoteConstants, 0, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out data);
            data.Write(constants);
            context.UnmapSubresource(globalNoteConstants, 0);
            context.VertexShader.SetConstantBuffer(0, globalNoteConstants);
            context.GeometryShader.SetConstantBuffer(0, globalNoteConstants);
        }

        double[] x1array = new double[257];
        double[] wdtharray = new double[257];
        bool[] pressedKeys = new bool[256];

        public void Render(Device device, RenderTargetView target, DrawEventArgs args)
        {
            var context = device.ImmediateContext;
            context.InputAssembler.InputLayout = noteLayout;

            double time = Time.GetTime();
            double timeScale = settings.Volatile.Size;
            double renderCutoff = time + timeScale;
            int firstNote = 0;
            int lastNote = 128;
            if (settings.General.KeyRange == KeyRangeTypes.KeyDynamic ||
                settings.General.KeyRange == KeyRangeTypes.Key128)
            {
                firstNote = 0;
                lastNote = 128;
            }
            else if (settings.General.KeyRange == KeyRangeTypes.Key256)
            {
                firstNote = 0;
                lastNote = 256;
            }
            else if (settings.General.KeyRange == KeyRangeTypes.Key88)
            {
                firstNote = 21;
                lastNote = 108;
            }
            else if (settings.General.KeyRange == KeyRangeTypes.Custom)
            {
                firstNote = settings.General.CustomFirstKey;
                lastNote = settings.General.CustomLastKey + 1;
            }
            else if (settings.General.KeyRange == KeyRangeTypes.KeyMIDI)
            {
                if (File != null)
                {
                    firstNote = File.FirstKey;
                    lastNote = File.LastKey + 1;
                }
            }
            int kbfirstNote = firstNote;
            int kblastNote = lastNote;
            if (blackKeys[firstNote]) kbfirstNote--;
            if (blackKeys[lastNote - 1]) kblastNote++;


            notesShader.SetShaders(context);
            noteConstants.ScreenAspect = (float)(args.RenderSize.Height / args.RenderSize.Width);
            noteConstants.NoteBorder = 0.0015f;
            SetNoteShaderConstants(context, noteConstants);

            context.ClearRenderTargetView(target, new Color4(0.4f, 0.4f, 0.4f, 1f));

            double ds = dynamicState.GetValue(0, 1);

            double fullLeft = x1array[firstNote];
            double fullRight = x1array[lastNote - 1] + wdtharray[lastNote - 1];
            if (settings.General.KeyRange == KeyRangeTypes.KeyDynamic)
            {
                double kleft = x1array[21];
                double kright = x1array[108] + wdtharray[108];
                fullLeft = fullLeft * (1 - ds) + kleft * ds;
                fullRight = fullRight * (1 - ds) + kright * ds;
            }
            double fullWidth = fullRight - fullLeft;

            float kbHeight = (float)(args.RenderSize.Width / args.RenderSize.Height / fullWidth);
            if (settings.General.KeyboardStyle == KeyboardStyle.Small) kbHeight *= 0.017f;
            noteConstants.KeyboardHeight = kbHeight;

            if (File != null)
            {
                File.SetColorEvents(time);

                var colors = File.MidiNoteColors;
                var lastTime = File.lastRenderTime;

                long notesRendered = 0;
                object addLock = new object();

                int firstRenderKey = 256;
                int lastRenderKey = -1;

                for (int black = 0; black < 2; black++)
                {
                    Parallel.For(firstNote, lastNote, new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount }, k =>
                    {
                        if ((blackKeys[k] && black == 0) || (!blackKeys[k] && black == 1)) return;
                        long _notesRendered = 0;
                        float left = (float)((x1array[k] - fullLeft) / fullWidth);
                        float right = (float)((x1array[k] + wdtharray[k] - fullLeft) / fullWidth);
                        bool pressed = false;
                        NoteCol col = new NoteCol();
                        unsafe
                        {
                            RenderNote* rn = stackalloc RenderNote[noteBufferLength];
                            int nid = 0;
                            int noff = File.FirstRenderNote[k];
                            Note[] notes = File.Notes[k];
                            if (lastTime > time)
                            {
                                for (noff = 0; noff < notes.Length; noff++)
                                {
                                    if (notes[noff].end > time)
                                    {
                                        File.FirstRenderNote[k] = noff;
                                        break;
                                    }
                                }
                            }
                            else if (lastTime < time)
                            {
                                for (; noff < notes.Length; noff++)
                                {
                                    if (notes[noff].end > time)
                                    {
                                        File.FirstRenderNote[k] = noff;
                                        break;
                                    }
                                }
                            }
                            while (noff != notes.Length && notes[noff].start < renderCutoff)
                            {
                                var n = notes[noff++];
                                if (n.end < time) continue;
                                if (n.start < time)
                                {
                                    pressed = true;
                                    NoteCol kcol = File.MidiNoteColors[n.colorPointer];
                                    col.rgba = NoteCol.Blend(col.rgba, kcol.rgba);
                                    col.rgba2 = NoteCol.Blend(col.rgba2, kcol.rgba2);
                                }
                                _notesRendered++;
                                rn[nid++] = new RenderNote()
                                {
                                    start = (float)((n.start - time) / timeScale),
                                    end = (float)((n.end - time) / timeScale),
                                    color = colors[n.colorPointer]
                                };
                                if (nid == noteBufferLength)
                                {
                                    FlushNoteBuffer(context, left, right, (IntPtr)rn, nid);
                                    nid = 0;
                                }
                            }
                            FlushNoteBuffer(context, left, right, (IntPtr)rn, nid);
                            renderKeys[k].colorl = col.rgba;
                            renderKeys[k].colorr = col.rgba2;
                            if (pressed && keyEases[k].End == 0) keyEases[k].SetEnd(1);
                            else if (!pressed && keyEases[k].End == 1) keyEases[k].SetEnd(0);
                            renderKeys[k].distance = (float)keyEases[k].GetValue(0, 1);
                            lock (addLock)
                            {
                                notesRendered += _notesRendered;
                                if (_notesRendered > 0)
                                {
                                    if (firstRenderKey > k) firstRenderKey = k;
                                    if (lastRenderKey < k) lastRenderKey = k;
                                }
                            }
                        }
                    });
                }
                if (firstRenderKey <= 19 || lastRenderKey >= 110)
                {
                    if (dynamicState88)
                    {
                        dynamicState.SetEnd(0);
                        dynamicState88 = false;
                    }
                }
                else
                {
                    if (!dynamicState88)
                    {
                        dynamicState.SetEnd(1);
                        dynamicState88 = true;
                    }
                }

                LastRenderedNoteCount = notesRendered;
                File.lastRenderTime = time;
            }
            else
            {
                LastRenderedNoteCount = 0;
            }


            SetKeyboardShaderConstants(context, new KeyboardGlobalConstants() {
                Height = kbHeight,
                Left = (float)fullLeft,
                Right = (float)fullRight,
                Aspect = noteConstants.ScreenAspect
            });
            DataStream data;
            context.MapSubresource(keyBuffer, 0, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out data);
            data.Position = 0;
            data.WriteRange(renderKeys, 0, 257);
            context.UnmapSubresource(keyBuffer, 0);
            context.InputAssembler.InputLayout = keyLayout;
            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.PointList;
            context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(keyBuffer, 24, 0));
            SmallWhiteKeyShader.SetShaders(context);
            context.Draw(257, 0);
            SmallBlackKeyShader.SetShaders(context);
            context.Draw(257, 0);
        }

        unsafe void FlushNoteBuffer(DeviceContext context, float left, float right, IntPtr notes, int count)
        {
            if (count == 0) return;
            lock (context)
            {
                DataStream data;
                context.MapSubresource(noteBuffer, 0, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out data);
                data.Position = 0;
                data.WriteRange(notes, count * sizeof(RenderNote));
                context.UnmapSubresource(noteBuffer, 0);
                context.InputAssembler.PrimitiveTopology = PrimitiveTopology.PointList;
                context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(noteBuffer, 16, 0));
                noteConstants.NoteLeft = left;
                noteConstants.NoteRight = right;
                SetNoteShaderConstants(context, noteConstants);
                context.Draw(count, 0);
            }
        }

        private DeviceContext GetInternalContext(Device device)
        {
            var props = device.GetType().GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var prop = device.GetType().GetField("Context", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            DeviceContext context = prop.GetValue(device) as DeviceContext;
            return context;
        }

        public void Dispose()
        {
            Disposer.SafeDispose(ref noteLayout);
            Disposer.SafeDispose(ref keyLayout);
            Disposer.SafeDispose(ref notesShader);
            Disposer.SafeDispose(ref SmallWhiteKeyShader);
            Disposer.SafeDispose(ref SmallBlackKeyShader);
            Disposer.SafeDispose(ref BigWhiteKeyShader);
            Disposer.SafeDispose(ref BigBlackKeyShader);
            Disposer.SafeDispose(ref globalNoteConstants);
            Disposer.SafeDispose(ref noteBuffer);
            Disposer.SafeDispose(ref keyBuffer);
        }

        bool isBlackNote(int n)
        {
            n = n % 12;
            return n == 1 || n == 3 || n == 6 || n == 8 || n == 10;
        }
    }
}
