using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kiva_MIDI
{
    public class MIDILoaderSettings
    {
        public MIDILoaderSettings()
        {
        }
        public byte EventVelocityThreshold { get; set; } = 0;
        public byte NoteVelocityThreshold { get; set; } = 0;
        public bool SaveMemory { get; set; } = false;
        public int EventPlayerThreads { get; set; } = Environment.ProcessorCount;
    }
}
