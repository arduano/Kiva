using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Kiva_MIDI
{
    /// <summary>
    /// A WPF control that performs all OpenGL rendering on a thread separate from the UI thread to improve performance
    /// </summary>
    public class ThreadOpenTkControl : OpenTkControlBase
    {
        public static readonly DependencyProperty ThreadNameProperty = DependencyProperty.Register(
            nameof(ThreadName), typeof(string), typeof(ThreadOpenTkControl), new PropertyMetadata("OpenTk Render Thread"));

        /// <summary>
        /// The name of the background thread that does the OpenGL rendering
        /// </summary>
        public string ThreadName
        {
            get => (string)GetValue(ThreadNameProperty);
            set => SetValue(ThreadNameProperty, value);
        }

        /// <summary>
        /// This event is set to notify the thread to wake up when the control becomes visible
        /// </summary>
        private readonly ManualResetEvent _becameVisibleEvent = new ManualResetEvent(false);

        /// <summary>
        /// The Thread object for the rendering thread
        /// </summary>
        private Thread _renderThread;

        /// <summary>
        /// The CTS used to stop the thread when this control is unloaded
        /// </summary>
        private CancellationTokenSource _endThreadCts;


        public ThreadOpenTkControl()
        {
            IsVisibleChanged += OnIsVisibleChanged;
        }

        public override Task RunOnUiThread(Action action)
        {
            return Dispatcher.InvokeAsync(action).Task;
        }

        protected override void OnLoaded(object sender, RoutedEventArgs args)
        {
            base.OnLoaded(sender, args);

            _endThreadCts = new CancellationTokenSource();
            _renderThread = new Thread(RenderThread)
            {
                IsBackground = true,
                Priority = ThreadPriority.Highest,
                Name = ThreadName
            };
            _renderThread.Start(_endThreadCts.Token);
        }

        protected override void OnUnloaded(object sender, RoutedEventArgs args)
        {
            base.OnUnloaded(sender, args);

            _endThreadCts.Cancel();
            _renderThread.Join();
        }

        /// <summary>
        /// Wakes up the thread when the control becomes visible
        /// </summary>
        /// <param name="sender">The object that sent the event</param>
        /// <param name="args">The event arguments about this event</param>
        private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            bool visible = (bool)args.NewValue;

            if (visible)
                _becameVisibleEvent.Set();
        }

        /// <summary>
        /// The function that the thread runs to render the control
        /// </summary>
        /// <param name="boxedToken"></param>
        private void RenderThread(object boxedToken)
        {
            CancellationToken token = (CancellationToken)boxedToken;

            InitOpenGl();

            WaitHandle[] notContinousHandles = { token.WaitHandle, ManualRepaintEvent };
            WaitHandle[] notVisibleHandles = { token.WaitHandle, _becameVisibleEvent };
            while (!token.IsCancellationRequested)
            {
                if (!_continuous)
                {
                    WaitHandle.WaitAny(notContinousHandles);
                }
                else if (!IsVisible)
                {
                    WaitHandle.WaitAny(notVisibleHandles);
                    _becameVisibleEvent.Reset();

                    if (!_continuous)
                        continue;
                }

                if (token.IsCancellationRequested)
                    break;

                ManualRepaintEvent.Reset();

                TimeSpan sleepTime = Render();
                if (sleepTime.CompareTo(TimeSpan.Zero) > 0)
                    Thread.Sleep(sleepTime);
            }

            DeInitOpenGl();
        }
    }
}
