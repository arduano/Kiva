using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Kiva_MIDI
{
    /// <summary>
    /// A WPF control that performs OpenGL rendering on the UI thread
    /// </summary>
    public class UiOpenTkControl : OpenTkControlBase
    {
        private DateTime _nextRenderTime = DateTime.MinValue;

        /// <summary>
        /// Creates a UiOpenTkControl
        /// </summary>
        public UiOpenTkControl()
        {
            IsVisibleChanged += OnIsVisibleChanged;
        }

        public override Task RunOnUiThread(Action action)
        {
            action();
            return null;
        }

        protected override void OnLoaded(object sender, RoutedEventArgs args)
        {
            base.OnLoaded(sender, args);

            InitOpenGl();
        }

        protected override void OnUnloaded(object sender, RoutedEventArgs routedEventArgs)
        {
            DeInitOpenGl();

            base.OnUnloaded(sender, routedEventArgs);
        }

        /// <summary>
        /// Performs the OpenGl rendering when this control is visible
        /// </summary>
        /// <param name="sender">The object that sent the event</param>
        /// <param name="args">The event arguments about this event</param>
        private void CompositionTargetOnRendering(object sender, EventArgs args)
        {
            DateTime now = DateTime.Now;
            if ((_continuous && now > _nextRenderTime) || ManualRepaintEvent.WaitOne(0))
            {
                ManualRepaintEvent.Reset();
                _nextRenderTime = now + Render();
            }
        }

        /// <summary>
        /// Handles subscribing and unsubcribing <see cref="CompositionTargetOnRendering"/> when this component's visibility has changed
        /// </summary>
        /// <param name="sender">The object that sent the event</param>
        /// <param name="args">The event arguments about this event</param>
        private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            bool visible = (bool)args.NewValue;

            if (visible)
                CompositionTarget.Rendering += CompositionTargetOnRendering;
            else
                CompositionTarget.Rendering -= CompositionTargetOnRendering;

        }
    }
}
