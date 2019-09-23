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
        public MIDIFile File { get; set; }
        public PlayingState Time { get; set; } = new PlayingState();
        bool disposed = false;
        Task playerThread = null;

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
                    SpinWait.SpinUntil(() => File != null || disposed);
                    try
                    {
                        object l = new object();
                        List<Task> tasks = new List<Task>();
                        for (int i = 0; i < File.MIDIEvents.Length; i++)
                        {
                            tasks.Add(RunPlayerThread(i, l));
                        }
                        foreach (var t in tasks) t.GetAwaiter().GetResult();
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
                var events = File.MIDIEvents[i];
                int evid = 0;
                double lastTime = 0;
                while (File != null)
                {
                    if (Time.Paused) SpinWait.SpinUntil(() => !Time.Paused);
                    double time = Time.GetTime();
                    if (time < lastTime)
                    {
                        evid = GetEventPos(events, time);
                    }
                    lastTime = time;
                    evid++;
                    if (evid < events.Length)
                    {
                        var ev = events[evid];
                        var delay = ev.time - time;
                        if (delay > 0.5) SpinWait.SpinUntil(() =>
                        {
                            time = ev.time - Time.GetTime();
                            return time > 0.5;
                        });
                        if (delay > 0) Thread.Sleep(new TimeSpan((long)(delay * 10000000)));
                        if (delay < -10)
                        {
                            lastTime = ev.time;
                            evid = GetEventPos(events, time);
                            continue;
                        }
                        if (delay < -0.01) while (evid < events.Length && (events[evid].time - time < -3)) KDMAPI.SendDirectData(events[evid++].data);
                        else
                            KDMAPI.SendDirectData(ev.data);
                    }
                    else
                    {
                        SpinWait.SpinUntil(() => Time.GetTime() < time);
                    }
                }
            });
        }

        int GetEventPos(MIDIEvent[] events, double time)
        {
            int min = 0;
            int max = events.Length - 1;
            while (min <= max)
            {
                int mid = (min + max) / 2;
                if (events[mid].time > time)
                {
                    max = mid - 1;
                }
                else if (events[mid + 1].time < time)
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
