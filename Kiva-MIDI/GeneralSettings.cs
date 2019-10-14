using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    public class GeneralSettings : INotifyPropertyChanged
    {
        public KeyRangeTypes KeyRange { get; set; } = KeyRangeTypes.KeyDynamic;
        public int CustomFirstKey { get; set; } = 0;
        public int CustomLastKey { get; set; } = 127;

        public KeyboardStyle KeyboardStyle { get; set; } = KeyboardStyle.Small;

        public int FPSLock { get; set; } = 0;
        public bool CompatibilityFPS { get; set; } = false;

        public int SelectedMIDIDevice { get; set; } = -1;
        public string SelectedMIDIDeviceName { get; set; } = "KDMAPI";

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
