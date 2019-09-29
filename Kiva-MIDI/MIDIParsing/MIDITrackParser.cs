using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kiva_MIDI
{
    class MIDITrackParser : IDisposable
    {
        struct UnendedNote
        {
            public int id;
            public byte vel;
        }
        BufferByteReader reader;

        public FastList<TempoEvent> Tempos = new FastList<TempoEvent>();
        public TempoEvent[] globaTempos;

        public long noteCount;
        int midiEventCount = 0;
        public int[] noteCounts = new int[256];
        public int[] colorEventCounts = new int[16];
        public int[] currColorEventIndx = new int[16];
        long trackTimep1 = 0;
        long trackTimep2 = 0;
        public double trackSeconds = 0;
        int ppq;
        FastList<UnendedNote>[] UnendedNotes = null;
        public Note[][] Notes = new Note[256][];
        public ColorEvent[][] ColorEvents = new ColorEvent[16][];
        public MIDIEvent[] Events = null;
        int currEventIndex = 0;
        int[] currNoteIndexes = new int[256];

        public int FirstKey;
        public int LastKey;

        int track;

        int currGlobalTempoID = 0;
        double tempoMultiplier;

        MIDILoaderSettings settings;

        public MIDITrackParser(BufferByteReader reader, int ppq, int track, MIDILoaderSettings settings)
        {
            this.settings = settings;
            this.reader = reader;
            this.ppq = ppq;
            this.track = track;
            tempoMultiplier = ((double)500000 / ppq) / 1000000;
        }

        uint ReadVariableLenP1()
        {
            uint val = 0;
            byte c;
            for (int i = 0; i < 4; i++)
            {
                c = reader.ReadFast();
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
            byte c;
            uint val = 0;
            for (int i = 0; i < 4; i++)
            {
                c = reader.ReadFast();
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
                    v += (globaTempos[currGlobalTempoID].time - t) * tempoMultiplier;
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
            UnendedNotes = new FastList<UnendedNote>[256 * 16];
            for (int i = 0; i < 256 * 16; i++)
            {
                UnendedNotes[i] = new FastList<UnendedNote>();
            }
            byte eventThresh = settings.EventVelocityThreshold;
            byte noteThresh = settings.NoteVelocityThreshold;
            if (noteThresh > eventThresh) noteThresh = eventThresh;
            //try
            {
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
                            if (vel >= noteThresh)
                            {
                                noteCount++;
                                noteCounts[note]++;
                            }
                            if (vel >= eventThresh)
                                midiEventCount++;
                            UnendedNotes[note * 16 + channel].Add(new UnendedNote() { vel = vel });
                        }
                        else
                        {
                            var un = UnendedNotes[note * 16 + channel];
                            if (!un.ZeroLen)
                            {
                                if (un.Pop().vel >= eventThresh)
                                    midiEventCount++;
                            }
                        }
                        continue;
                    }
                    else if (comm == 0b10000000)
                    {
                        byte channel = (byte)(command & 0b00001111);
                        byte note = reader.Read();
                        byte vel = reader.ReadFast();
                        var un = UnendedNotes[note * 16 + channel];
                        if (!un.ZeroLen)
                        {
                            if (un.Pop().vel >= eventThresh)
                                midiEventCount++;
                        }
                        continue;
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
                                        if (channel < 16 || channel == 0x7F)
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
            }
            //catch { }
            UnendedNotes = null;
        }

        public void PrepareForSecondPass()
        {
            reader.Reset();
            for (int i = 0; i < 256; i++)
            {
                Notes[i] = new Note[noteCounts[i]];
            }
            for(int i = 0; i < 16; i++)
            {
                ColorEvents[i] = new ColorEvent[colorEventCounts[i]];
            }
            Events = new MIDIEvent[midiEventCount];
        }

        public void SecondPassParse()
        {
            byte eventThresh = settings.EventVelocityThreshold;
            byte noteThresh = settings.NoteVelocityThreshold;
            if (noteThresh > eventThresh) noteThresh = eventThresh;
            UnendedNotes = new FastList<UnendedNote>[256 * 16];
            for (int i = 0; i < 256 * 16; i++)
            {
                UnendedNotes[i] = new FastList<UnendedNote>();
            }
            //try
            {
                while (true)
                {
                    double delta = ReadVariableLenP2();
                    trackSeconds += delta;
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
                        if (vel == 0)
                        {
                            var un = UnendedNotes[note * 16 + channel];
                            if (!un.ZeroLen)
                            {
                                var n = un.Pop();
                                if (n.id != -1)
                                {
                                    Notes[note][n.id].end = trackSeconds;
                                    if (n.vel >= eventThresh)
                                    {
                                        Events[currEventIndex++] = new MIDIEvent()
                                        {
                                            time = (float)trackSeconds,
                                            data = (uint)(command | (note << 8) | (vel << 16))
                                        };
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (vel >= noteThresh)
                            {
                                UnendedNotes[note * 16 + channel].Add(new UnendedNote() { id = currNoteIndexes[note], vel = vel });
                                Notes[note][currNoteIndexes[note]++] = new Note()
                                {
                                    start = (float)trackSeconds,
                                    colorPointer = track * 16 + channel
                                };
                                if (vel >= eventThresh)
                                {
                                    Events[currEventIndex++] = new MIDIEvent()
                                    {
                                        time = (float)trackSeconds,
                                        data = (uint)(command | (note << 8) | (vel << 16))
                                    };
                                }
                            }
                            else
                            {
                                UnendedNotes[note * 16 + channel].Add(new UnendedNote() { id = -1 });
                            }
                        }
                        continue;
                    }
                    else if (comm == 0b10000000)
                    {
                        byte channel = (byte)(command & 0b00001111);
                        byte note = reader.Read();
                        byte vel = reader.ReadFast();
                        var un = UnendedNotes[note * 16 + channel];
                        if (!un.ZeroLen)
                        {
                            var n = un.Pop();
                            if (n.id != -1)
                            {
                                Notes[note][n.id].end = trackSeconds;
                                if (n.vel >= eventThresh)
                                {
                                    Events[currEventIndex++] = new MIDIEvent()
                                    {
                                        time = (float)trackSeconds,
                                        data = (uint)(command | (note << 8) | (vel << 16))
                                    };
                                }
                                else { }
                            }
                        }
                        continue;
                    }
                    else if (comm == 0b10100000)
                    {
                        byte channel = (byte)(command & 0b00001111);
                        byte note = reader.Read();
                        byte vel = reader.Read();
                        Events[currEventIndex++] = new MIDIEvent()
                        {
                            time = (float)trackSeconds,
                            data = (uint)(command | (note << 8) | (vel << 16))
                        };
                    }
                    else if (comm == 0b10110000)
                    {
                        byte channel = (byte)(command & 0b00001111);
                        byte controller = reader.Read();
                        byte value = reader.Read();
                        Events[currEventIndex++] = new MIDIEvent()
                        {
                            time = (float)trackSeconds,
                            data = (uint)(command | (controller << 8) | (value << 16))
                        };
                    }
                    else if (comm == 0b11000000)
                    {
                        byte program = reader.Read();
                        Events[currEventIndex++] = new MIDIEvent()
                        {
                            time = (float)trackSeconds,
                            data = (uint)(command | (program << 8))
                        };
                    }
                    else if (comm == 0b11010000)
                    {
                        byte pressure = reader.Read();
                        Events[currEventIndex++] = new MIDIEvent()
                        {
                            time = (float)trackSeconds,
                            data = (uint)(command | (pressure << 8))
                        };
                    }
                    else if (comm == 0b11100000)
                    {
                        byte var1 = reader.Read();
                        byte var2 = reader.Read();
                        Events[currEventIndex++] = new MIDIEvent()
                        {
                            time = (float)trackSeconds,
                            data = (uint)(command | (var1 << 8) | (var2 << 16))
                        };
                    }
                    else if (comm == 0b10110000)
                    {
                        byte cc = reader.Read();
                        byte vv = reader.Read();
                        Events[currEventIndex++] = new MIDIEvent()
                        {
                            time = (float)trackSeconds,
                            data = (uint)(command | (cc << 8) | (vv << 16))
                        };
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
                                        if (channel < 16 || channel == 0x7F)
                                        {
                                            if (reader.Read() == 0x00)
                                            {
                                                var col = new NoteCol();
                                                /*
                                                col.r = reader.Read() / 255.0f;
                                                col.g = reader.Read() / 255.0f;
                                                col.b = reader.Read() / 255.0f;
                                                col.a = reader.Read() / 255.0f;
                                                */
                                                col.rgba = (uint)((reader.Read() << 24) & 0xff000000) |
                                                           (uint)((reader.Read() << 16) & 0xff0000) |
                                                           (uint)((reader.Read() << 8) & 0xff00) |
                                                           (uint)(reader.Read() & 0xff);
                                                if (size == 8)
                                                {
                                                    /*
                                                    col.r2 = col.r;
                                                    col.g2 = col.g;
                                                    col.b2 = col.b;
                                                    col.a2 = col.a;
                                                    */
                                                    col.rgba2 = col.rgba;
                                                }
                                                else
                                                {
                                                    /*
                                                    col.r2 = reader.Read() / 255.0f;
                                                    col.g2 = reader.Read() / 255.0f;
                                                    col.b2 = reader.Read() / 255.0f;
                                                    col.a2 = reader.Read() / 255.0f;
                                                    */
                                                    col.rgba2 = (uint)((reader.Read() << 24) & 0xff000000) |
                                                                (uint)((reader.Read() << 16) & 0xff0000) |
                                                                (uint)((reader.Read() << 8) & 0xff00) |
                                                                (uint)(reader.Read() & 0xff);
                                                }
                                                if (channel == 0x7F)
                                                {
                                                    for (int i = 0; i < 16; i++)
                                                        ColorEvents[i][currColorEventIndx[i]++] = new ColorEvent()
                                                        {
                                                            time = trackSeconds,
                                                            color = col
                                                        };
                                                }
                                                else
                                                {
                                                    ColorEvents[channel][currColorEventIndx[channel]++] = new ColorEvent() {
                                                        time = trackSeconds,
                                                        color = col
                                                    };
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
            }
            //catch { }
            for (int note = 0; note < 256; note++)
                for (int channel = 0; channel < 16; channel++)
                {
                    while (!UnendedNotes[note * 16 + channel].ZeroLen)
                    {
                        var n = UnendedNotes[note * 16 + channel].Pop();
                        if (n.id != -1)
                            Notes[note][n.id].end = trackSeconds;
                    }
                }
            UnendedNotes = null;
        }

        public void Dispose()
        {
            Tempos = null;
            globaTempos = null;
            noteCounts = null;
            colorEventCounts = null;
            UnendedNotes = null;
            Notes = null;
            currNoteIndexes = null;
        }
    }
}
