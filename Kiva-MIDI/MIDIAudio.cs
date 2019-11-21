using CSCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Un4seen.Bass.AddOn.Midi;
using CSCore.SoundOut;
using System.IO;

namespace Kiva_MIDI
{
    public class MIDIAudio : IDisposable
    {
        class AudioBufferStream : ISampleSource
        {
            public bool CanSeek => false;

            public WaveFormat WaveFormat => format;

            public long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public long Length => throw new NotImplementedException();

            MIDIAudio audioSource;

            public AudioBufferStream(MIDIAudio source)
            {
                audioSource = source;
            }

            public void Dispose()
            {

            }

            public int Read(float[] buffer, int offset, int count)
            {
                lock (audioSource.AudioBuffer)
                {
                    if (audioSource.Paused)
                    {
                        for (int i = 0; i < count; i++)
                        {
                            buffer[i + offset] = 0;
                        }
                        return count;
                    }
                    if (count % 2 != 0) throw new Exception("Expected a multiple of 2");
                    var readpos = audioSource.bufferReadPos % (audioSource.AudioBuffer.Length / 2);
                    var writepos = audioSource.bufferReadPos % (audioSource.AudioBuffer.Length / 2);
                    if (audioSource.bufferReadPos + count / 2 > audioSource.bufferWritePos)
                    {
                        int copyCount = audioSource.bufferReadPos - (audioSource.bufferWritePos + count / 2);
                        if (copyCount > count / 2) copyCount = count / 2;
                        if (copyCount > 0) WrappedCopy(audioSource.AudioBuffer, readpos * 2, buffer, offset, copyCount * 2);
                        else
                        {
                            copyCount = 0;
                        }
                        for (int i = copyCount * 2; i < count; i++)
                        {
                            buffer[i + offset] = 0;
                        }
                    }
                    else
                    {
                        WrappedCopy(audioSource.AudioBuffer, readpos * 2, buffer, offset, count);
                    }
                    audioSource.bufferReadPos += count / 2;
                    audioSource.lastReadtime = DateTime.UtcNow;
                    return count;
                }
            }
        }

        static void WrappedCopy(float[] src, int pos, float[] dst, int pos2, int count)
        {
            if (pos + count > src.Length)
            {
                Buffer.BlockCopy(src, pos * 4, dst, pos2 * 4, (src.Length - pos) * 4);
                count -= src.Length - pos;
                pos = 0;
            }
            Buffer.BlockCopy(src, pos * 4, dst, pos2 * 4, count * 4);
        }

        float[] AudioBuffer;
        int bufferReadPos = 0;
        int bufferWritePos = 0;
        double startTime = 0;

        DateTime lastReadtime = DateTime.UtcNow;

        public int SkippingVelocity
        {
            get
            {
                if (Paused) return 0;
                var diff = 127 + 10 - (bufferWritePos - bufferReadPos) / 100;
                if (diff > 127) diff = 127;
                if (diff < 0) diff = 0;
                return diff;
            }
        }

        public double BufferSeconds => Math.Max(0, bufferWritePos - bufferReadPos) / 48000.0;
        public double PlayerTime => startTime + bufferReadPos / 48000.0;

        static WaveFormat format = new WaveFormat(48000, 32, 2);

        public bool Paused { get; set; } = true;

        Task generatorThread = null;
        CancellationTokenSource cancelGenerator = null;

        AudioBufferStream audioStream;
        ISoundOut soundOut;

        public static void Init()
        {
            BASSMIDI.InitBASS(format);
            BASSMIDI.LoadGlobalSoundfonts();
        }

        private ISoundOut GetSoundOut()
        {
            if (WasapiOut.IsSupportedOnCurrentPlatform)
                return new WasapiOut();
            else
                return new DirectSoundOut();
        }

        public MIDIAudio(int bufferLength)
        {
            AudioBuffer = new float[bufferLength * 2];
            audioStream = new AudioBufferStream(this);
            soundOut = GetSoundOut();
            soundOut.Initialize(new LoudMaxStream(audioStream).ToWaveSource());
            soundOut.Play();
            lastReadtime = DateTime.UtcNow;
        }

        void BassWriteWrapped(BASSMIDI bass, int start, int count)
        {
            start = (start * 2) % AudioBuffer.Length;
            count *= 2;
            if (start + count > AudioBuffer.Length)
            {
                bass.Read(AudioBuffer, start, AudioBuffer.Length - start);
                count -= AudioBuffer.Length - start;
                bass.Read(AudioBuffer, 0, count);
            }
            else
            {
                bass.Read(AudioBuffer, start, count);
            }
        }

        void GeneratorFunc(IEnumerable<MIDIEvent> events, double speed, Action<double, int> skipEvents)
        {
            var bass = new BASSMIDI(1000);
            bufferWritePos = 0;
            bufferReadPos = 0;
            lastReadtime = DateTime.UtcNow;
            foreach (var e in events)
            {
                var shiftedBufferReadPos = bufferReadPos;// + (int)((DateTime.UtcNow - lastReadtime).TotalSeconds * 48000);
                if (bufferWritePos < bufferReadPos)
                {
                    bufferWritePos = bufferReadPos;
                }

                double offset = (e.time - startTime) / speed;
                int samples = (int)(offset * 48000) - bufferWritePos;

                if (samples > 0)
                {
                    while (bufferWritePos + samples > bufferReadPos + AudioBuffer.Length / 2)
                    {
                        var spare = (bufferReadPos + AudioBuffer.Length / 2) - bufferWritePos;
                        if (spare > 0)
                        {
                            if (spare > samples) spare = samples;
                            if (spare != 0)
                            {
                                BassWriteWrapped(bass, bufferWritePos, spare);
                                samples -= spare;
                                bufferWritePos += spare;
                            }
                            if (samples == 0) break;
                        }
                        Thread.Sleep(2);
                        if (cancelGenerator.Token.IsCancellationRequested) break;
                    }
                    if (samples != 0) BassWriteWrapped(bass, bufferWritePos, samples);
                    bufferWritePos += samples;
                }

                var ev = e.data;

                byte cmd = (byte)(ev & 0xF0);

                if (cmd < 0xA0) //Note
                {
                    if (samples >= 0 && !(bufferWritePos - shiftedBufferReadPos < (127 - e.vel + 10) * 20 && bufferReadPos > 1000))
                        bass.SendEvent(BASSMIDIEvent.MIDI_EVENT_NOTE, cmd < 0x90 ? (byte)(ev >> 8) : (ushort)(ev >> 8), (int)ev & 0xF, 0, 0);
                    else
                        skipEvents(startTime + shiftedBufferReadPos / 48000.0, 127 - (bufferWritePos - shiftedBufferReadPos) / 100);
                }
                else if (cmd == 0xE0) //PitchBend
                {
                    var b1 = (ev >> 8) & 0x7f;
                    var b2 = (ev >> 16) & 0x7f;
                    bass.SendEvent(BASSMIDIEvent.MIDI_EVENT_PITCH, (int)(b1 | (b2 << 7)), (int)ev & 0xF, 0, 0);
                }
                else if (cmd == 0xC0) //InstrumentSelect
                {
                    bass.SendEvent(BASSMIDIEvent.MIDI_EVENT_PROGRAM, (byte)(ev >> 8), (int)ev & 0xF, 0, 0);
                }
                else if (cmd == 0xA0) //AfterTouch
                {
                    bass.SendEvent(BASSMIDIEvent.MIDI_EVENT_KEYPRES, (ushort)(ev >> 8), (int)ev & 0xF, 0, 0);
                }
                else if (cmd == 0xD0) //Channel Pressure
                {
                    bass.SendEvent(BASSMIDIEvent.MIDI_EVENT_CHANPRES, (byte)(ev >> 8), (int)ev & 0xF, 0, 0);
                }
                else if (cmd == 0xB0) //Control
                {
                    var b1 = (ev >> 8) & 0x7f;
                    var b2 = (ev >> 16) & 0x7f;
                    bass.SendEventRaw(BASSMIDIEvent.MIDI_EVENT_CONTROL, ev & 0xFFFFF0, (int)ev & 0xF);
                }
                if (cancelGenerator.Token.IsCancellationRequested) break;
            }
            while (!cancelGenerator.Token.IsCancellationRequested)
            {
                var spare = (bufferReadPos + AudioBuffer.Length / 2) - bufferWritePos;
                if (spare > 0)
                {
                    if (spare != 0)
                    {
                        BassWriteWrapped(bass, bufferWritePos, spare);
                        bufferWritePos += spare;
                    }
                }
                Thread.Sleep(2);
            }
            bass.Dispose();
        }

        void KillLastGenerator()
        {
            if (cancelGenerator != null) cancelGenerator.Cancel();
            if (generatorThread != null) generatorThread.GetAwaiter().GetResult();
        }

        public void Start(double time, IEnumerable<MIDIEvent> events, double speed, Action<double, int> skipEvents)
        {
            KillLastGenerator();
            cancelGenerator = new CancellationTokenSource();
            startTime = time;
            generatorThread = Task.Run(() => GeneratorFunc(events, speed, skipEvents));
        }

        public void Stop()
        {
            KillLastGenerator();
            cancelGenerator = null;
            generatorThread = null;
            bufferWritePos = 0;
            bufferReadPos = 0;
        }

        public void SyncPlayer(double time)
        {
            lock (AudioBuffer)
            {
                var t = startTime + bufferReadPos / 48000.0;
                var offset = time - t;
                var newPos = bufferReadPos + (int)(offset * 48000);
                if (newPos < 0) newPos = 0;
                bufferReadPos = newPos;
            }
        }

        public void Dispose()
        {
            KillLastGenerator();
            soundOut.Stop();
            soundOut.Dispose();
            audioStream = null;
        }
    }
}
