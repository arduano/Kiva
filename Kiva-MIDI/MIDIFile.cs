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
        public MIDIEvent[][] MIDIEvents { get; private set; } = null;
        public Note[][] Notes { get; private set; } = new Note[256][];
        public int[] FirstRenderNote { get; private set; } = new int[256];
        public double lastRenderTime { get; set; } = 0;
        public NoteCol[] MidiNoteColors { get; private set; } = null;
        public double MidiLength { get; private set; } = 0;


        public ushort division { get; private set; }
        public int trackcount { get; private set; }
        public ushort format { get; private set; }

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
            MidiLength = parsers.Select(p => p.trackSeconds).Max();
            foreach (var p in parsers) p.Dispose();
            parsers = null;
            globalTempos = null;
            trackBeginnings = null;
            trackLengths = null;
            MidiFileReader.Dispose();
            MidiFileReader = null;
            GC.Collect();
            SetColors();
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
            var eventMerger = Task.Run(() =>
            {
                int count = Environment.ProcessorCount;
                //count = 1;
                MIDIEvents = new MIDIEvent[count][];
                Parallel.For(0, count, i => {
                    MIDIEvents[i] = TimedMerger<MIDIEvent>.MergeMany(parsers.Select(p => new SkipIterator<MIDIEvent>(p.Events, i, count)).ToArray(), e => e.time).ToArray();
                });
            });
            Parallel.For(0, 256, (i) =>
            {
                Notes[i] = TimedMerger<Note>.MergeMany(parsers.Select(p => p.Notes[i]).ToArray(), n => n.start).ToArray();
                lock (l)
                {
                    keysMerged++;
                    Console.WriteLine("Merged key " + keysMerged + "/" + 256);
                }
            });
            Console.WriteLine("Merging events...");
            eventMerger.GetAwaiter().GetResult();
        }

        void SetColors()
        {
            MidiNoteColors = new NoteCol[trackcount * 16];
            for (int i = 0; i < MidiNoteColors.Length; i++)
            {
                int r, g, b;
                HsvToRgb((i * 40) % 360, 1, 1, out r, out g, out b);
                MidiNoteColors[i] = new NoteCol()
                {
                    r = r / 255.0f,
                    g = g / 255.0f,
                    b = b / 255.0f,
                    a = 1,
                    r2 = r / 255.0f,
                    g2 = g / 255.0f,
                    b2 = b / 255.0f,
                    a2 = 1
                };
            }
        }

        void HsvToRgb(double h, double S, double V, out int r, out int g, out int b)
        {
            double H = h;
            while (H < 0) { H += 360; };
            while (H >= 360) { H -= 360; };
            double R, G, B;
            if (V <= 0)
            { R = G = B = 0; }
            else if (S <= 0)
            {
                R = G = B = V;
            }
            else
            {
                double hf = H / 60.0;
                int i = (int)Math.Floor(hf);
                double f = hf - i;
                double pv = V * (1 - S);
                double qv = V * (1 - S * f);
                double tv = V * (1 - S * (1 - f));
                switch (i)
                {

                    // Red is the dominant color

                    case 0:
                        R = V;
                        G = tv;
                        B = pv;
                        break;

                    // Green is the dominant color

                    case 1:
                        R = qv;
                        G = V;
                        B = pv;
                        break;
                    case 2:
                        R = pv;
                        G = V;
                        B = tv;
                        break;

                    // Blue is the dominant color

                    case 3:
                        R = pv;
                        G = qv;
                        B = V;
                        break;
                    case 4:
                        R = tv;
                        G = pv;
                        B = V;
                        break;

                    // Red is the dominant color

                    case 5:
                        R = V;
                        G = pv;
                        B = qv;
                        break;

                    // Just in case we overshoot on our math by a little, we put these here. Since its a switch it won't slow us down at all to put these here.

                    case 6:
                        R = V;
                        G = tv;
                        B = pv;
                        break;
                    case -1:
                        R = V;
                        G = pv;
                        B = qv;
                        break;

                    // The color is not defined, we should throw an error.

                    default:
                        //LFATAL("i Value error in Pixel conversion, Value is %d", i);
                        R = G = B = V; // Just pretend its black/white
                        break;
                }
            }
            r = Clamp((int)(R * 255.0));
            g = Clamp((int)(G * 255.0));
            b = Clamp((int)(B * 255.0));
        }

        int Clamp(int i)
        {
            if (i < 0) return 0;
            if (i > 255) return 255;
            return i;
        }
    }
}
