using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kiva_MIDI
{
    class MIDITrackParser : IDisposable
    {
        BufferByteReader reader;

        public FastList<TempoEvent> Tempos = new FastList<TempoEvent>();
        public TempoEvent[] globaTempos;

        public long noteCount;
        int midiEventCount = 0;
        public int[] noteCounts = new int[256];
        public int[] colorEventCounts = new int[16];
        long trackTimep1 = 0;
        long trackTimep2 = 0;
        public double trackSeconds = 0;
        int ppq;
        FastList<int>[] UnendedNotes = null;
        public Note[][] Notes = new Note[256][];
        int[] currNoteIndexes = new int[256];

        int track;

        int currGlobalTempoID = 0;
        double tempoMultiplier;

        public MIDITrackParser(BufferByteReader reader, int ppq, int track)
        {
            this.reader = reader;
            this.ppq = ppq;
            this.track = track;
            tempoMultiplier = ((double)500000 / ppq) / 1000000;
        }

        uint ReadVariableLenP1()
        {
            byte[] b = new byte[5];
            byte c;
            uint val = 0;
            for (int i = 0; i < 4; i++)
            {
                c = reader.Read();
                if (c > 0x7F)
                {
                    val = ((val << 7) | (byte)(c & 0x7F));
                }
                else
                {
                    val = val << 7 | c;
                    break;
                }
            }
            return val;
        }

        double ReadVariableLenP2()
        {
            byte[] b = new byte[5];
            byte c;
            uint val = 0;
            for (int i = 0; i < 4; i++)
            {
                c = reader.Read();
                if (c > 0x7F)
                {
                    val = ((val << 7) | (byte)(c & 0x7F));
                }
                else
                {
                    val = val << 7 | c;
                    break;
                }
            }
            trackTimep2 += val;

            if (currGlobalTempoID < globaTempos.Length && trackTimep2 > globaTempos[currGlobalTempoID].time)
            {
                long t = trackTimep2 - val;
                double v = 0;
                while (currGlobalTempoID < globaTempos.Length && trackTimep2 > globaTempos[currGlobalTempoID].time)
                {
                    v += (t - globaTempos[currGlobalTempoID].time) * tempoMultiplier;
                    t = globaTempos[currGlobalTempoID].time;
                    tempoMultiplier = ((double)globaTempos[currGlobalTempoID].tempo / ppq) / 1000000;
                    currGlobalTempoID++;
                }
                v += (trackTimep2 - t) * tempoMultiplier;
                return v;
            }
            else
            {
                return val * tempoMultiplier;
            }
        }

        byte prevCommand = 0;
        public void FirstPassParse()
        {
            //try
            //{
            while (true)
            {
                uint delta = ReadVariableLenP1();
                trackTimep1 += delta;
                byte command = reader.ReadFast();
                if (command < 0x80)
                {
                    reader.Pushback = command;
                    command = prevCommand;
                }
                prevCommand = command;
                byte comm = (byte)(command & 0b11110000);
                if (comm == 0b10010000)
                {
                    byte channel = (byte)(command & 0b00001111);
                    byte note = reader.Read();
                    byte vel = reader.ReadFast();
                    if (vel != 0)
                    {
                        noteCount++;
                        noteCounts[note]++;
                    }
                    midiEventCount++;
                }
                else if (comm == 0b10000000)
                {
                    byte channel = (byte)(command & 0b00001111);
                    byte note = reader.Read();
                    byte vel = reader.ReadFast();
                    midiEventCount++;
                }
                else if (comm == 0b10100000)
                {
                    byte channel = (byte)(command & 0b00001111);
                    byte note = reader.Read();
                    byte vel = reader.Read();
                    midiEventCount++;
                }
                else if (comm == 0b10110000)
                {
                    byte channel = (byte)(command & 0b00001111);
                    byte controller = reader.Read();
                    byte value = reader.Read();
                    midiEventCount++;
                }
                else if (comm == 0b11000000)
                {
                    byte program = reader.Read();
                    midiEventCount++;
                }
                else if (comm == 0b11010000)
                {
                    byte pressure = reader.Read();
                    midiEventCount++;
                }
                else if (comm == 0b11100000)
                {
                    byte var1 = reader.Read();
                    byte var2 = reader.Read();
                    midiEventCount++;
                }
                else if (comm == 0b10110000)
                {
                    byte cc = reader.Read();
                    byte vv = reader.Read();
                    midiEventCount++;
                }
                else if (command == 0b11110000)
                {
                    List<byte> data = new List<byte>() { command };
                    byte b = 0;
                    while (b != 0b11110111)
                    {
                        b = reader.Read();
                        data.Add(b);
                    }
                }
                else if (command == 0b11110100 || command == 0b11110001 || command == 0b11110101 || command == 0b11111001 || command == 0b11111101)
                {
                }
                else if (command == 0b11110010)
                {
                    byte var1 = reader.Read();
                    byte var2 = reader.Read();
                }
                else if (command == 0b11110011)
                {
                    byte pos = reader.Read();
                }
                else if (command == 0b11110110)
                {
                }
                else if (command == 0b11110111)
                {
                }
                else if (command == 0b11111000)
                {
                }
                else if (command == 0b11111010)
                {
                }
                else if (command == 0b11111100)
                {
                }
                else if (command == 0b11111110)
                {
                }
                else if (command == 0xFF)
                {
                    command = reader.Read();
                    if (command == 0x00)
                    {
                        if (reader.Read() != 2)
                        {
                            throw new Exception("Corrupt Track");
                        }
                    }
                    else if ((command >= 0x01 && command <= 0x0A) || command == 0x7F)
                    {
                        int size = (int)ReadVariableLenP1();
                        if (command == 0x0A &&
                            (size == 8 || size == 12))
                        {
                            if (reader.Read() == 0x00)
                                if (reader.Read() == 0x0F)
                                {
                                    byte channel = reader.Read();
                                    if (channel < 16 || channel == 7F)
                                    {
                                        if (reader.Read() == 0x00)
                                        {
                                            if (size == 8) reader.Skip(4);
                                            else if (size == 12) reader.Skip(8);
                                            if (channel == 0x7F)
                                            {
                                                for (int i = 0; i < 16; i++)
                                                    colorEventCounts[i]++;
                                            }
                                            else
                                            {
                                                colorEventCounts[channel]++;
                                            }
                                        }
                                        else reader.Skip(size - 4);
                                    }
                                    else reader.Skip(size - 3);
                                }
                                else reader.Skip(size - 2);
                            else reader.Skip(size - 1);
                        }
                        else
                        {
                            reader.Skip(size);
                        }
                    }
                    else if (command == 0x20)
                    {
                        command = reader.Read();
                        if (command != 1)
                        {
                            throw new Exception("Corrupt Track");
                        }
                        reader.Read();
                    }
                    else if (command == 0x21)
                    {
                        command = reader.Read();
                        if (command != 1)
                        {
                            throw new Exception("Corrupt Track");
                        }
                        reader.Read();
                    }
                    else if (command == 0x2F)
                    {
                        command = reader.Read();
                        if (command != 0)
                        {
                            throw new Exception("Corrupt Track");
                        }
                        break;
                    }
                    else if (command == 0x51)
                    {
                        command = reader.Read();
                        if (command != 3)
                        {
                            throw new Exception("Corrupt Track");
                        }
                        int btempo = 0;
                        for (int i = 0; i != 3; i++)
                            btempo = (int)((btempo << 8) | reader.Read());
                        Tempos.Add(new TempoEvent() { time = trackTimep1, tempo = btempo });
                    }
                    else if (command == 0x54)
                    {
                        command = reader.Read();
                        if (command != 5)
                        {
                            throw new Exception("Corrupt Track");
                        }
                        byte hr = reader.Read();
                        byte mn = reader.Read();
                        byte se = reader.Read();
                        byte fr = reader.Read();
                        byte ff = reader.Read();
                    }
                    else if (command == 0x58)
                    {
                        command = reader.Read();
                        if (command != 4)
                        {
                            throw new Exception("Corrupt Track");
                        }
                        byte nn = reader.Read();
                        byte dd = reader.Read();
                        byte cc = reader.Read();
                        byte bb = reader.Read();
                    }
                    else if (command == 0x59)
                    {
                        command = reader.Read();
                        if (command != 2)
                        {
                            throw new Exception("Corrupt Track");
                        }
                        byte sf = reader.Read();
                        byte mi = reader.Read();
                    }
                    else
                    {
                        throw new Exception("Corrupt Track");
                    }
                }
                else
                {
                    throw new Exception("Corrupt Track");
                }
            }
            //}
            //catch { }
        }

        public void PrepareForSecondPass()
        {
            reader.Reset();
            for (int i = 0; i < 256; i++)
            {
                Notes[i] = new Note[noteCounts[i]];
            }
        }

        public void SecondPassParse()
        {
            UnendedNotes = new FastList<int>[256 * 16];
            for (int i = 0; i < 256 * 16; i++)
            {
                UnendedNotes[i] = new FastList<int>();
            }
            //try
            //{
            while (true)
            {
                double delta = ReadVariableLenP2();
                trackSeconds += delta;
                byte command = reader.Read();
                if (command < 0x80)
                {
                    reader.Pushback = command;
                    command = prevCommand;
                }
                prevCommand = command;
                byte comm = (byte)(command & 0b11110000);
                if (comm == 0b10010000)
                {
                    byte channel = (byte)(command & 0b00001111);
                    byte note = reader.Read();
                    byte vel = reader.ReadFast();
                    if (vel == 0)
                    {
                        var un = UnendedNotes[note * 16 + channel];
                        if (!un.ZeroLen)
                            Notes[note][un.Pop()].end = trackSeconds;
                    }
                    else
                    {
                        UnendedNotes[note * 16 + channel].Add(currNoteIndexes[note]);
                        Notes[note][currNoteIndexes[note]++] = new Note()
                        {
                            start = trackSeconds,
                            colorPointer = track * 16 + channel
                        };
                    }
                }
                else if (comm == 0b10000000)
                {
                    byte channel = (byte)(command & 0b00001111);
                    byte note = reader.Read();
                    byte vel = reader.ReadFast();
                        var un = UnendedNotes[note * 16 + channel];
                        if (!un.ZeroLen)
                            Notes[note][un.Pop()].end = trackSeconds;
                }
                else if (comm == 0b10100000)
                {
                    byte channel = (byte)(command & 0b00001111);
                    byte note = reader.Read();
                    byte vel = reader.Read();
                }
                else if (comm == 0b10110000)
                {
                    byte channel = (byte)(command & 0b00001111);
                    byte controller = reader.Read();
                    byte value = reader.Read();
                }
                else if (comm == 0b11000000)
                {
                    byte program = reader.Read();
                }
                else if (comm == 0b11010000)
                {
                    byte pressure = reader.Read();
                }
                else if (comm == 0b11100000)
                {
                    byte var1 = reader.Read();
                    byte var2 = reader.Read();
                }
                else if (comm == 0b10110000)
                {
                    byte cc = reader.Read();
                    byte vv = reader.Read();
                }
                else if (command == 0b11110000)
                {
                    List<byte> data = new List<byte>() { command };
                    byte b = 0;
                    while (b != 0b11110111)
                    {
                        b = reader.Read();
                        data.Add(b);
                    }
                }
                else if (command == 0b11110100 || command == 0b11110001 || command == 0b11110101 || command == 0b11111001 || command == 0b11111101)
                {
                }
                else if (command == 0b11110010)
                {
                    byte var1 = reader.Read();
                    byte var2 = reader.Read();
                }
                else if (command == 0b11110011)
                {
                    byte pos = reader.Read();
                }
                else if (command == 0b11110110)
                {
                }
                else if (command == 0b11110111)
                {
                }
                else if (command == 0b11111000)
                {
                }
                else if (command == 0b11111010)
                {
                }
                else if (command == 0b11111100)
                {
                }
                else if (command == 0b11111110)
                {
                }
                else if (command == 0xFF)
                {
                    command = reader.Read();
                    if (command == 0x00)
                    {
                        if (reader.Read() != 2)
                        {
                            throw new Exception("Corrupt Track");
                        }
                    }
                    else if ((command >= 0x01 && command <= 0x0A) || command == 0x7F)
                    {
                        int size = (int)ReadVariableLenP1();
                        if (command == 0x0A &&
                            (size == 8 || size == 12))
                        {
                            if (reader.Read() == 0x00)
                                if (reader.Read() == 0x0F)
                                {
                                    byte channel = reader.Read();
                                    if (channel < 16 || channel == 7F)
                                    {
                                        if (reader.Read() == 0x00)
                                        {
                                            var col = new NoteCol();
                                            col.r = reader.Read() / 255.0f;
                                            col.g = reader.Read() / 255.0f;
                                            col.b = reader.Read() / 255.0f;
                                            col.a = reader.Read() / 255.0f;
                                            if (size == 8)
                                            {
                                                col.r2 = col.r;
                                                col.g2 = col.g;
                                                col.b2 = col.b;
                                                col.a2 = col.a;
                                            }
                                            else
                                            {
                                                col.r2 = reader.Read() / 255.0f;
                                                col.g2 = reader.Read() / 255.0f;
                                                col.b2 = reader.Read() / 255.0f;
                                                col.a2 = reader.Read() / 255.0f;
                                            }
                                            if (channel == 0x7F)
                                            {
                                                for (int i = 0; i < 16; i++)
                                                    colorEventCounts[i]++;
                                            }
                                            else
                                            {
                                                colorEventCounts[channel]++;
                                            }
                                        }
                                        else reader.Skip(size - 4);
                                    }
                                    else reader.Skip(size - 3);
                                }
                                else reader.Skip(size - 2);
                            else reader.Skip(size - 1);
                        }
                        else
                        {
                            reader.Skip(size);
                        }
                    }
                    else if (command == 0x20)
                    {
                        command = reader.Read();
                        if (command != 1)
                        {
                            throw new Exception("Corrupt Track");
                        }
                        reader.Read();
                    }
                    else if (command == 0x21)
                    {
                        command = reader.Read();
                        if (command != 1)
                        {
                            throw new Exception("Corrupt Track");
                        }
                        reader.Read();
                    }
                    else if (command == 0x2F)
                    {
                        command = reader.Read();
                        if (command != 0)
                        {
                            throw new Exception("Corrupt Track");
                        }
                        break;
                    }
                    else if (command == 0x51)
                    {
                        command = reader.Read();
                        if (command != 3)
                        {
                            throw new Exception("Corrupt Track");
                        }
                        int btempo = 0;
                        for (int i = 0; i != 3; i++)
                            btempo = (int)((btempo << 8) | reader.Read());
                    }
                    else if (command == 0x54)
                    {
                        command = reader.Read();
                        if (command != 5)
                        {
                            throw new Exception("Corrupt Track");
                        }
                        byte hr = reader.Read();
                        byte mn = reader.Read();
                        byte se = reader.Read();
                        byte fr = reader.Read();
                        byte ff = reader.Read();
                    }
                    else if (command == 0x58)
                    {
                        command = reader.Read();
                        if (command != 4)
                        {
                            throw new Exception("Corrupt Track");
                        }
                        byte nn = reader.Read();
                        byte dd = reader.Read();
                        byte cc = reader.Read();
                        byte bb = reader.Read();
                    }
                    else if (command == 0x59)
                    {
                        command = reader.Read();
                        if (command != 2)
                        {
                            throw new Exception("Corrupt Track");
                        }
                        byte sf = reader.Read();
                        byte mi = reader.Read();
                    }
                    else
                    {
                        throw new Exception("Corrupt Track");
                    }
                }
                else
                {
                    throw new Exception("Corrupt Track");
                }
            }
            //}
            //catch { }
            for (int i = 0; i < 256; i++)
            {
                while (!UnendedNotes[i].ZeroLen)
                {
                    Notes[i][UnendedNotes[i].Pop()].end = trackSeconds;
                }
            }
            UnendedNotes = null;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
