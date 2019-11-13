using CSCore;
using CSCore.SoundOut;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Midi;

namespace MIDIAudioFramework
{
    public static class BASSMIDI
    {
        class PlayerSource : ISampleSource
        {
            public bool CanSeek => false;

            public WaveFormat WaveFormat => WaveFormatStatic;

            public long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public long Length => throw new NotImplementedException();

            public void Dispose()
            {

            }

            public unsafe int Read(float[] buffer, int offset, int count)
            {
                fixed (float* buff = buffer)
                {
                    var obuff = buff + offset;
                    int ret = Bass.BASS_ChannelGetData(Handle, (IntPtr)obuff, (count * 4) | (int)BASSData.BASS_DATA_FLOAT);
                    if (ret == 0)
                    {
                        var err = Bass.BASS_ErrorGetCode();
                        if (err != BASSError.BASS_ERROR_ENDED)
                            throw new Exception("ret " + ret + " " + Bass.BASS_ErrorGetCode());
                    }
                    lastReadTime = DateTime.UtcNow;
                    return ret / 4;
                }
            }
        }

        static DateTime lastReadTime = DateTime.UtcNow;

        static int Handle { get; set; }

        public static WaveFormat WaveFormatStatic { get; private set; } = new WaveFormat(48000, 32, 2);
        public static int Voices { get; set; } = 50000;

        static ISoundOut soundOut;
        static ISampleSource player;

        static BASS_MIDI_FONTEX[] fontarr;

        static ISoundOut GetSoundOut()
        {
            if (WasapiOut.IsSupportedOnCurrentPlatform)
                return new WasapiOut();
            else
                return new DirectSoundOut();
        }

        public static void InitBASS()
        {
            Bass.BASS_Free();
            if (!Bass.BASS_Init(0, WaveFormatStatic.SampleRate, BASSInit.BASS_DEVICE_NOSPEAKER, IntPtr.Zero))
                throw new Exception();

            LoadDefaultSoundfont();

            Handle = BassMidi.BASS_MIDI_StreamCreate(16,
                BASSFlag.BASS_SAMPLE_FLOAT |
                BASSFlag.BASS_STREAM_DECODE |
                BASSFlag.BASS_MIDI_SINCINTER,
                WaveFormatStatic.SampleRate);

            if (Handle == 0)
            {
                var error = Bass.BASS_ErrorGetCode();
                throw new Exception(error.ToString());
            }

            Bass.BASS_ChannelSetAttribute(Handle, BASSAttribute.BASS_ATTRIB_MIDI_VOICES, Voices);
            Bass.BASS_ChannelSetAttribute(Handle, BASSAttribute.BASS_ATTRIB_SRC, 3);

            Bass.BASS_ChannelFlags(Handle, BASSFlag.BASS_MIDI_NOFX, BASSFlag.BASS_MIDI_NOFX);

            BassMidi.BASS_MIDI_StreamSetFonts(Handle, fontarr, fontarr.Length);

            soundOut = GetSoundOut();
            player = new PlayerSource();
            soundOut.Initialize(player.ToWaveSource());
            lastReadTime = DateTime.UtcNow;
            soundOut.Play();
        }

        public static void DisposeBASS()
        {
            soundOut.Stop();
            soundOut.Dispose();
            Bass.BASS_StreamFree(Handle);
            Bass.BASS_Free();
        }

        public static void LoadDefaultSoundfont()
        {
            String omconfig = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!String.IsNullOrEmpty(omconfig))
                omconfig = Path.Combine(omconfig, "OmniMIDI", "lists", "OmniMIDI_A.omlist");
            List<BASS_MIDI_FONTEX> fonts = new List<BASS_MIDI_FONTEX>();
            if (File.Exists(omconfig))
            {
                String[] lines = File.ReadAllLines(omconfig, Encoding.UTF8);


                BASS_MIDI_FONTEX currfont = new BASS_MIDI_FONTEX();
                String currfilename = null;
                bool xgdrums = false;
                bool add = true;

                int lineno = 0;

                foreach (String line in lines)
                {
                    lineno++;

                    if (String.IsNullOrWhiteSpace(line) || line.StartsWith("//"))
                        continue;

                    if (line == "sf.start")
                    {
                        currfont = new BASS_MIDI_FONTEX(0, -1, -1, -1, 0, 0);
                        currfilename = null;
                        xgdrums = false;
                        add = true;
                        continue;
                    }

                    if (line == "sf.end")
                    {
                        if (add)
                        {
                            if (currfilename == null)
                            {
                                throw new Exception("Missing filename at line " + lineno);
                            }

                            currfont.font = BassMidi.BASS_MIDI_FontInit(currfilename,
                                xgdrums ? BASSFlag.BASS_MIDI_FONT_XGDRUMS : BASSFlag.BASS_DEFAULT);

                            if (currfont.font != 0)
                            {
                                fonts.Add(currfont);

                                BassMidi.BASS_MIDI_FontLoad(currfont.font, currfont.spreset, currfont.sbank);
                            }
                        }
                        currfilename = null;
                        continue;
                    }

                    if (!line.StartsWith("sf."))
                    {
                        throw new Exception("Invalid line " + lineno);
                    }

                    int idx = line.IndexOf(" = ");
                    if (idx < 4)
                    {
                        throw new Exception("Invalid instruction at line " + lineno);
                    }

                    String instr = line.Substring(3, idx - 3);
                    String idata = line.Substring(idx + 3);

                    switch (instr)
                    {
                        case "path": currfilename = idata; break;
                        case "enabled": add = idata != "0"; break;
                        case "srcb": currfont.sbank = int.Parse(idata); break;
                        case "srcp": currfont.spreset = int.Parse(idata); break;
                        case "desb": currfont.dbank = int.Parse(idata); break;
                        case "desp": currfont.dpreset = int.Parse(idata); break;
                        case "xgdrums": xgdrums = idata != "0"; break;

                        default:
                            throw new Exception("Invalid instruction at line " + lineno);
                    }
                }

                fontarr = fonts.ToArray();
                Array.Reverse(fontarr);
            }
            else
            {
                throw new Exception("OmniMIDI config file missing");
            }
        }

        public static int SendEvent(BASSMIDIEvent type, int param, int chan, int tick, int time)

        {
            var ev = new BASS_MIDI_EVENT(type, param, chan, tick, time << 3);
            var mode = BASSMIDIEventMode.BASS_MIDI_EVENTS_TIME | BASSMIDIEventMode.BASS_MIDI_EVENTS_STRUCT;
            return BassMidi.BASS_MIDI_StreamEvents(Handle, mode, new BASS_MIDI_EVENT[] { ev });
        }

        public static int BassStreamEvents(BASS_MIDI_EVENT[] events)
        {
            var mode = BASSMIDIEventMode.BASS_MIDI_EVENTS_TIME | BASSMIDIEventMode.BASS_MIDI_EVENTS_STRUCT;
            return BassMidi.BASS_MIDI_StreamEvents(Handle, mode, events);
        }

        public static void SendEvent(uint e)
        {
            SendEvent(BASSMIDIEvent.MIDI_EVENT_NOTE, (127 << 8) | 64, 0, 0, (int)(DateTime.UtcNow.Ticks - lastReadTime.Ticks) << 3);
        }
    }
}
