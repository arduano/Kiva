using KivaShared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace KivaInstaller
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public Exception exception = null;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Task.Run(() =>
            {
                try
                {
                    var data = KivaUpdates.DownloadAssetData(KivaUpdates.DataAssetName);
                    Dispatcher.Invoke(() => progressText.Content = "Installing...");
                    KivaUpdates.InstallFromStream(data);
                    data.Close();
                    Program.FinalizeInstall();
                    Dispatcher.Invoke(() => Close());
                }
                catch (Exception ex)
                {
                    exception = ex;
                    Dispatcher.Invoke(() => Close());
                }
            });
        }
    }
}
