using System;
using System.Collections.Generic;
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

    public class GeneralSettings
    {
        public KeyRangeTypes KeyRange { get; set; } = KeyRangeTypes.KeyDynamic;
    }
}
