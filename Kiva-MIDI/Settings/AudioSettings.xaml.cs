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
using Sanford.Multimedia.Midi;

namespace Kiva_MIDI
{
    /// <summary>
    /// Interaction logic for AudioSettings.xaml
    /// </summary>
    public partial class AudioSettings : UserControl
    {
        struct DeviceData
        {
            public int id;
            public string name;
        }

        private Settings settings;

        public Settings Settings
        {
            get => settings; set
            {
                settings = value;
                SetValues();
            }
        }

        bool kdmapiDisabled = false;
        SolidColorBrush selectBrush = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255));

        public AudioSettings()
        {
            try
            {
                kdmapiDisabled = !KDMAPI.IsKDMAPIAvailable();
            }
            catch { kdmapiDisabled = false; }
            InitializeComponent();

            for (int i = -1; i < OutputDevice.DeviceCount; i++)
            {
                string name;
                if (i == -1)
                {
                    if (kdmapiDisabled) continue;
                    name = "KDMAPI (direct API)";
                }
                else
                {
                    var device = OutputDevice.GetDeviceCapabilities(i);
                    name = device.name;
                }
                var item = new ListBoxItem()
                {
                    Tag = new DeviceData() { id = i, name = name },
                    Padding = new Thickness(0),
                    Content = new RippleEffectDecorator()
                    {
                        Content = new Label
                        {
                            Content = name
                        }
                    }
                };
                item.PreviewMouseDown += (s, e) =>
                {
                    ClearSelectedDevice();
                    SelectDevice(devicesList.Items.IndexOf(item));
                };
                devicesList.Items.Add(item);
            }

            ClearSelectedDevice();
        }

        public void SelectDevice(int index)
        {
            ClearSelectedDevice();
            ((ContentControl)devicesList.Items[index]).Background = selectBrush;
            var tag = (DeviceData)((ContentControl)devicesList.Items[index]).Tag;
            devicesList.SelectedItem = index;
            settings.General.SelectedMIDIDevice = tag.id;
            settings.General.SelectedMIDIDeviceName = tag.name;
        }

        public void SetValues()
        {
            ClearSelectedDevice();
            int i = 0;
            bool selected = false;
            foreach (var b in devicesList.Items.Cast<ContentControl>())
            {
                var tag = (DeviceData)b.Tag;
                if (tag.name == settings.General.SelectedMIDIDeviceName)
                {
                    if (settings.General.SelectedMIDIDevice == tag.id)
                    {
                        SelectDevice(i);
                        selected = true;
                        break;
                    }
                }
                i++;
            }
            if (!selected)
            {
                SelectDevice(0);
            }
        }

        public void ClearSelectedDevice()
        {
            foreach (var b in devicesList.Items.Cast<ContentControl>()) b.Background = Brushes.Transparent;
        }
    }
}
