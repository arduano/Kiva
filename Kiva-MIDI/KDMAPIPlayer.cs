using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kiva_MIDI
{
    class KDMAPIPlayer : IDisposable
    {
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
                KDMAPI.InitializeKDMAPIStream();
                while (!disposed)
                {
                    SpinWait.SpinUntil(() => file != null || disposed);
                    try
                    {
                        object l = new object();
                        for (int i = 0; i < file.MIDIEvents.Length; i++)
                        {
                            tasks.Add(RunPlayerThread(i, l));
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
                KDMAPI.TerminateKDMAPIStream();
            });
        }

        Task RunPlayerThread(int i, object l)
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
                        KDMAPI.ResetKDMAPIStream();
                        evid = GetEventPos(events, time);
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
                        if (delay > 0) Thread.Sleep(new TimeSpan((long)(delay * 10000000)));
                        if (delay < -0.05)
                            while (evid < events.Length && (events[evid].time - time < -0.05)) KDMAPI.SendDirectData(events[evid++].data);
                        else
                            KDMAPI.SendDirectData(ev.data);
                    }
                    else
                    {
                        while(Time.GetTime() > events[events.Length - 1].time)
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
