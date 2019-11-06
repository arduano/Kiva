using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace KivaShared
{
    /// <summary>
    /// Interaction logic for MessageBox.xaml
    /// </summary>
    public partial class MessageBox : Window
    {
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private const int GWL_STYLE = -16;
        private const int WS_MAXIMIZEBOX = 0x10000;

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            var hwnd = new WindowInteropHelper((Window)sender).Handle;
            var value = GetWindowLong(hwnd, GWL_STYLE);
            SetWindowLong(hwnd, GWL_STYLE, (int)(value & ~WS_MAXIMIZEBOX));
        }

        public MessageBox(string title, string message)
        {
            InitializeComponent();
            titleText.Text = title;
            bodyText.Text = message;
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Window_SourceInitialized(this, null);
            Width = ActualWidth;
            //Height = ActualHeight;
            SizeToContent = SizeToContent.Manual;
            if (Width > 400) Width = 400;
            if(Height > 200)
            {
                double area = Width * Height;
                Width = Math.Sqrt(area) * 2.5;
            }
        }

        public static void Show(string message, string title)
        {
            new MessageBox(title, message).ShowDialog();
        }

        public static void Show(string message, string title, Window parent)
        {
            new MessageBox(title, message) { Owner = parent }.ShowDialog();
        }

        public static void Show(string message)
        {
            Show(message, "");
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if(content.ActualHeight < bodyText.ActualHeight + 20)
            {
                Height = ActualHeight + (bodyText.ActualHeight + 20 - content.ActualHeight);
            }
        }
    }
}
