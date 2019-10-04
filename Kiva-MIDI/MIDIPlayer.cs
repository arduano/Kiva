using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Sanford.Multimedia.Midi;

namespace Kiva_MIDI
{
    class MIDIPlayer : IDisposable
    {
        public int BufferLen => eventFeed == null ? 0 : eventFeed.Count;

        public MIDIFile File
        {
            get => file;
            set
            {
                Task.Run(() =>
                {
                    lock (this)
                    {
                        if (file != null)
                        {
                            file = null;
                            lock (tasks)
                            {
                                foreach (var t in tasks) t.GetAwaiter().GetResult();
                                tasks.Clear();
                            }
                        }
                        file = value;
                    }
                });
            }
        }

        BlockingCollection<MIDIEvent> eventFeed;

        MIDIFile file = null;
        public PlayingState Time { get; set; } = new PlayingState();
        bool disposed = false;
        Task playerThread = null;

        List<Task> tasks = new List<Task>();

        public void Dispose()
        {
            File = null;
            disposed = true;
            playerThread.GetAwaiter().GetResult();
        }

        public void RunPlayer()
        {
            playerThread = Task.Run(() =>
            {
                eventFeed = new BlockingCollection<MIDIEvent>();
                Task.Run(RunEventConsumerKDMAPI);
                while (!disposed)
                {
                    SpinWait.SpinUntil(() => file != null || disposed);
                    try
                    {
                        for (int i = 0; i < file.MIDIEvents.Length; i++)
                        {
                            tasks.Add(RunPlayerThread(i));
                        }
                        lock (tasks)
                        {
                            foreach (var t in tasks) t.GetAwaiter().GetResult();
                            lock (tasks)
                                tasks.Clear();
                        }
                    }
                    catch { }
                }
                eventFeed.CompleteAdding();
            });
        }

        Task RunPlayerThread(int i)
        {
            return Task.Run(() =>
            {
                var events = file.MIDIEvents[i];
                int evid = 0;
                double lastTime = 0;
                bool changed = true;
                Action onChanged = () => changed = true;
                Time.TimeChanged += onChanged;
                while (file != null)
                {
                    reset:
                    double time = Time.GetTime();
                    if (Time.Paused)
                    {
                        evid = GetEventPos(events, time);
                        while (Time.Paused)
                        {
                            Thread.Sleep(50);
                        }
                    }
                    if (changed || lastTime > time)
                    {
                        time = Time.GetTime();
                        KDMAPI.ResetKDMAPIStream();
                        evid = GetEventPos(events, time) - 10;
                        if (evid < 0) evid = 0;
                        changed = false;
                    }
                    lastTime = time;
                    if (evid < events.Length)
                    {
                        var ev = events[evid];
                        var delay = (ev.time - time) / Time.Speed;
                        while (delay > 0.2)
                        {
                            Thread.Sleep(20);
                            delay = (ev.time - Time.GetTime()) / Time.Speed;
                            if (changed) break;
                        }
                        if (changed) goto reset;
                        if (delay > 0)
                            Thread.Sleep(new TimeSpan((long)(delay * 10000000)));
                        if (eventFeed.Count > 10000 || delay < -1)
                            while ((evid < events.Length && events[evid].time < Time.GetTime() && (eventFeed.Count > 10000 || delay < -1)))
                            {
                                var d = events[evid++];
                                var de = d.data & 0xF0;
                                if (!(de == 0x90 || de == 0x80))
                                    eventFeed.Add(d);
                            }
                        else
                            eventFeed.Add(ev);
                    }
                    else
                    {
                        while (Time.GetTime() > events[events.Length - 1].time)
                        {
                            Thread.Sleep(50);
                        }
                        evid = GetEventPos(events, time);
                    }
                    evid++;
                }
                Time.TimeChanged -= onChanged;
            });
        }

        void RunEventConsumerKDMAPI()
        {
            KDMAPI.InitializeKDMAPIStream();
            foreach (var e in eventFeed.GetConsumingEnumerable())
            {
                KDMAPI.SendDirectData(e.data);
            }
            KDMAPI.TerminateKDMAPIStream();
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
