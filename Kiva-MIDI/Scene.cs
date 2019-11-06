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
using System.Threading;

namespace Kiva_MIDI
{

    class Scene : IDirect3D
    {
        public FPS FPS { get; set; }

        MIDIRenderer render;
        public Settings Settings { get; set; }

        public MIDIFile File
        {
            get => render.File;
            set => render.File = value;
        }

        public PlayingState Time
        {
            get => render.Time;
            set => render.Time = value;
        }

        public long LastRenderedNoteCount
        {
            get => render.LastRenderedNoteCount;
        }

        public long LastPolyphony
        {
            get => render.LastPolyphony;
        }

        public long LastNPS
        {
            get => render.LastNPS;
        }

        public long NotesPassedSum
        {
            get => render.NotesPassedSum;
        }

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

        public Scene(Settings settings)
        {
            Settings = settings;
        }

        void ContextRendering(object aCtx, DrawEventArgs args) { RenderScene(args); }

        protected void Attach()
        {
            if (Renderer == null)
                return;

            render = new MIDIRenderer(Renderer.Device, Settings);
        }

        protected void Detach()
        {
            render.Dispose();
        }

        public void RenderScene(DrawEventArgs args)
        {
            render.Render(Renderer.Device, Renderer.RenderTargetView, args);
            FPS.AddFrame(args.TotalTime);
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
