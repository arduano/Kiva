using SharpDX;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
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
        public bool SyncRender { get; set; } = false;
        Stopwatch frameTimer = new Stopwatch();
        double delayExtraDelay = 0;

        SemaphoreSlim semaphore = new SemaphoreSlim(0, 5);

        Stopwatch renderTimer = new Stopwatch();

        CancellationTokenSource disposeCancel = new CancellationTokenSource();

        object fpslock = new object();

        public double FPS
        {
            get
            {
                lock (fpslock) return (10000000 / ((double)RealFrameTimes.Sum() / FrameTimes.Count));
            }
        }
        public double FakeFPS
        {
            get
            {
                lock (fpslock) return (10000000 / ((double)FrameTimes.Sum() / FrameTimes.Count));
            }
        }


        List<long> FrameTimes = new List<long>();
        List<long> RealFrameTimes = new List<long>();

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

        bool disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            disposed = true;
            disposeCancel.Cancel();
            renderThread.GetAwaiter().GetResult();
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
            var old = field;
            field = newValue;
            if (old != null)
                old.Dispose();
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
            if (semaphore.CurrentCount < 5)
                semaphore.Release();
            lock (argsPointer)
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
                while (!disposed)
                {
                    while (SingleThreadedRender)
                    {
                        Thread.Sleep(100);
                        if (disposed) return;
                    }
                    frameTimer.Start();
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
                    lock (fpslock) FrameTimes.Add(frameTimer.ElapsedTicks);
                    if (SyncRender)
                    {
                        try
                        {
                            semaphore.Wait(disposeCancel.Token);
                        }
                        catch (OperationCanceledException) { }
                    }
                    else if (FPSLock != 0)
                    {
                        var desired = 10000000 / FPSLock;
                        var elapsed = frameTimer.ElapsedTicks;
                        long remaining = -(desired + (long)delayExtraDelay - elapsed);
                        Stopwatch s = new Stopwatch();
                        s.Start();
                        if (remaining < 0)
                        {
                            //NtDelayExecution(false, ref remaining);
                            Thread.Sleep(-(int)remaining / 10000);
                        }
                        var excess = desired - frameTimer.ElapsedTicks;
                        delayExtraDelay = (delayExtraDelay * 60 + excess) / 61;
                    }
                    lock (fpslock) RealFrameTimes.Add(frameTimer.ElapsedTicks);
                    frameTimer.Reset();

                    lock (fpslock)
                    {
                        while (RealFrameTimes.Sum() - RealFrameTimes[0] > 10000000)
                        {
                            RealFrameTimes.RemoveAt(0);
                            FrameTimes.RemoveAt(0);
                        }
                    }
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
