using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Kiva_MIDI
{
    [StructLayout(LayoutKind.Sequential)]
    struct NoteCol
    {
        public float r, g, b, a;
        public float r2, g2, b2, a2;
    }

    interface ITimed
    {
        double Time { get; }
    }

    [StructLayout(LayoutKind.Sequential)]
    struct Note
    {
        public double start, end;
        public int colorPointer;
        public int skip;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct MIDIEvent
    {
        public double time;
        public uint data;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct ColorEvent
    {
        public double time;
        public NoteCol color;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct TempoEvent
    {
        public long time;
        public int tempo;
    }

    enum ParsingStage
    {
        Opening,
        FirstPass,
        SecondPass,
        Merging
    }

    class MIDIFile
    {
        List<long> trackBeginnings = new List<long>();
        List<uint> trackLengths = new List<uint>();

        MIDITrackParser[] parsers;
        TempoEvent[] globalTempos;

        //Persistent values
        public MIDIEvent[] MIDIEvents = null;
        public Note[][] Notes = new Note[256][];
        public int[] NoteReadProgresses = new int[256];

        public ushort division;
        public int trackcount;
        public ushort format;

        Stream MidiFileReader;
        string filepath;

        public MIDIFile(string path)
        {
            filepath = path;
        }

        void AssertText(string text)
        {
            foreach (char c in text)
            {
                if (MidiFileReader.ReadByte() != c)
                {
                    throw new Exception("Corrupt chunk headers");
                }
            }
        }

        uint ReadInt32()
        {
            uint length = 0;
            for (int i = 0; i != 4; i++)
                length = (uint)((length << 8) | (byte)MidiFileReader.ReadByte());
            return length;
        }

        ushort ReadInt16()
        {
            ushort length = 0;
            for (int i = 0; i != 2; i++)
                length = (ushort)((length << 8) | (byte)MidiFileReader.ReadByte());
            return length;
        }

        void ParseHeaderChunk()
        {
            AssertText("MThd");
            uint length = ReadInt32();
            if (length != 6) throw new Exception("Header chunk size isn't 6");
            format = ReadInt16();
            ReadInt16();
            division = ReadInt16();
            if (format == 2) throw new Exception("Midi type 2 not supported");
            if (division < 0) throw new Exception("Division < 0 not supported");
        }

        void ParseTrackChunk()
        {
            AssertText("MTrk");
            uint length = ReadInt32();
            trackBeginnings.Add(MidiFileReader.Position);
            trackLengths.Add(length);
            MidiFileReader.Position += length;
            trackcount++;
            Console.WriteLine("Track " + trackcount + ", Size " + length);
        }

        public void Parse()
        {
            Open();
            FirstPassParse();
            foreach (var p in parsers)
            {
                p.globaTempos = globalTempos;
                p.PrepareForSecondPass();
            }
            SecondPassParse();
            foreach (var p in parsers) p.Dispose();
            parsers = null;
            globalTempos = null;
            trackBeginnings = null;
            trackLengths = null;
            MidiFileReader.Dispose();
            MidiFileReader = null;
            GC.Collect();
        }

        void Open()
        {
            MidiFileReader = File.Open(filepath, FileMode.Open);
            ParseHeaderChunk();
            while (MidiFileReader.Position < MidiFileReader.Length)
            {
                ParseTrackChunk();
            }
            parsers = new MIDITrackParser[trackcount];
        }

        void FirstPassParse()
        {
            object l = new object();
            int tracksParsed = 0;
            Parallel.For(0, parsers.Length, (i) =>
            {
                var reader = new BufferByteReader(MidiFileReader, 10000, trackBeginnings[i], trackLengths[i]);
                parsers[i] = new MIDITrackParser(reader, division, i);
                parsers[i].FirstPassParse();
                lock (l)
                {
                    tracksParsed++;
                    Console.WriteLine("Pass 1 Parsed track " + tracksParsed + "/" + parsers.Length);
                }
            });
            var temposMerge = TimedMerger<TempoEvent>.MergeMany(parsers.Select(p => p.Tempos).ToArray(), t => t.time);
            globalTempos = temposMerge.Cast<TempoEvent>().ToArray();
        }

        void SecondPassParse()
        {
            object l = new object();
            int tracksParsed = 0;
            Parallel.For(0, parsers.Length, (i) =>
            {
                parsers[i].SecondPassParse();
                lock (l)
                {
                    tracksParsed++;
                    Console.WriteLine("Pass 2 Parsed track " + tracksParsed + "/" + parsers.Length);
                }
            });
            int keysMerged = 0;
            Parallel.For(0, 256, (i) =>
            {
                Notes[i] = TimedMerger<Note>.MergeMany(parsers.Select(p => p.Notes[i]).ToArray(), n => n.start).ToArray();
                lock (l)
                {
                    keysMerged++;
                    Console.WriteLine("Merged key " + keysMerged + "/" + 256);
                }
            });
        }
    }
}
