using SharpDX;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Kiva_MIDI
{
    /// <summary>
    /// A vanilla implementation of <see cref="IDirect3D"/> with some common wiring already done.
    /// </summary>
    public abstract partial class D3D : IDirect3D, IDisposable
    {
        [DllImport("ntdll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int NtDelayExecution([MarshalAs(UnmanagedType.I1)] bool alertable, ref Int64 DelayInterval);

        class ArgPointer { public DrawEventArgs args = null; }

        ArgPointer argsPointer = new ArgPointer();

        Task renderThread = null;

        List<DateTime> frameTimes = new List<DateTime>();

        public int FPSLock { get; set; } = 60;
        public bool SingleThreadedRender { get; set; } = false;
        Stopwatch frameTimer = new Stopwatch();
        double delayExtraDelay = 0;

        Stopwatch renderTimer = new Stopwatch();

        public D3D()
        {
            OnInteractiveInit();
        }

        partial void OnInteractiveInit();

        ~D3D() { Dispose(false); }
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        /// <summary>
        /// Size set with call to <see cref="Reset(DrawEventArgs)"/>
        /// </summary>
        public Vector2 RenderSize { get; protected set; }

        public virtual void Reset(DrawEventArgs args)
        {
            lock (argsPointer)
            {
                int w = (int)Math.Ceiling(args.RenderSize.Width);
                int h = (int)Math.Ceiling(args.RenderSize.Height);
                if (w < 1 || h < 1)
                    return;

                RenderSize = new Vector2(w, h);

                Reset(w, h);
                if (Resetted != null)
                    Resetted(this, args);

                argsPointer.args = args;
                Render(args);

                if (args.Target != null)
                    SetBackBuffer(args.Target);
            }
        }

        public virtual void Reset(int w, int h)
        {
        }

        public event EventHandler<DrawEventArgs> Resetted;

        /// <summary>
        /// SharpDX 1.3 requires explicit dispose of all its ComObject.
        /// This method makes it easy.
        /// (Remark: I attempted to hack a correct Dispose implementation but it crashed the app on first GC!)
        /// </summary>
        public static void Set<T>(ref T field, T newValue)
            where T : IDisposable
        {
            if (field != null)
                field.Dispose();
            field = newValue;
        }

        public abstract System.Windows.Media.Imaging.WriteableBitmap ToImage();

        public abstract void SetBackBuffer(DXImageSource dximage);

        /// <summary>
        /// Time in the last <see cref="DrawEventArgs"/> passed to <see cref="Render(DrawEventArgs)"/>
        /// </summary>
        public TimeSpan RenderTime { get; protected set; }

        public void Render(DrawEventArgs args)
        {
            RenderTime = args.TotalTime;

            if (renderThread == null)
            {
                renderThread = StartRenderThread();
            }
            if (SingleThreadedRender)
            {
                lock (argsPointer)
                {
                    TrueRender();
                }
            }
            argsPointer.args = args;
        }

        Task StartRenderThread()
        {
            return
            Task.Run(() =>
            {
                renderTimer.Start();
                TimeSpan last = renderTimer.Elapsed;
                frameTimes.Add(DateTime.UtcNow);
                while (true)
                {
                    frameTimer.Start();
                    while (SingleThreadedRender) Thread.Sleep(100);
                    lock (argsPointer)
                    {
                        if (argsPointer.args == null || SingleThreadedRender) Thread.Sleep(100);
                        else
                        {
                            argsPointer.args.TotalTime = renderTimer.Elapsed;
                            argsPointer.args.DeltaTime = renderTimer.Elapsed - last;
                            last = renderTimer.Elapsed;
                            TrueRender();
                        }
                    }
                    if (FPSLock != 0)
                    {
                        var desired = 10000000 / FPSLock;
                        var elapsed = frameTimer.ElapsedTicks;
                        long remaining = -(desired + (long)delayExtraDelay - elapsed);
                        Stopwatch s = new Stopwatch();
                        s.Start();
                        if (remaining < 0)
                        {
                            NtDelayExecution(false, ref remaining);
                        }
                        var excess = desired - frameTimer.ElapsedTicks;
                        delayExtraDelay = (delayExtraDelay * 60 + excess) / 61;
                    }
                    frameTimer.Reset();
                }
            });
        }

        void TrueRender()
        {
            BeginRender(argsPointer.args);
            RenderScene(argsPointer.args);
            EndRender(argsPointer.args);
        }

        public virtual void BeginRender(DrawEventArgs args) { }
        public virtual void RenderScene(DrawEventArgs args)
        {
            Rendering?.Invoke(this, args);
        }
        public virtual void EndRender(DrawEventArgs args) { }

        public event EventHandler<DrawEventArgs> Rendering;
    }
}
