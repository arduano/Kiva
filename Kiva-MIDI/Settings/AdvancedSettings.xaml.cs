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

        public void SetValues()
        {
            disableTransparency.IsChecked = settings.General.DisableTransparency;
        }

        private void disableTransparency_CheckToggled(object sender, RoutedPropertyChangedEventArgs<bool> e)
        {
            settings.General.DisableTransparency = disableTransparency.IsChecked;
        }
    }
}
