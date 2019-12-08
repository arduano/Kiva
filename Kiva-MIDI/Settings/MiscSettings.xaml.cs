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
            discordRP.IsChecked = settings.General.DiscordRP;

            var cp = settings.General.InfoCardParams;

            timeLabel.IsChecked = (cp & CardParams.Time) > 0;
            renderedNotesLabel.IsChecked = (cp & CardParams.RenderedNotes) > 0;
            polyphonyLabel.IsChecked = (cp & CardParams.Polyphony) > 0;
            npsLabel.IsChecked = (cp & CardParams.NPS) > 0;
            ncLabel.IsChecked = (cp & CardParams.NoteCount) > 0;
            fpsLabel.IsChecked = (cp & CardParams.FPS) > 0;
            estimatedFpsLabel.IsChecked = (cp & CardParams.FakeFps) > 0;
            bufferLengthLabel.IsChecked = (cp & CardParams.AudioBuffer) > 0;
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

        private void cardLabel_CheckToggled(object sender, RoutedPropertyChangedEventArgs<bool> e)
        {
            CardParams cp = 0;

            if (timeLabel.IsChecked) cp |= CardParams.Time;
            if (renderedNotesLabel.IsChecked) cp |= CardParams.RenderedNotes;
            if (polyphonyLabel.IsChecked) cp |= CardParams.Polyphony;
            if (npsLabel.IsChecked) cp |= CardParams.NPS;
            if (ncLabel.IsChecked) cp |= CardParams.NoteCount;
            if (fpsLabel.IsChecked) cp |= CardParams.FPS;
            if (estimatedFpsLabel.IsChecked) cp |= CardParams.FakeFps;
            if (bufferLengthLabel.IsChecked) cp |= CardParams.AudioBuffer;

            settings.General.InfoCardParams = cp;
        }

        private void discordRP_CheckToggled(object sender, RoutedPropertyChangedEventArgs<bool> e)
        {
            settings.General.DiscordRP = discordRP.IsChecked;
        }
    }
}
