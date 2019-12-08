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
        public int EventPlayerThreads { get; set; } = Environment.ProcessorCount;
        public bool RemoveOverlaps { get; set; } = false;

        public MIDILoaderSettings Clone()
        {
            return new MIDILoaderSettings()
            {
                EventVelocityThreshold = EventVelocityThreshold,
                NoteVelocityThreshold = NoteVelocityThreshold,
                EventPlayerThreads = EventPlayerThreads,
                RemoveOverlaps = RemoveOverlaps
            };
        }
    }
}
