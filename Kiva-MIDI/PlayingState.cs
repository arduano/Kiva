using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kiva_MIDI
{
    public class PlayingState
    {
        public DateTime Time { get; private set; } = DateTime.UtcNow;
        public double MIDITime { get; private set; } = 0;
        public bool Paused { get; private set; } = true;
        public double Speed { get; private set; } = 1;

        public event Action TimeChanged;
        public event Action PauseChanged;
        public event Action SpeedChanged;

        public void Pause()
        {
            if (Paused) return;
            MIDITime += (DateTime.UtcNow - Time).TotalSeconds * Speed;
            var pause = Paused;
            Paused = true;
            TimeChanged?.Invoke();
            if (!pause) PauseChanged?.Invoke();
        }

        public void Play()
        {
            Time = DateTime.UtcNow;
            var pause = Paused;
            Paused = false;
            if (pause) PauseChanged?.Invoke();
        }

        public void Reset()
        {
            MIDITime = 0;
            var pause = Paused;
            Paused = true;
            TimeChanged?.Invoke();
            if (!pause) PauseChanged?.Invoke();
        }

        public double GetTime()
        {
            if (Paused) return MIDITime;
            return MIDITime + (DateTime.UtcNow - Time).TotalSeconds * Speed;
        }

        public void Navigate(double time)
        {
            Time = DateTime.UtcNow;
            MIDITime = time;
            TimeChanged?.Invoke();
        }

        public void ChangeSpeed(double speed)
        {
            MIDITime = GetTime();
            Time = DateTime.UtcNow;
            Speed = speed;
            SpeedChanged?.Invoke();
        }
    }
}
