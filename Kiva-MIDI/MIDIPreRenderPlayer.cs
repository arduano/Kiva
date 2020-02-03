using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kiva_MIDI
{
    class MIDIPreRenderPlayer : IDisposable
    {
        Settings settings;

        public int SkippingVelocity => ma.SkippingVelocity;
        public double BufferSeconds => ma.BufferSeconds;

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
                    Time.SpeedChanged -= OnSpeedChanged;
                }
                time = value;
                Time.TimeChanged += OnTimeChange;
                Time.PauseChanged += OnPauseChange;
                Time.SpeedChanged += OnSpeedChanged;
                ma.Paused = Time.Paused;
            }
        }
        MIDIAudio ma;
        private PlayingState time = new PlayingState();

        Task syncThread;
        bool disposed = false;

        IEnumerable<MIDIEvent> EventArrayEnumerable(MIDIEvent[] array, double time, List<Action<double, int>> skipList)
        {
            int i = GetEventPos(array, time) - 1;
            if (i < 0) i = 0;
            skipList.Add((t, v) =>
            {
                if (v > 127) v = 127;
                while (i != array.Length)
                {
                    var ev = array[i];
                    if (ev.time > t && ev.vel > v) break;
                    if (ev.time - t > (128 - ev.vel + 10) / 48000.0 * 50) break;
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
            if (_file.MIDIControlEvents.Length != 0)
                events.Add(_file.MIDIControlEvents);
            for (int i = 0; i < _file.MIDINoteEvents.Length; i++)
                if (_file.MIDINoteEvents[i].Length != 0)
                    events.Add(EventArrayEnumerable(_file.MIDINoteEvents[i], time, skipList));
            return TimedMerger<MIDIEvent>.MergeMany(events.ToArray(), e => e.time);
        }

        void OnSpeedChanged()
        {
            StartRender(true);
        }

        void OnTimeChange()
        {
            StartRender(false);
        }

        void OnPauseChange()
        {
            ma.Paused = Time.Paused;
            ma.SyncPlayer(Time.GetTime(), Time.Speed);
        }

        void StartRender(bool force)
        {
            if (file == null) return;

            if (!force)
            {
                var time = Time.GetTime();
                if (time + 0.1 > ma.PlayerTime + ma.BufferSeconds || time + 0.01 < ma.PlayerTime)
                {
                    force = true;
                }
            }

            if (force)
            {
                var skipList = new List<Action<double, int>>();
                ma.Start(Time.GetTime(), EventStream(Time.GetTime(), skipList), time.Speed, (t, v) => skipList.ForEach(s => s(t, v)));
            }
            else
            {
                ma.SyncPlayer(Time.GetTime(), Time.Speed);
            }
        }

        public MIDIPreRenderPlayer(Settings settings)
        {
            this.settings = settings;
            ma = new MIDIAudio(48000 * settings.General.RenderBufferLength);
            Time.TimeChanged += OnTimeChange;
            Time.PauseChanged += OnPauseChange;

            ma.defaultVoices = settings.General.RenderVoices;
            ma.defaultNoFx = settings.General.RenderNoFx;
            ma.simulatedLagScale = settings.General.RenderSimulateLag;

            BASSMIDI.LoadSoundfonts(settings.Soundfonts.Soundfonts);

            settings.Soundfonts.SoundfontsUpdated += OnSoundfontsChanged;

            syncThread = Task.Run(() =>
            {
                while (!disposed)
                {
                    if (file != null && !Time.Paused)
                    {
                        var time = Time.GetTime();
                        if (time + 0.1 < ma.PlayerTime)
                        {
                            ma.SyncPlayer(Time.GetTime(), Time.Speed);
                        }
                    }
                    Thread.Sleep(100);
                }
            });
            settings.General.PropertyChanged += OnSettingsPropertyChanged;
        }

        private void OnSoundfontsChanged(bool reload)
        {
            ma.Paused = true;
            ma.Stop();
            BASSMIDI.LoadSoundfonts(settings.Soundfonts.Soundfonts);
            ma.Paused = Time.Paused;
            StartRender(true);
        }

        public void Dispose()
        {
            ma.Dispose();
            BASSMIDI.FreeSoundfonts();
            disposed = true;
            syncThread.GetAwaiter().GetResult();
            settings.Soundfonts.SoundfontsUpdated -= OnSoundfontsChanged;
            settings.General.PropertyChanged -= OnSettingsPropertyChanged;

            Time.TimeChanged -= OnTimeChange;
            Time.PauseChanged -= OnPauseChange;
            Time.SpeedChanged -= OnSpeedChanged;

            GC.Collect(2, GCCollectionMode.Forced);
        }

        void OnSettingsPropertyChanged(object s, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "RenderVoices")
            {
                ma.defaultVoices = settings.General.RenderVoices;
                StartRender(true);
            }
            if (e.PropertyName == "RenderNoFx")
            {
                ma.defaultNoFx = settings.General.RenderNoFx;
                StartRender(true);
            }
            if (e.PropertyName == "RenderBufferLength")
            {
                ma.ResizeBuffer(settings.General.RenderBufferLength * 48000);
                StartRender(true);
            }
            if (e.PropertyName == "RenderSimulateLag")
            {
                ma.simulatedLagScale = settings.General.RenderSimulateLag;
                StartRender(true);
            }
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
