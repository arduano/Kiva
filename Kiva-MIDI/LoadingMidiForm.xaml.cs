using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MessageBox = KivaShared.MessageBox;

namespace Kiva_MIDI
{
    /// <summary>
    /// Interaction logic for LoadingMidiForm.xaml
    /// </summary>
    public partial class LoadingMidiForm : Window, IDisposable
    {
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private const int GWL_STYLE = -16;
        private const int WS_MAXIMIZEBOX = 0x10000;

        public MIDIFile LoadedFile { get; private set; } = null;
        public event Action ParseFinished;
        public event Action ParseCancelled;

        bool cancelling = false;
        bool loading = false;

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            var hwnd = new WindowInteropHelper((Window)sender).Handle;
            var value = GetWindowLong(hwnd, GWL_STYLE);
            SetWindowLong(hwnd, GWL_STYLE, (int)(value & ~WS_MAXIMIZEBOX));
        }

        double rotateProgress = 0;

        string filepath;
        Settings settings;
        MIDILoaderSettings loaderSettings;

        public LoadingMidiForm(string filepath, Settings settings)
        {
            InitializeComponent();
            this.filepath = filepath;
            this.settings = settings;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            cancel.Cancel();
            cancelling = true;
        }

        VelocityDrivenAnimation rotate = new VelocityDrivenAnimation();
        Storyboard rotateStoryboard = new Storyboard();

        CancellationTokenSource cancel = new CancellationTokenSource();

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Window_SourceInitialized(this, null);
            Focus();
            Topmost = true;
            Topmost = false;
            BringIntoView();
            Activate();
            Keyboard.Focus(this);

            audioThreads.Maximum = Environment.ProcessorCount;

            loaderSettings = settings.GetMIDILoaderSettings();
            var ls = loaderSettings.Clone();

            audioThresh.Value = ls.EventVelocityThreshold;
            visibleThresh.Value = ls.NoteVelocityThreshold;
            audioThreads.Value = ls.EventPlayerThreads;
            removeOverlaps.IsChecked = loaderSettings.RemoveOverlaps;
        }

        void ContinueLoading()
        {
            if (loading) return;
            loading = true;
            Keyboard.ClearFocus();
            loadSettingsScreen.Visibility = Visibility.Collapsed;
            loadProgessScreen.Visibility = Visibility.Visible;
            LoadedFile = new MIDIMemoryFile(filepath, loaderSettings, cancel.Token);
            LoadedFile.ParseFinished += () =>
            {
                Dispatcher.InvokeAsync(() =>
                {
                    ParseFinished?.Invoke();
                }).Task.GetAwaiter().GetResult();
            };
            LoadedFile.ParseCancelled += () =>
            {
                Dispose();
                Dispatcher.InvokeAsync(() =>
                {
                    ParseCancelled?.Invoke();
                }).Task.GetAwaiter().GetResult();
            };
            Task.Run(() =>
            {
                try
                {
                    LoadedFile.Parse();
                }
                catch  (Exception e)
                {
                    Dispatcher.InvokeAsync(() =>
                    {
                        MessageBox.Show(e.Message, "Couldn't load midi file", this);
                        ParseCancelled?.Invoke();
                    }).Task.GetAwaiter().GetResult();
                }
            });

            rotateStoryboard.Children.Add(rotate);
            Storyboard.SetTarget(rotate, rotateLogo);
            Storyboard.SetTargetProperty(rotate, new PropertyPath("(Image.RenderTransform).(RotateTransform.Angle)"));

            CompositionTarget.Rendering += OnRender;
        }

        void OnRender(object sender, EventArgs e)
        {
            try
            {
                if (cancelling)
                    loadingText.Text = "Cancelling...";
                else
                    loadingText.Text = LoadedFile.ParseStatusText;
                memoryText.Text = "RAM: " + (Process.GetCurrentProcess().PrivateMemorySize64 / 1000000.0).ToString("#.##") + "MB";
                if (rotateProgress != LoadedFile.ParseNumber)
                {
                    rotateProgress = LoadedFile.ParseNumber;
                    rotate.Duration = TimeSpan.FromSeconds(1);
                    rotate.From = (rotateLogo.RenderTransform as RotateTransform).Angle;
                    rotate.To = rotateProgress;
                    rotateStoryboard.Begin();
                }
            }
            catch { }
        }

        public void Dispose()
        {
            try
            {
                CompositionTarget.Rendering -= OnRender;
                this.LoadedFile = null;
            }
            catch { }
        }

        private void SaveDefaults_Click(object sender, RoutedEventArgs e)
        {
            saveDefaults.Focus();
            settings.UpdateMIDILoaderSettings(loaderSettings);
            continueButton.Focus();
            Keyboard.Focus(continueButton);
        }

        private void ContinueButton_Click(object sender, RoutedEventArgs e)
        {
            ContinueLoading();
        }

        private void VisibleThresh_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (IsInitialized)
            {
                loaderSettings.NoteVelocityThreshold = (byte)visibleThresh.Value;
                if(audioThresh.Value < visibleThresh.Value)
                {
                    audioThresh.Value = visibleThresh.Value;
                }
            }
        }

        private void AudioThresh_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (IsInitialized)
            {
                loaderSettings.EventVelocityThreshold = (byte)audioThresh.Value;
                if (audioThresh.Value < visibleThresh.Value)
                {
                    visibleThresh.Value = audioThresh.Value;
                }
            }
        }

        private void AudioThreads_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (IsInitialized)
                loaderSettings.EventPlayerThreads = (int)audioThreads.Value;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (audioThresh.TextFocused || visibleThresh.TextFocused || audioThreads.TextFocused) return;
            ContinueLoading();
        }

        private void removeOverlaps_CheckToggled(object sender, RoutedPropertyChangedEventArgs<bool> e)
        {
            if (IsInitialized)
                loaderSettings.RemoveOverlaps = removeOverlaps.IsChecked;
        }
    }

    class VelocityDrivenAnimation : DoubleAnimationBase
    {
        public double From
        {
            get { return (double)GetValue(FromProperty); }
            set { SetValue(FromProperty, value); }
        }

        public static readonly DependencyProperty FromProperty =
            DependencyProperty.Register("From", typeof(double), typeof(VelocityDrivenAnimation), new PropertyMetadata(0.0));


        public double To
        {
            get { return (double)GetValue(ToProperty); }
            set { SetValue(ToProperty, value); }
        }

        public static readonly DependencyProperty ToProperty =
            DependencyProperty.Register("To", typeof(double), typeof(VelocityDrivenAnimation), new PropertyMetadata(0.0));


        VelocityDrivenAnimation parent = null;
        double velocity = 0;

        public VelocityDrivenAnimation() { }

        public Freezable CreateInstanceCorePublic() => CreateInstanceCore();

        protected override Freezable CreateInstanceCore()
        {
            var instance = new VelocityDrivenAnimation()
            {
                parent = this,
                From = From,
                To = To,
                velocity = velocity
            };
            return instance;
        }

        double easeFunc(double x, double v) =>
            (-2 + 4 * x + v * (1 + 2 * x * (1 + x * (-5 - 2 * (x - 3) * x)))) /
            (4 + 8 * (x - 1) * x);
        double easeVelFunc(double x, double v) =>
            -((x - 1) * (2 * x + v * (x - 1) * (-1 + 4 * x * (1 + (x - 1) * x)))) /
            Math.Pow(1 + 2 * (x - 1) * x, 2);

        public double GetCurrentValueCorePublic(double defaultOriginValue, double defaultDestinationValue, AnimationClock animationClock) =>
            GetCurrentValueCore(defaultOriginValue, defaultDestinationValue, animationClock);

        protected override double GetCurrentValueCore(double defaultOriginValue, double defaultDestinationValue, AnimationClock animationClock)
        {
            double s = From;
            double f = To;
            double dist = f - s;
            if (dist == 0)
            {
                parent.velocity = 0;
                return s;
            }
            double v = velocity / dist;
            double x = (double)animationClock.CurrentProgress / 2 + 0.5;

            double ease = easeFunc(x, v) - easeFunc(0, v);
            //ease = (ease - 0.5) * 2;
            double vel = easeVelFunc(x, v);

            parent.velocity = vel * dist;
            ease = (ease - 0.5) * 2;
            return ease * dist + s;
        }
    }
}
