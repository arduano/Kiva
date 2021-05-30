using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kiva.MIDI
{
    class MIDINoteFile : MIDIMemoryFile
    {
        public Note[][] Notes { get; private set; } = new Note[256][];

        public MIDINoteFile(string path, MIDILoaderSettings settings, CancellationToken cancel) : base(path, settings, cancel)
        {
        }

        protected override void SecondPassParse()
        {
            object l = new object();
            ParseStage = ParsingStage.SecondPass;
            RunSecondPassParse();
            cancel.ThrowIfCancellationRequested();
            int keysMerged = 0;
            var eventMerger = Task.Run(() =>
            {
                MergeAudioEvents();
            });
            var controlEventMerger = Task.Run(() =>
            {
                MergeControlEvents();
            });
            cancel.ThrowIfCancellationRequested();
            ParseStage = ParsingStage.MergingKeys;
            long noteCount = 0;
            Parallel.For(0, 256, new ParallelOptions() { CancellationToken = cancel }, (i) =>
            {
                var en = TimedMerger<Note>.MergeMany(parsers.Select(p => p.Notes[i]).ToArray(), n => n.start);
                if (LoaderSettings.RemoveOverlaps) Notes[i] = RemoveOverlaps(en).ToArray();
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
            MergeColorEvents();
            cancel.ThrowIfCancellationRequested();
            ParseStatusText = "Merging Events...";
            ParseStage = ParsingStage.MergingEvents;
            controlEventMerger.GetAwaiter().GetResult();
            eventMerger.GetAwaiter().GetResult();
            ParseStatusText = "Done!";
        }
    }
}
