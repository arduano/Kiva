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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SharpDX.Direct3D;
using SharpDX.Direct3D9;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.WPF;

namespace Kiva_MIDI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {

        #region Chrome Window scary code
        private static IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case 0x0024:
                    WmGetMinMaxInfo(hwnd, lParam);
                    handled = true;
                    break;
            }
            return (IntPtr)0;
        }

        private static void WmGetMinMaxInfo(IntPtr hwnd, IntPtr lParam)
        {
            MINMAXINFO mmi = (MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(MINMAXINFO));
            int MONITOR_DEFAULTTONEAREST = 0x00000002;
            IntPtr monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
            if (monitor != IntPtr.Zero)
            {
                MONITORINFO monitorInfo = new MONITORINFO();
                GetMonitorInfo(monitor, monitorInfo);
                RECT rcWorkArea = monitorInfo.rcWork;
                RECT rcMonitorArea = monitorInfo.rcMonitor;
                if (_fullscreen)
                    rcWorkArea = rcMonitorArea;
                mmi.ptMaxPosition.x = Math.Abs(rcWorkArea.left - rcMonitorArea.left);
                mmi.ptMaxPosition.y = Math.Abs(rcWorkArea.top - rcMonitorArea.top);
                mmi.ptMaxSize.x = Math.Abs(rcWorkArea.right - rcWorkArea.left);
                mmi.ptMaxSize.y = Math.Abs(rcWorkArea.bottom - rcWorkArea.top);
            }
            Marshal.StructureToPtr(mmi, lParam, true);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            /// <summary>x coordinate of point.</summary>
            public int x;
            /// <summary>y coordinate of point.</summary>
            public int y;
            /// <summary>Construct a point of coordinates (x,y).</summary>
            public POINT(int x, int y)
            {
                this.x = x;
                this.y = y;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        };

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class MONITORINFO
        {
            public int cbSize = Marshal.SizeOf(typeof(MONITORINFO));
            public RECT rcMonitor = new RECT();
            public RECT rcWork = new RECT();
            public int dwFlags = 0;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 0)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
            public static readonly RECT Empty = new RECT();
            public int Width { get { return Math.Abs(right - left); } }
            public int Height { get { return bottom - top; } }
            public RECT(int left, int top, int right, int bottom)
            {
                this.left = left;
                this.top = top;
                this.right = right;
                this.bottom = bottom;
            }
            public RECT(RECT rcSrc)
            {
                left = rcSrc.left;
                top = rcSrc.top;
                right = rcSrc.right;
                bottom = rcSrc.bottom;
            }
            public bool IsEmpty { get { return left >= right || top >= bottom; } }
            public override string ToString()
            {
                if (this == Empty) { return "RECT {Empty}"; }
                return "RECT { left : " + left + " / top : " + top + " / right : " + right + " / bottom : " + bottom + " }";
            }
            public override bool Equals(object obj)
            {
                if (!(obj is Rect)) { return false; }
                return (this == (RECT)obj);
            }
            /// <summary>Return the HashCode for this struct (not garanteed to be unique)</summary>
            public override int GetHashCode() => left.GetHashCode() + top.GetHashCode() + right.GetHashCode() + bottom.GetHashCode();
            /// <summary> Determine if 2 RECT are equal (deep compare)</summary>
            public static bool operator ==(RECT rect1, RECT rect2) { return (rect1.left == rect2.left && rect1.top == rect2.top && rect1.right == rect2.right && rect1.bottom == rect2.bottom); }
            /// <summary> Determine if 2 RECT are different(deep compare)</summary>
            public static bool operator !=(RECT rect1, RECT rect2) { return !(rect1 == rect2); }
        }

        [DllImport("user32")]
        internal static extern bool GetMonitorInfo(IntPtr hMonitor, MONITORINFO lpmi);

        [DllImport("User32")]
        internal static extern IntPtr MonitorFromWindow(IntPtr handle, int flags);
        #endregion

        #region Fullscreen        
        static System.Windows.WindowState _cacheWindowState;
        static bool _fullscreen = false;
        static bool Fullscreen
        {
            get => _fullscreen;
        }
        void SetFullscreen(bool value)
        {
            _fullscreen = value;
            if (value)
            {
                _cacheWindowState = WindowState;
                WindowState = System.Windows.WindowState.Normal;
                WindowState = System.Windows.WindowState.Maximized;
                ChromeVisibility = Visibility.Collapsed;
            }
            else
            {
                WindowState = System.Windows.WindowState.Normal;
                WindowState = _cacheWindowState;
                ChromeVisibility = Visibility.Visible;
            }
        }
        #endregion


        public Visibility ChromeVisibility
        {
            get { return (Visibility)GetValue(ChromeVisibilityProperty); }
            set { SetValue(ChromeVisibilityProperty, value); }
        }

        public static readonly DependencyProperty ChromeVisibilityProperty =
            DependencyProperty.Register("ChromeVisibility", typeof(Visibility), typeof(MainWindow), new PropertyMetadata(Visibility.Visible));

        Scene scene;
        KDMAPIPlayer player;

        public MainWindow()
        {
            InitializeComponent();
            AllowsTransparency = false;
            SourceInitialized += (s, e) =>
            {
                IntPtr handle = (new WindowInteropHelper(this)).Handle;
                HwndSource.FromHwnd(handle).AddHook(new HwndSourceHook(WindowProc));
            };

            FPS = new FPS();
            Time = new PlayingState();
            Time.PauseChanged += PauseChanged;
            PauseChanged();
            scene = new Scene() { Renderer = new D3D11(), FPS = FPS };
            dx11img.Renderer = scene;
            dx11img.MouseDown += (s, e) => Focus();
            scene.Time = Time;

            player = new KDMAPIPlayer();
            player.RunPlayer();
            player.Time = Time;

            CompositionTarget.Rendering += (s, e) =>
            {
                fpsLabel.Content = "FPS: " + FPS.Value.ToString("#,##0.0");
                renderNcLabel.Content = "Rendered Notes: " + scene.LastRenderedNoteCount.ToString("#,##0");
                timeSlider.Value = Time.GetTime();
            };
        }
        public FPS FPS { get; set; }
        public PlayingState Time { get; set; }

        void LoadMidi(string filename)
        {
            Time.Reset();
            player.File = null;
            scene.File = null;
            GC.Collect(2, GCCollectionMode.Forced);


            var form = new LoadingMidiForm(filename);
            form.ParseFinished += () =>
            {
                var file = form.LoadedFile;
                Dispatcher.Invoke(() =>
                {
                    player.File = file;
                    scene.File = file;
                    timeSlider.Maximum = file.MidiLength;
                    form.Close();
                    form.Dispose();
                    GC.Collect(2, GCCollectionMode.Forced);
                    Time.Play();
                });
            };
            form.ParseCancelled += () =>
            {
                Dispatcher.Invoke(() =>
                {
                    form.Close();
                    form.Dispose();
                    GC.Collect(2, GCCollectionMode.Forced);
                });
            };
            form.ShowDialog();
            //var file = new MIDIFile("E:\\Midi\\tau2.5.9.mid");
            //var file = new MIDIFile("E:\\Midi\\9KX2 18 Million Notes.mid");
            //var file = new MIDIFile("E:\\Midi\\[Black MIDI]scarlet_zone-& The Young Descendant of Tepes V.2.mid");
            //var file = new MIDIFile("E:\\Midi\\Septette For The Dead Princess 442 MILLION.mid");
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MinimiseButton_Click(object sender, RoutedEventArgs e)
        {
            WindowStyle = WindowStyle.SingleBorderWindow;
            WindowState = WindowState.Minimized;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SetFullscreen(!Fullscreen);
        }

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {

        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.Property == WindowStateProperty)
            {
                if (WindowState != WindowState.Minimized) WindowStyle = WindowStyle.None;
            }
        }

        private void TimeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
        }

        private void TimeSlider_UserValueChanged(object sender, double e)
        {
            if (timeSlider == null) return;
            Time.Navigate(timeSlider.Value);
        }

        void PauseChanged()
        {
            if (Time.Paused)
            {
                pauseButton.Background = new SolidColorBrush(Color.FromArgb(100, 255, 255, 255));
                playButton.Background = new SolidColorBrush(Color.FromArgb(0, 255, 255, 255));
            }
            else
            {
                pauseButton.Background = new SolidColorBrush(Color.FromArgb(0, 255, 255, 255));
                playButton.Background = new SolidColorBrush(Color.FromArgb(100, 255, 255, 255));
            }
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (!Time.Paused)
                Time.Pause();
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (scene.File != null)
                if (Time.Paused)
                    Time.Play();
        }

        private void MainWindow_PreviewDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0)
                {
                    Task.Run(() =>
                    {
                        Thread.Sleep(500);
                        Dispatcher.Invoke(() =>
                        {
                            dropHighlight.Visibility = Visibility.Hidden;
                            LoadMidi(files[0]);
                        });
                    });
                }
            }
        }

        private void MainWindow_PreviewDragEnter(object sender, DragEventArgs e)
        {
            dropHighlight.Visibility = Visibility.Visible;
        }

        private void MainWindow_PreviewDragLeave(object sender, DragEventArgs e)
        {
            dropHighlight.Visibility = Visibility.Hidden;
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SetFullscreen(!Fullscreen);
            }
            if (e.Key == Key.Escape && Fullscreen)
            {
                SetFullscreen(!_fullscreen);
            }
            if (e.Key == Key.Right)
            {
                double time = Time.GetTime() + 5;
                time = Math.Max(time, timeSlider.Minimum);
                time = Math.Min(time, timeSlider.Maximum);
                Time.Navigate(time);
            }
            if (e.Key == Key.Left)
            {
                double time = Time.GetTime() - 5;
                time = Math.Max(time, timeSlider.Minimum);
                time = Math.Min(time, timeSlider.Maximum);
                Time.Navigate(time);
            }
            if (e.Key == Key.Space)
            {
                if (Time.Paused)
                {
                    if (scene.File != null) Time.Play();
                }
                else Time.Pause();
            }
        }
    }
}
