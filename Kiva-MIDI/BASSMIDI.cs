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

namespace Kiva_MIDI
{
    public class BASSMIDI : ISampleSource
    {
        public int Handle { get; private set; }

        public bool CanSeek => false;

        public WaveFormat WaveFormat { get; }
        public static WaveFormat WaveFormatStatic { get; private set; }

        public long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public long Length => throw new NotImplementedException();

        static BASS_MIDI_FONTEX[] fontarr;

        static object sfLock = new object();

        public static void InitBASS(WaveFormat format)
        {
            WaveFormatStatic = format;
            Bass.BASS_Free();
            if (!Bass.BASS_Init(0, WaveFormatStatic.SampleRate, BASSInit.BASS_DEVICE_NOSPEAKER, IntPtr.Zero))
                throw new Exception();
        }

        public static void DisposeBASS()
        {
            Bass.BASS_Free();
        }

        public BASSMIDI(int voices, bool nofx = true)
        {
            Handle = BassMidi.BASS_MIDI_StreamCreate(16,
                BASSFlag.BASS_SAMPLE_FLOAT |
                BASSFlag.BASS_STREAM_DECODE |
                BASSFlag.BASS_MIDI_SINCINTER |
                BASSFlag.BASS_MIDI_NOTEOFF1,
                WaveFormatStatic.SampleRate);

            if (Handle == 0)
            {
                var error = Bass.BASS_ErrorGetCode();
                throw new Exception(error.ToString());
            }

            Bass.BASS_ChannelSetAttribute(Handle, BASSAttribute.BASS_ATTRIB_MIDI_VOICES, voices);
            Bass.BASS_ChannelSetAttribute(Handle, BASSAttribute.BASS_ATTRIB_SRC, 3);
            Bass.BASS_ChannelSetAttribute(Handle, BASSAttribute.BASS_ATTRIB_MIDI_CHANS, 16);

            if (nofx) Bass.BASS_ChannelFlags(Handle, BASSFlag.BASS_MIDI_NOFX, BASSFlag.BASS_MIDI_NOFX);

            lock (sfLock)
            {
                BassMidi.BASS_MIDI_StreamSetFonts(Handle, fontarr, fontarr.Length);
            }
        }

        public static void FreeSoundfonts()
        {
            if (fontarr != null)
            {
                foreach (var f in fontarr) BassMidi.BASS_MIDI_FontFree(f.font);
                fontarr = null;
            }
        }

        public static void LoadSoundfonts(SoundfontData[] soundfonts)
        {
            lock (sfLock)
            {
                FreeSoundfonts();
                List<BASS_MIDI_FONTEX> fonts = new List<BASS_MIDI_FONTEX>();
                foreach (var s in soundfonts)
                {
                    if (!s.enabled) continue;
                    var font = BassMidi.BASS_MIDI_FontInit(s.path,
                        s.xgdrums ? BASSFlag.BASS_MIDI_FONT_XGDRUMS : BASSFlag.BASS_DEFAULT);

                    if (font != 0)
                    {
                        fonts.Add(new BASS_MIDI_FONTEX(font, s.srcp, s.srcb, s.desp, s.desb, s.xgdrums ? 1 : 0));

                        BassMidi.BASS_MIDI_FontLoad(font, s.srcp, s.srcb);
                    }
                }
                fontarr = fonts.ToArray();
                Array.Reverse(fontarr);
            }
        }

        public bool WriteBass(int buflen, Stream bs, ref ulong progress)
        {
            buflen <<= 3;
            byte[] buf = new byte[buflen];

            int ret = Bass.BASS_ChannelGetData(Handle, buf, buflen | (int)BASSData.BASS_DATA_FLOAT);
            if (ret > 0)
            {
                progress += (uint)ret;
                bs.Write(buf, 0, ret);
                return true;
            }
            else
            {
                var err = Bass.BASS_ErrorGetCode();
                if (err != BASSError.BASS_ERROR_ENDED)
                    throw new Exception("ret " + ret + " " + Bass.BASS_ErrorGetCode());
                return false;
            }
        }

        public float[] WriteFloatArray(int buflen, ref ulong progress)
        {
            byte[] buf = new byte[buflen * 4];
            float[] flt = new float[buflen];

            int ret = Bass.BASS_ChannelGetData(Handle, buf, buflen * 4);
            if (ret > 0)
            {
                progress += (uint)ret;
                Buffer.BlockCopy(buf, 0, flt, 0, buflen * 4);
                return flt;
            }
            else
            {
                var err = Bass.BASS_ErrorGetCode();
                if (err != BASSError.BASS_ERROR_ENDED)
                    throw new Exception("ret " + ret + " " + Bass.BASS_ErrorGetCode());
                return null;
            }
        }

        public int KShortMessage(int dwParam1, int sampleoffset)
        {
            if ((byte)dwParam1 == 0xFF)
                return 1;

            byte cmd = (byte)dwParam1;

            BASS_MIDI_EVENT ev;

            if (cmd < 0xA0) //Note
            {
                ev = new BASS_MIDI_EVENT(BASSMIDIEvent.MIDI_EVENT_NOTE,
                    cmd < 0x90 ? (byte)(dwParam1 >> 8) : (ushort)(dwParam1 >> 8), (int)dwParam1 & 0xF, 0, sampleoffset << 3);
            }
            else if (cmd < 0xB0) //AfterTouch
            {
                ev = new BASS_MIDI_EVENT(BASSMIDIEvent.MIDI_EVENT_KEYPRES,
                    (ushort)(dwParam1 >> 8), (int)dwParam1 & 0xF, 0, sampleoffset << 3);
            }
            else if (cmd < 0xC0) //Control
            {
                //TODO
                return 0;
            }
            else if (cmd < 0xD0) //InstrumentSelect
            {
                ev = new BASS_MIDI_EVENT(BASSMIDIEvent.MIDI_EVENT_PROGRAM,
                    (byte)(dwParam1 >> 8), (int)dwParam1 & 0xF, 0, sampleoffset << 3);
            }
            else if (cmd < 0xE0) //???
            {
                ev = new BASS_MIDI_EVENT(BASSMIDIEvent.MIDI_EVENT_CHANPRES,
                    (byte)(dwParam1 >> 8), (int)dwParam1 & 0xF, 0, sampleoffset << 3);
            }
            else if (cmd == 0xF0) //PitchBend
            {
                //TODO: check bit pack
                ev = new BASS_MIDI_EVENT(BASSMIDIEvent.MIDI_EVENT_PITCH,
                    (int)((byte)(dwParam1 >> 16) | ((dwParam1 & 0x7F00) >> 1)), (int)dwParam1 & 0xF, 0, sampleoffset << 3);
            }
            else
            {
                return 0;
            }

            BassStreamEvents(new BASS_MIDI_EVENT[] { ev });

            return 0;
        }

        public int SendEvent(BASSMIDIEvent type, int param, int chan, int tick, int time)
        {
            var ev = new BASS_MIDI_EVENT(type, param, chan, tick, time << 3);
            var mode = BASSMIDIEventMode.BASS_MIDI_EVENTS_TIME | BASSMIDIEventMode.BASS_MIDI_EVENTS_STRUCT;
            return BassMidi.BASS_MIDI_StreamEvents(Handle, mode, new BASS_MIDI_EVENT[] { ev });
        }

        public unsafe int SendEventRaw(uint data, int channel)
        {
            var mode = BASSMIDIEventMode.BASS_MIDI_EVENTS_RAW | BASSMIDIEventMode.BASS_MIDI_EVENTS_NORSTATUS;
            return BassMidi.BASS_MIDI_StreamEvents(Handle, mode, channel, (IntPtr)(&data), 3);
        }

        public int BassStreamEvents(BASS_MIDI_EVENT[] events)
        {
            var mode = BASSMIDIEventMode.BASS_MIDI_EVENTS_TIME | BASSMIDIEventMode.BASS_MIDI_EVENTS_STRUCT;
            return BassMidi.BASS_MIDI_StreamEvents(Handle, mode, events);
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
                return ret / 4;
            }
        }

        public void Dispose()
        {
            Bass.BASS_StreamFree(Handle);
        }
    }
}
