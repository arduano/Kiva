using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kiva_MIDI
{
    public class MIDIMemoryFile : MIDIFile
    {
        public MIDIEvent[][] MIDINoteEvents { get; private set; } = null;
        public MIDIEvent[] MIDIControlEvents { get; private set; } = null;
        public Note[][] Notes { get; private set; } = new Note[256][];
        public int[] FirstRenderNote { get; private set; } = new int[256];
        public int[] FirstUnhitNote { get; private set; } = new int[256];
        public double lastRenderTime { get; set; } = 0;
        public int[] LastColorEvent;
        public ColorEvent[][] ColorEvents;

        public MIDIMemoryFile(string path, MIDILoaderSettings settings, CancellationToken cancel)
            : base(path, settings, cancel)
        {
        }

        public override void Parse()
        {
            try
            {
                Open();
                FirstPassParse();
                foreach (var p in parsers)
                {
                    p.globaTempos = globalTempos;
                    p.PrepareForSecondPass();
                }
                SecondPassParse();
                MidiLength = parsers.Select(p => p.trackSeconds).Max();
                foreach (var p in parsers) p.Dispose();
                parsers = null;
                globalTempos = null;
                trackBeginnings = null;
                trackLengths = null;
                MidiFileReader.Dispose();
                MidiFileReader = null;
                GC.Collect();
                cancel.ThrowIfCancellationRequested();
                LastColorEvent = new int[trackcount * 16];
                SetColors();
                ParseFinishedInvoke();
            }
            catch (OperationCanceledException)
            {
                MidiFileReader.Close();
                MidiFileReader.Dispose();
                ParseCancelledInvoke();
            }
        }

        void SecondPassParse()
        {
            object l = new object();
            int tracksParsed = 0;
            ParseStage = ParsingStage.SecondPass;
            Parallel.For(0, parsers.Length, (i) =>
            {
                parsers[i].SecondPassParse();
                lock (l)
                {
                    tracksParsed++;
                    ParseNumber += 20;
                    ParseStatusText = "Loading MIDI\nTracks " + tracksParsed + " of " + parsers.Length;
                    Console.WriteLine("Pass 2 Parsed track " + tracksParsed + "/" + parsers.Length);
                }
            });
            cancel.ThrowIfCancellationRequested();
            int keysMerged = 0;
            var eventMerger = Task.Run(() =>
            {
                int count = loaderSettings.EventPlayerThreads;
                MIDINoteEvents = new MIDIEvent[count][];
                Parallel.For(0, count, new ParallelOptions() { CancellationToken = cancel }, i =>
                {
                    try
                    {
                        MIDINoteEvents[i] = TimedMerger<MIDIEvent>.MergeMany(parsers.Select(p => new SkipIterator<MIDIEvent>(p.NoteEvents, i, count)).ToArray(), e =>
                        {
                            return e.time;
                        }).ToArray();
                    }
                    catch (OperationCanceledException)
                    {
                    }
                });
            });
            var controlEventMerger = Task.Run(() =>
            {
                int count = loaderSettings.EventPlayerThreads;
                MIDIControlEvents = TimedMerger<MIDIEvent>.MergeMany(parsers.Select(p => p.ControlEvents).ToArray(), e => e.time).ToArray();
            });
            cancel.ThrowIfCancellationRequested();
            ParseStage = ParsingStage.MergingKeys;
            long noteCount = 0;
            Parallel.For(0, 256, new ParallelOptions() { CancellationToken = cancel }, (i) =>
            {
                var en = TimedMerger<Note>.MergeMany(parsers.Select(p => p.Notes[i]).ToArray(), n => n.start);
                if (loaderSettings.RemoveOverlaps) Notes[i] = RemoveOverlaps(en).ToArray();
                else Notes[i] = en.ToArray();
                foreach (var p in parsers) p.Notes[i] = null;
                lock (l)
                {
                    noteCount += Notes[i].Length;
                    keysMerged++;
                    ParseNumber += 10;
                    ParseStatusText = "Merging Notes\nMerged keys " + keysMerged + " of " + 256;
                    Console.WriteLine("Merged key " + keysMerged + "/" + 256);
                }
            });
            MidiNoteCount = noteCount;
            List<ColorEvent[]> ce = new List<ColorEvent[]>();
            foreach (var p in parsers) ce.AddRange(p.ColorEvents);
            ColorEvents = ce.ToArray();
            cancel.ThrowIfCancellationRequested();
            ParseStatusText = "Merging Events...";
            ParseStage = ParsingStage.MergingEvents;
            Console.WriteLine("Merging events...");
            controlEventMerger.GetAwaiter().GetResult();
            eventMerger.GetAwaiter().GetResult();
            ParseStatusText = "Done!";
        }

        IEnumerable<Note> RemoveOverlaps(IEnumerable<Note> input)
        {
            List<Note> tickNotes = new List<Note>();
            double currTick = -1;
            double epsilon = 0.00001;
            foreach (var n in input)
            {
                if (n.start > currTick)
                {
                    foreach (var _n in tickNotes) yield return _n;
                    tickNotes.Clear();
                    currTick = n.start + epsilon;
                    tickNotes.Add(n);
                }
                else
                {
                    var count = tickNotes.Count;
                    var end = n.end + epsilon;
                    if (count != 0 && tickNotes[count - 1].end <= end)
                    {
                        int i = count - 1;
                        for (; i >= 0; i--)
                        {
                            if (tickNotes[i].end > end) break;
                        }
                        i++;
                        if (i == 0)
                            tickNotes.Clear();
                        else if (i != count)
                            tickNotes.RemoveRange(i, count - i);
                        tickNotes.Add(n);
                    }
                    else
                    {
                        tickNotes.Add(n);
                    }
                }
            }
            foreach (var _n in tickNotes) yield return _n;
        }

        public void SetColorEvents(double time)
        {
            if (time < 0) time = 0;
            Parallel.For(0, MidiNoteColors.Length, i =>
            {
                MidiNoteColors[i] = OriginalMidiNoteColors[i];
                var ce = ColorEvents[i];
                var last = LastColorEvent[i];
                if (ce.Length == 0) return;
                if (ce.First().time > time)
                {
                    LastColorEvent[i] = 0;
                    return;
                }
                if (ce.Last().time <= time)
                {
                    MidiNoteColors[i] = ce.Last().color;
                    return;
                }
                if (ce[last].time < time)
                {
                    for (int j = last; j < ce.Length; j++)
                        if (ce[j + 1].time > time)
                        {
                            LastColorEvent[i] = j;
                            MidiNoteColors[i] = ce[j].color;
                            return;
                        }
                }
                else
                {
                    for (int j = last; j >= 0; j--)
                        if (ce[j].time <= time)
                        {
                            LastColorEvent[i] = j;
                            MidiNoteColors[i] = ce[j].color;
                            return;
                        }
                }
            });
        }

    }
}
