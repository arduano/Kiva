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
                    if (audioSource.Paused || audioSource.awaitingReset)
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

        public int defaultVoices = 1000;
        public bool defaultNoFx = false;
        public double simulatedLagScale = 0.01;

        bool awaitingReset = false;

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
            var bass = new BASSMIDI(defaultVoices, defaultNoFx);
            bufferWritePos = 0;
            bufferReadPos = 0;
            lastReadtime = DateTime.UtcNow;
            Random r = new Random();
            double prevTime = -1;
            foreach (var e in events)
            {
                var shiftedBufferReadPos = bufferReadPos;// + (int)((DateTime.UtcNow - lastReadtime).TotalSeconds * 48000);
                if (bufferWritePos < bufferReadPos)
                {
                    bufferWritePos = bufferReadPos;
                }
                double evTime = e.time / speed;
                if (simulatedLagScale != 0)
                {
                    var timeDist = (evTime - prevTime);
                    if (evTime < prevTime) evTime = prevTime;
                    if (timeDist < simulatedLagScale)
                    {
                        evTime += r.NextDouble() / 100 * (simulatedLagScale + timeDist);
                        if (evTime - e.time >= simulatedLagScale)
                        { }
                    }
                    prevTime = evTime;
                }
                double offset = (evTime - startTime);
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

                int err = 1;

                err = bass.SendEventRaw(ev & 0xFFFFFF, 0);
                if (err <= 0)
                { }
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
            startTime = time / speed;
            generatorThread = Task.Run(() => GeneratorFunc(events, speed, skipEvents));
            awaitingReset = false;
        }

        public void Stop()
        {
            KillLastGenerator();
            cancelGenerator = null;
            generatorThread = null;
            bufferWritePos = 0;
            bufferReadPos = 0;
        }

        public void SyncPlayer(double time, double speed)
        {
            lock (AudioBuffer)
            {
                time /= speed;
                var t = startTime + bufferReadPos / 48000.0;
                var offset = time - t;
                var newPos = bufferReadPos + (int)(offset * 48000);
                if (newPos < 0) newPos = 0;
                if (Math.Abs(bufferReadPos - newPos) / 48000.0 > 0.03)
                    bufferReadPos = newPos;
            }
        }

        public void ResizeBuffer(int size)
        {
            awaitingReset = true;
            AudioBuffer = new float[size * 2];
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
