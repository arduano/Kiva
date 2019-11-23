using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Kiva_MIDI
{
    public enum KeyRangeTypes
    {
        Key88,
        Key128,
        Key256,
        KeyMIDI,
        KeyDynamic,
        Custom
    }

    public enum KeyboardStyle
    {
        None,
        Big,
        Small,
    }

    public enum AudioEngine
    {
        KDMAPI,
        WinMM,
        PreRender
    }

    public class GeneralSettings : INotifyPropertyChanged
    {
        public KeyRangeTypes KeyRange { get; set; } = KeyRangeTypes.KeyDynamic;
        public int CustomFirstKey { get; set; } = 0;
        public int CustomLastKey { get; set; } = 127;

        public KeyboardStyle KeyboardStyle { get; set; } = KeyboardStyle.Big;

        public int FPSLock { get; set; } = 60;
        public bool CompatibilityFPS { get; set; } = false;

        public Color BackgroundColor { get; set; } = Color.FromArgb(255, 142, 142, 142);
        public Color ForegroundColor { get; set; } = Color.FromArgb(255, 142, 142, 142);
        public Color BarColor { get; set; } = Color.FromArgb(255, 255, 0, 0);

        public string PaletteName { get; set; } = "Random.png";
        public bool PaletteRandomized { get; set; } = true;

        public bool HideInfoCard { get; set; } = false;

        public bool MainWindowTopmost { get; set; } = false;

        public AudioEngine SelectedAudioEngine { get; set; } = AudioEngine.PreRender;

        public int RenderBufferLength { get; set; } = 60;
        public int RenderVoices { get; set; } = 1000;

        public int SelectedMIDIDevice { get; set; } = -1;
        public string SelectedMIDIDeviceName { get; set; } = "";

        public event PropertyChangedEventHandler PropertyChanged;

        public GeneralSettings()
        {
            try
            {
                SelectedMIDIDevice = KDMAPI.IsKDMAPIAvailable() ? -1 : 0;
            }
            catch { SelectedMIDIDevice = 0; }
        }
    }
}
