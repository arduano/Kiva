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
    /// Interaction logic for MiscSettings.xaml
    /// </summary>
    public partial class MiscSettings : UserControl
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

        public MiscSettings()
        {
            InitializeComponent();
        }

        void SetValues()
        {
            backgroundColor.Color = settings.General.BackgroundColor;
            barColor.Color = settings.General.BarColor;
            hideInfoCard.IsChecked = settings.General.HideInfoCard;
            windowTopmost.IsChecked = settings.General.MainWindowTopmost;
        }

        private void BackgroundColor_ValueChanged(object sender, RoutedPropertyChangedEventArgs<Color> e)
        {
            if (IsInitialized)
                settings.General.BackgroundColor = backgroundColor.Color;
        }

        private void BarColor_ValueChanged(object sender, RoutedPropertyChangedEventArgs<Color> e)
        {
            if (IsInitialized)
                settings.General.BarColor = barColor.Color;
        }

        private void hideInfoCard_CheckToggled(object sender, RoutedPropertyChangedEventArgs<bool> e)
        {
            settings.General.HideInfoCard = hideInfoCard.IsChecked;
        }

        private void windowTopmost_CheckToggled(object sender, RoutedPropertyChangedEventArgs<bool> e)
        {
            settings.General.MainWindowTopmost = windowTopmost.IsChecked;
        }
    }
}
