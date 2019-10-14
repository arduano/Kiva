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
    /// Interaction logic for VisualSettings.xaml
    /// </summary>
    public partial class VisualSettings : UserControl
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

        public VisualSettings()
        {
            InitializeComponent();
        }

        void SetValues()
        {
            var range = settings.General.KeyRange;
            if (range == KeyRangeTypes.Key88) key88Range.IsChecked = true;
            if (range == KeyRangeTypes.Key128) key128Range.IsChecked = true;
            if (range == KeyRangeTypes.Key256) key256Range.IsChecked = true;
            if (range == KeyRangeTypes.KeyMIDI) midiRange.IsChecked = true;
            if (range == KeyRangeTypes.KeyDynamic) dynamicRange.IsChecked = true;
            if (range == KeyRangeTypes.Custom) customRange.IsChecked = true;

            var style = settings.General.KeyboardStyle;
            if (style == KeyboardStyle.Big) bigKeyboard.IsChecked = true;
            if (style == KeyboardStyle.Small) smallKeyboard.IsChecked = true;
            if (style == KeyboardStyle.None) noKeyboard.IsChecked = true;

            fpsLock.Value = settings.General.FPSLock;
            compatibilityFps.IsChecked = settings.General.CompatibilityFPS;

            firstKey.Value = settings.General.CustomFirstKey;
            lastKey.Value = settings.General.CustomLastKey;
        }

        private void RangeChanged(object sender, RoutedEventArgs e)
        {
            if (!IsInitialized) return;
            if (sender == key88Range) settings.General.KeyRange = KeyRangeTypes.Key88;
            if (sender == key128Range) settings.General.KeyRange = KeyRangeTypes.Key128;
            if (sender == key256Range) settings.General.KeyRange = KeyRangeTypes.Key256;
            if (sender == midiRange) settings.General.KeyRange = KeyRangeTypes.KeyMIDI;
            if (sender == dynamicRange) settings.General.KeyRange = KeyRangeTypes.KeyDynamic;
            if (sender == customRange) settings.General.KeyRange = KeyRangeTypes.Custom;
        }
        
        private void KBStyleChanged(object sender, RoutedEventArgs e)
        {
            if (!IsInitialized) return;
            if (sender == noKeyboard) settings.General.KeyboardStyle = KeyboardStyle.None;
            if (sender == smallKeyboard) settings.General.KeyboardStyle = KeyboardStyle.Small;
            if (sender == bigKeyboard) settings.General.KeyboardStyle = KeyboardStyle.Big;
        }

        private void FirstKey_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (!IsInitialized) return;
            settings.General.CustomFirstKey = (int)firstKey.Value;
        }

        private void LastKey_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (!IsInitialized) return;
            settings.General.CustomLastKey = (int)lastKey.Value;
        }

        private void FpsLock_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (!IsInitialized) return;
            settings.General.FPSLock = (int)fpsLock.Value;
        }

        private void CompatibilityFps_CheckToggled(object sender, RoutedPropertyChangedEventArgs<bool> e)
        {
            if (!IsInitialized) return;
            settings.General.CompatibilityFPS = compatibilityFps.IsChecked;
        }
    }
}
