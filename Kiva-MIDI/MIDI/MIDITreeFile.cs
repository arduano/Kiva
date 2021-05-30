using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kiva.MIDI
{
    //[StructLayout(LayoutKind.Sequential)]
    //public struct IntVector4
    //{
    //    public IntVector4(int val1, int val2, int val3, int val4)
    //    {
    //        Val1 = val1;
    //        Val2 = val2;
    //        Val3 = val3;
    //        Val4 = val4;
    //    }

    //    public int Val1 { get; }
    //    public int Val2 { get; }
    //    public int Val3 { get; }
    //    public int Val4 { get; }
    //}

    //public class MIDITreeFile : MIDIMemoryFile
    //{
    //    public MIDITreeFile(string path, MIDILoaderSettings settings, CancellationToken cancel) : base(path, settings, cancel)
    //    {
    //    }

    //    IntVector4[] BuildTree()
    //    {
    //        List<IntVector4> ParseForKey(int key)
    //        {
    //            List<IntVector4> output = new List<IntVector4>();

    //            int AddValue(IntVector4 val)
    //            {
    //                output.Add(val);
    //                return output.Count - 1;
    //            }

    //            HashSet<Note> uniqueNotes = new HashSet<Note>();

    //            var notes = TimedMerger<Note>.MergeMany(parsers.Select(p => p.Notes[key]).ToArray(), n => n.start); ;

    //            var iter = notes.GetEnumerator();
    //            bool iterEnded = !iter.MoveNext();

    //            int lastPos = 0;

    //            int noteCount = 0;

    //            Stack<Note> stack = new Stack<Note>();

    //            Note? TopNote() => stack.Count == 0 ? new Note?() : stack.Peek();
    //            int nextEvent = 0;

    //            void StepTo(int pos)
    //            {
    //                while (stack.Count > 0 && stack.Peek().end < pos) stack.Pop();

    //                while (!iterEnded && iter.Current.start <= pos)
    //                {
    //                    if (iter.Current.end > pos) stack.Push(iter.Current);
    //                    iterEnded = !iter.MoveNext();
    //                    noteCount++;
    //                }

    //                nextEvent = int.MaxValue;
    //                var topNote = TopNote();
    //                if (topNote != null) nextEvent = Math.Min(topNote.Value.end, nextEvent);
    //                if (!iterEnded) nextEvent = Math.Min(iter.Current.start, nextEvent);
    //            }

    //            int RecursiveBuild(int start, int end)
    //            {
    //                if (end - start == 1)
    //                {
    //                    StepTo(start);
    //                    var note = TopNote();
    //                    if (note != null)
    //                        uniqueNotes.Add(note.Value);
    //                    if(note == null)
    //                    {
    //                        return AddValue(new IntVector4(0, 0, -1, 0));
    //                    }
    //                    else
    //                    {
    //                        var n = note.Value;
    //                        return AddValue(new IntVector4(n.start, n.end, n.colorPointer, 0));
    //                    }
    //                }

    //                int half = (start + end) / 2;

    //                var first = RecursiveBuild(start, half);
    //                if (first < 0)
    //                {
    //                    if (nextEvent >= end) return first;
    //                }

    //                if (nextEvent > half && nextEvent < end) half = nextEvent;

    //                var second = RecursiveBuild(half, end);

    //                return AddValue(new IntVector4(half, first, second, 0));
    //            }

    //            var root = RecursiveBuild(0, MidiLength);

    //            uniqueNoteCount[key] = uniqueNotes.Count;

    //            return root;
    //        }

    //        var nodes = new NodeNoteUnion[Constants.KeyCount];

    //        ParallelFor(0, nodes.Length, Environment.ProcessorCount, CancellationToken.None, i =>
    //        {
    //            Console.WriteLine("Started " + i);
    //            nodes[i] = ParseForKey(i);
    //            Console.WriteLine("Finished " + i);
    //        });

    //        return new NoteBinaryTree(nodes, uniqueNoteCount);

    //    }

    //    protected override void SecondPassParse()
    //    {
    //        object l = new object();
    //        ParseStage = ParsingStage.SecondPass;
    //        RunSecondPassParse();
    //        cancel.ThrowIfCancellationRequested();
    //        int keysMerged = 0;
    //        var eventMerger = Task.Run(() =>
    //        {
    //            MergeAudioEvents();
    //        });
    //        var controlEventMerger = Task.Run(() =>
    //        {
    //            MergeControlEvents();
    //        });
    //        cancel.ThrowIfCancellationRequested();
    //        ParseStage = ParsingStage.BuildingTree;
    //        long noteCount = 0;
    //        Parallel.For(0, 256, new ParallelOptions() { CancellationToken = cancel }, (i) =>
    //        {
    //            var en = TimedMerger<Note>.MergeMany(parsers.Select(p => p.Notes[i]).ToArray(), n => n.start);
    //            if (LoaderSettings.RemoveOverlaps) Notes[i] = RemoveOverlaps(en).ToArray();
    //            else Notes[i] = en.ToArray();
    //            foreach (var p in parsers) p.Notes[i] = null;
    //            lock (l)
    //            {
    //                noteCount += Notes[i].Length;
    //                keysMerged++;
    //                ParseNumber += 10;
    //                ParseStatusText = "Merging Notes\nMerged keys " + keysMerged + " of " + 256;
    //                Console.WriteLine("Merged key " + keysMerged + "/" + 256);
    //            }
    //        });
    //        MidiNoteCount = noteCount;
    //        MergeColorEvents();
    //        cancel.ThrowIfCancellationRequested();
    //        ParseStatusText = "Merging Events...";
    //        ParseStage = ParsingStage.MergingEvents;
    //        controlEventMerger.GetAwaiter().GetResult();
    //        eventMerger.GetAwaiter().GetResult();
    //        ParseStatusText = "Done!";
    //    }

    //    IEnumerable<Note> RemoveOverlaps(IEnumerable<Note> input)
    //    {
    //        List<Note> tickNotes = new List<Note>();
    //        double currTick = -1;
    //        double epsilon = 0.00001;
    //        foreach (var n in input)
    //        {
    //            if (n.start > currTick)
    //            {
    //                foreach (var _n in tickNotes) yield return _n;
    //                tickNotes.Clear();
    //                currTick = n.start + epsilon;
    //                tickNotes.Add(n);
    //            }
    //            else
    //            {
    //                var count = tickNotes.Count;
    //                var end = n.end + epsilon;
    //                if (count != 0 && tickNotes[count - 1].end <= end)
    //                {
    //                    int i = count - 1;
    //                    for (; i >= 0; i--)
    //                    {
    //                        if (tickNotes[i].end > end) break;
    //                    }
    //                    i++;
    //                    if (i == 0)
    //                        tickNotes.Clear();
    //                    else if (i != count)
    //                        tickNotes.RemoveRange(i, count - i);
    //                    tickNotes.Add(n);
    //                }
    //                else
    //                {
    //                    tickNotes.Add(n);
    //                }
    //            }
    //        }
    //        foreach (var _n in tickNotes) yield return _n;
    //    }
    //}
}
