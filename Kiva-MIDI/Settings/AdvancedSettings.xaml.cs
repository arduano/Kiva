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

namespace Kiva_MIDI
{
    /// <summary>
    /// Interaction logic for AdvancedSettings.xaml
    /// </summary>
    public partial class AdvancedSettings : UserControl
    {
        private Settings settings;

        public Settings Settings
        {
            get => settings; set
            {
                settings = value;
                SetValues();
            }
        }

        public AdvancedSettings()
        {
            InitializeComponent();
        }

        bool valuesSet = false;

        public void SetValues()
        {
            threadCount.Maximum = Environment.ProcessorCount;
            threadCount.Value = settings.General.MaxRenderThreads;
            forceSingleThread.IsChecked = !settings.General.MultiThreadedRendering;
            disableTransparency.IsChecked = settings.General.DisableTransparency;
            valuesSet = true;
        }

        private void disableTransparency_CheckToggled(object sender, RoutedPropertyChangedEventArgs<bool> e)
        {
            if (!valuesSet) return;
            settings.General.DisableTransparency = disableTransparency.IsChecked;
        }

        private void forceSingleThread_CheckToggled(object sender, RoutedPropertyChangedEventArgs<bool> e)
        {
            if (!valuesSet) return;
            settings.General.MultiThreadedRendering = !forceSingleThread.IsChecked;
        }

        private void threadCount_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (!valuesSet) return;
            settings.General.MaxRenderThreads = (int)threadCount.Value;
        }
    }
}
