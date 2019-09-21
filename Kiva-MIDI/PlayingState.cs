using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kiva_MIDI
{
    class PlayingState
    {
        public DateTime Time { get; set; }
        public double MIDITime { get; set; }
        public bool Paused { get; set; }

        public void Pause()
        {
            if (Paused) return;
            MIDITime += (DateTime.Now - Time).TotalSeconds;
            Paused = true;
        }
        
        public void Play()
        {
            Time = DateTime.Now;
            Paused = false;
        }

        public void Reset()
        {
            MIDITime = 0;
            Paused = true;
        }

        public double GetTime()
        {
            if (Paused) return MIDITime;
            return MIDITime + (DateTime.Now - Time).TotalSeconds;
        }
    }
}
