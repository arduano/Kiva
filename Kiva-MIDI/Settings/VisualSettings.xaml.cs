using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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

        SolidColorBrush selectBrush = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255));

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
            //compatibilityFps.IsChecked = settings.General.CompatibilityFPS;

            var first = settings.General.CustomFirstKey;
            var last = settings.General.CustomLastKey;

            firstKey.Value = first;
            lastKey.Value = last;

            syncFps.IsChecked = settings.General.SyncFPS;
            fpsLock.IsEnabled = !syncFps.IsChecked;

            randomisePaletteOrder.IsChecked = settings.General.PaletteRandomized;

            SetPalettes();
        }

        void SetPalettes()
        {
            foreach (var p in settings.PaletteSettings.Palettes.Keys)
            {
                var item = new Grid()
                {
                    Tag = p,
                    Background = Brushes.Transparent
                };
                item.Children.Add(
                    new RippleEffectDecorator()
                    {
                        Content = new Label
                        {
                            Content = p
                        }
                    }
                );
                item.PreviewMouseDown += (s, e) =>
                {
                    if (settings.General.PaletteName == p) return;
                    foreach (var i in palettesPanel.Children.Cast<Grid>()) i.Background = Brushes.Transparent;
                    settings.General.PaletteName = p;
                    item.Background = selectBrush;
                };
                if (p == settings.General.PaletteName)
                    item.Background = selectBrush;
                palettesPanel.Children.Add(item);
            }
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
            //settings.General.CompatibilityFPS = compatibilityFps.IsChecked;
        }

        private void OpenPaletteFolder_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Process.Start("explorer.exe", System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "Palettes\\"));
        }

        private void ReloadPalettes_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            palettesPanel.Children.Clear();
            settings.PaletteSettings.Reload();
            if (!settings.PaletteSettings.Palettes.Keys.Contains(settings.General.PaletteName))
                settings.General.PaletteName = settings.PaletteSettings.Palettes.Keys.First();

            SetPalettes();
        }

        private void RandomisePaletteOrder_CheckToggled(object sender, RoutedPropertyChangedEventArgs<bool> e)
        {
            if (!IsInitialized) return;
            settings.General.PaletteRandomized = randomisePaletteOrder.IsChecked;
        }

        private void syncFps_CheckToggled(object sender, RoutedPropertyChangedEventArgs<bool> e)
        {
            if (!IsInitialized) return;
            settings.General.SyncFPS = syncFps.IsChecked;
            fpsLock.IsEnabled = !syncFps.IsChecked;
        }
    }
}
