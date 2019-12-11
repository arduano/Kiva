using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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
    /// Interaction logic for WinMMAudioSettings.xaml
    /// </summary>
    public partial class WinMMAudioSettings : UserControl
    {
        struct DeviceData
        {
            public uint id;
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

        SolidColorBrush selectBrush = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255));

        public WinMMAudioSettings()
        {
            InitializeComponent();

            var devCount = WinMM.midiOutGetNumDevs();

            for (uint i = 0; i < devCount; i++)
            {
                string name;
                MIDIOUTCAPS device;
                WinMM.midiOutGetDevCaps(i, out device, (uint)Marshal.SizeOf(typeof(MIDIOUTCAPS)));
                name = device.szPname;
                var item = new Grid()
                {
                    Tag = new DeviceData() { id = i, name = name },
                };
                item.Children.Add(
                    new RippleEffectDecorator()
                    {
                        Content = new Label
                        {
                            FontSize = 14,
                            HorizontalContentAlignment = HorizontalAlignment.Center,
                            Content = name
                        }
                    }
                );
                item.PreviewMouseDown += (s, e) =>
                {
                    ClearSelectedDevice();
                    SelectDevice(devicesList.Children.IndexOf(item));
                };
                devicesList.Children.Add(item);
            }

            ClearSelectedDevice();
        }

        public void SelectDevice(int index)
        {
            ClearSelectedDevice();
            ((Grid)devicesList.Children[index]).Background = selectBrush;
            var tag = (DeviceData)((Grid)devicesList.Children[index]).Tag;
            settings.General.SelectedMIDIDevice = (int)tag.id;
            settings.General.SelectedMIDIDeviceName = tag.name;
        }

        public void SetValues()
        {
            ClearSelectedDevice();
            int i = 0;
            bool selected = false;
            foreach (var b in devicesList.Children.Cast<Grid>())
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
            foreach (var b in devicesList.Children.Cast<Grid>()) b.Background = Brushes.Transparent;
        }
    }
}
