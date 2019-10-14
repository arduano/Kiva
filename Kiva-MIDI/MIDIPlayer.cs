﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Sanford.Multimedia.Midi;

namespace Kiva_MIDI
{
    class MIDIPlayer : IDisposable
    {
        [DllImport("ntdll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int NtDelayExecution([MarshalAs(UnmanagedType.I1)] bool alertable, ref Int64 DelayInterval);

        public int BufferLen => eventFeed == null ? 0 : eventFeed.Count;

        public int DeviceID
        {
            get => deviceID;
            set
            {
                if (deviceID != value)
                {
                    deviceID = value;
                    cancelConsumer.Cancel();
                    if (deviceThread != null)
                        deviceThread.GetAwaiter().GetResult();
                    cancelConsumer = new CancellationTokenSource();
                    if (deviceID < 0)
                        deviceThread = Task.Factory.StartNew(() => RunEventConsumerKDMAPI(cancelConsumer.Token), TaskCreationOptions.LongRunning);
                    else
                        deviceThread = Task.Factory.StartNew(() => RunEventConsumerWINMM(cancelConsumer.Token), TaskCreationOptions.LongRunning);
                }
            }
        }

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
        private int deviceID = -1;

        OutputDevice device = null;
        Task deviceThread = null;

        CancellationTokenSource cancelConsumer;

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
                cancelConsumer = new CancellationTokenSource();
                if (deviceID < 0)
                    deviceThread = Task.Factory.StartNew(() => RunEventConsumerKDMAPI(cancelConsumer.Token), TaskCreationOptions.LongRunning);
                else
                    deviceThread = Task.Factory.StartNew(() => RunEventConsumerWINMM(cancelConsumer.Token), TaskCreationOptions.LongRunning);
                while (!disposed)
                {
                    SpinWait.SpinUntil(() => file != null || disposed);
                    try
                    {
                        tasks.Add(RunPlayerThread(-1));
                        for (int i = 0; i < file.MIDINoteEvents.Length; i++)
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
            return Task.Factory.StartNew(() =>
            {
                MIDIEvent[] events;
                if (i == -1)
                    try
                    {
                        events = file.MIDIControlEvents;
                    }
                    catch { return; }
                else
                    try
                    {
                        events = file.MIDINoteEvents[i];
                    }
                    catch { return; }
                if (events.Length == 0) return;
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
                        while (Time.Paused)
                        {
                            Thread.Sleep(50);
                        }
                        evid = GetEventPos(events, time) - 10;
                        if (evid < 0 || i == -1) evid = 0;
                    }
                    while (evid == events.Length && !changed) { Thread.Sleep(50); }
                    if (changed || lastTime > time)
                    {
                        time = Time.GetTime();
                        if (deviceID == 01)
                            KDMAPI.ResetKDMAPIStream();
                        else if (device != null)
                            try
                            {
                                device.Reset();
                            }
                            catch { }

                        evid = GetEventPos(events, time) - 10;
                        if (evid < 0 || i == -1) evid = 0;
                        changed = false;
                    }
                    lastTime = time;
                    if (evid < events.Length)
                    {
                        var ev = events[evid];
                        var delay = (ev.time - time) / Time.Speed;
                        while (delay > 0.2 && file != null)
                        {
                            Thread.Sleep(20);
                            delay = (ev.time - Time.GetTime()) / Time.Speed;
                            if (changed) break;
                            if (file == null) goto dispose;
                        }
                        if (changed) goto reset;
                        if (delay > 0)
                        {
                            Int64 s = (long)(delay * -10000000);
                            NtDelayExecution(false, ref s);
                            //Thread.Sleep(new TimeSpan((long)(delay * 10000000)));
                        }
                        if ((eventFeed.Count > 10000 || delay < -1) && i != -1)
                            while ((evid < events.Length && events[evid].time < Time.GetTime() && (eventFeed.Count > 10000 || delay < -1)))
                            {
                                evid++;
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
                dispose:
                Time.TimeChanged -= onChanged;
            }, TaskCreationOptions.LongRunning);
        }

        void RunEventConsumerKDMAPI(CancellationToken cancel)
        {
            KDMAPI.InitializeKDMAPIStream();
            try
            {
                foreach (var e in eventFeed.GetConsumingEnumerable(cancel))
                {
                    KDMAPI.SendDirectDataNoBuf(e.data);
                    if (deviceID != -1) break;
                }
            }
            catch (OperationCanceledException) { }
            KDMAPI.TerminateKDMAPIStream();
        }

        void RunEventConsumerWINMM(CancellationToken cancel)
        {
            var id = deviceID;
            var device = new OutputDevice(id);
            this.device = device;
            try
            {
                foreach (var e in eventFeed.GetConsumingEnumerable(cancel))
                {
                    device.SendShort((int)e.data);
                    if (deviceID != id) break;
                }
            }
            catch (OperationCanceledException) { }
            device.Close();
            device.Dispose();
            this.device = null;
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