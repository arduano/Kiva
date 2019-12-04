using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
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
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Midi;

namespace Kiva_MIDI
{
    /// <summary>
    /// Interaction logic for PreRenderAudioSettings.xaml
    /// </summary>
    public partial class PreRenderAudioSettings : UserControl
    {
        private Settings settings;

        public Settings Settings
        {
            get => settings; set
            {
                if (settings != null) settings.Soundfonts.SoundfontsUpdated -= DispatcherSetSfs;
                settings = value;
                settings.Soundfonts.SoundfontsUpdated += DispatcherSetSfs;
                SetValues();
            }
        }

        SolidColorBrush selectBrush = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255));
        private DockPanel selectedItem = null;

        DockPanel SelectedItem
        {
            get => selectedItem;
            set
            {
                if (selectedItem != null) selectedItem.Background = Brushes.Transparent;
                selectedItem = value;
                if (selectedItem != null) selectedItem.Background = selectBrush;
            }
        }

        public PreRenderAudioSettings()
        {
            InitializeComponent();
        }

        public void SetValues()
        {
            bufferLength.Value = settings.General.RenderBufferLength;
            voices.Value = settings.General.RenderVoices;
            disableFx.IsChecked = settings.General.RenderNoFx;
            simulatedLag.Value = (decimal)(settings.General.RenderSimulateLag * 1000);
            SetSizeLabel();

            SetSfs();
        }

        public void DispatcherSetSfs(bool reload)
        {
            if (!reload) return;
            Dispatcher.InvokeAsync(SetSfs).Task.GetAwaiter().GetResult();
        }

        public void SetSfs()
        {
            sfList.Children.Clear();
            foreach (var sf in settings.Soundfonts.Soundfonts)
            {
                sfList.Children.Add(MakeSfEntry(sf));
            }
        }

        DockPanel MakeSfEntry(SoundfontData sf)
        {
            var checkBox = new BetterCheckbox()
            {
                IsChecked = sf.enabled,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 0, 0)
            };
            var label = new Label()
            {
                Content = System.IO.Path.GetFileName(sf.path),
                FontSize = 14,
                Padding = new Thickness(3)
            };
            var dock = new DockPanel()
            {
                Tag = sf,
                Background = Brushes.Transparent,
            };
            dock.Children.Add(checkBox);
            dock.Children.Add(label);
            checkBox.CheckToggled += (s, e) =>
            {
                sf.enabled = checkBox.IsChecked;
                UpdateFonts();
            };
            dock.MouseDown += (s, e) => SelectedItem = dock;
            return dock;
        }

        void UpdateFonts()
        {
            List<SoundfontData> list = new List<SoundfontData>();
            foreach (var i in sfList.Children) list.Add((SoundfontData)((FrameworkElement)i).Tag);
            settings.Soundfonts.Soundfonts = list.ToArray();
            settings.Soundfonts.SaveList();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (settings != null) settings.Soundfonts.SoundfontsUpdated -= DispatcherSetSfs;
        }

        private void upButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedItem == null) return;
            var index = sfList.Children.IndexOf(selectedItem);
            if (index == -1)
            {
                selectedItem = null;
                return;
            }
            if (index == 0) return;
            sfList.Children.RemoveAt(index);
            sfList.Children.Insert(index - 1, selectedItem);
            UpdateFonts();
        }

        private void downButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedItem == null) return;
            var index = sfList.Children.IndexOf(selectedItem);
            if (index == -1)
            {
                selectedItem = null;
                return;
            }
            if (index == sfList.Children.Count - 1) return;
            sfList.Children.RemoveAt(index);
            sfList.Children.Insert(index + 1, selectedItem);
            UpdateFonts();
        }

        void SetSizeLabel() => bufferSizeLabel.Content = "(~" + Math.Round(settings.General.RenderBufferLength * 48000 * 2 * 4 / 1000000.0) + "mb)";

        private void bufferLength_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            settings.General.RenderBufferLength = (int)bufferLength.Value;
            SetSizeLabel();
        }

        private void voices_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            settings.General.RenderVoices = (int)voices.Value;
        }

        bool IsValidSF(string path)
        {
            try
            {
                int SFH = BassMidi.BASS_MIDI_FontInit(path, BASSFlag.BASS_DEFAULT);
                BASSError Err = Bass.BASS_ErrorGetCode();

                if (Err == 0)
                {
                    BassMidi.BASS_MIDI_FontFree(SFH);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
        private void addButton_Click(object sender, RoutedEventArgs e)
        {
            var open = new OpenFileDialog();
            open.Filter = "Soundfont Files|*.sf1;*.sf2;*.sfz;*.sfark;*.sfpack;";
            open.Multiselect = true;
            if ((bool)open.ShowDialog())
            {
                foreach (var f in open.FileNames)
                {
                    if (IsValidSF(f))
                    {
                        SoundfontData sf = new SoundfontData(System.IO.Path.GetExtension(f).ToLowerInvariant() == ".sfz");
                        sf.path = f;
                        sfList.Children.Add(MakeSfEntry(sf));
                    }
                    else
                    {
                        MessageBox.Show("Could not load soundfont " + System.IO.Path.GetFileName(f), "Invalid Soundfont");
                    }
                }
                UpdateFonts();
            }
        }

        private void removeButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedItem != null && sfList.Children.Contains(selectedItem))
            {
                sfList.Children.Remove(selectedItem);
                UpdateFonts();
            }
        }

        private void sfPanel_PreviewDragLeave(object sender, DragEventArgs e)
        {
            if (!IsInitialized) return;
            sfPanel.Background = Brushes.Transparent;
        }

        private void sfPanel_PreviewDrop(object sender, DragEventArgs e)
        {
            if (!IsInitialized) return;
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
                            sfPanel.Background = Brushes.Transparent;
                            foreach (var f in files)
                            {
                                var ext = System.IO.Path.GetExtension(f).ToLowerInvariant();
                                if (!(
                                    ext == ".sf1" ||
                                    ext == ".sf2" ||
                                    ext == ".sfz" ||
                                    ext == ".sfark" ||
                                    ext == ".sfpack"
                                )) continue;
                                if (IsValidSF(f))
                                {
                                    SoundfontData sf = new SoundfontData(System.IO.Path.GetExtension(f).ToLowerInvariant() == ".sfz");
                                    sf.path = f;
                                    sfList.Children.Add(MakeSfEntry(sf));
                                }
                                else
                                {
                                    MessageBox.Show("Could not load soundfont " + System.IO.Path.GetFileName(f), "Invalid Soundfont");
                                }
                            }
                        });
                    });
                }
            }
        }

        private void sfPanel_PreviewDragEnter(object sender, DragEventArgs e)
        {
            if (!IsInitialized) return;
            sfPanel.Background = selectBrush;
        }

        private void disableFx_CheckToggled(object sender, RoutedPropertyChangedEventArgs<bool> e)
        {
            settings.General.RenderNoFx = disableFx.IsChecked;
        }

        private void simulatedLag_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            settings.General.RenderSimulateLag = (double)simulatedLag.Value / 1000.0;
        }
    }
}
