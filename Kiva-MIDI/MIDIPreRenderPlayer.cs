using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kiva_MIDI
{
    class MIDIPreRenderPlayer : IDisposable
    {
        Settings settings;


        public MIDIFile File
        {
            get => file;
            set
            {
                lock (this)
                {
                    file = value;
                    if (file != null)
                    {
                        StartRender(true);
                    }
                    else
                    {
                        ma.Stop();
                    }
                }
            }
        }

        MIDIFile file = null;
        public PlayingState Time
        {
            get => time;
            set
            {
                if (time != null)
                {
                    Time.TimeChanged -= OnTimeChange;
                    Time.PauseChanged -= OnPauseChange;
                }
                time = value;
                Time.TimeChanged += OnTimeChange;
                Time.PauseChanged += OnPauseChange;
            }
        }
        MIDIAudio ma;
        private PlayingState time = new PlayingState();

        IEnumerable<MIDIEvent> EventArrayEnumerable(MIDIEvent[] array, double time, List<Action<double, int>> skipList)
        {
            int i = GetEventPos(array, time);
            skipList.Add((t, v) =>
            {
                if (v > 127) v = 127;
                while(i != array.Length)
                {
                    var ev = array[i];
                    if (ev.time > t && ev.vel >= v) break;
                    if (ev.time - t > (v + 1) / 48000.0 * 100) break;
                    i++;
                }
            });
            while (i != array.Length)
            {
                yield return array[i++];
            }
        }

        IEnumerable<MIDIEvent> EventStream(double time, List<Action<double, int>> skipList)
        {
            var _file = (MIDIMemoryFile)file;
            List<IEnumerable<MIDIEvent>> events = new List<IEnumerable<MIDIEvent>>();
            events.Add(_file.MIDIControlEvents);
            for (int i = 0; i < _file.MIDINoteEvents.Length; i++)
                events.Add(EventArrayEnumerable(_file.MIDINoteEvents[i], time, skipList));
                //events.Add(new SkipIterator<MIDIEvent>(_file.MIDINoteEvents[i], GetEventPos(_file.MIDINoteEvents[i], time), 1));
            return TimedMerger<MIDIEvent>.MergeMany(events.ToArray(), e => e.time);
        }

        void OnTimeChange()
        {
            StartRender(false);
        }

        void OnPauseChange()
        {

        }

        void StartRender(bool force)
        {
            if (file == null) return;
            var skipList = new List<Action<double, int>>();
            ma.Start(Time.GetTime(), EventStream(Time.GetTime(), skipList), (t, v) => skipList.ForEach(s => s(t, v)));
        }

        public MIDIPreRenderPlayer(Settings settings)
        {
            this.settings = settings;
            ma = new MIDIAudio(48000 * 60);
            Time.TimeChanged += OnTimeChange;
            Time.PauseChanged += OnPauseChange;
        }

        public void Dispose()
        {
            ma.Dispose();
        }

        int GetEventPos(MIDIEvent[] events, double time)
        {
            if (time < events[0].time) return 0;
            if (time > events[events.Length - 1].time) return events.Length;
            int min = 0;
            int max = events.Length - 1;
            while (min <= max)
            {
                int mid = (min + max) / 2;
                if (events[mid].time > time)
                {
                    max = mid - 1;
                }
                else if (mid + 1 != events.Length && events[mid + 1].time < time)
                {
                    min = mid + 1;
                }
                else
                {
                    return mid;
                }
            }
            return events.Length;
        }
    }
}
