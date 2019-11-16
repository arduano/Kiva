﻿using CSCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading;
using Un4seen.Bass.AddOn.Midi;
using CSCore.SoundOut;

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
                throw new NotImplementedException();
            }

            public int Read(float[] buffer, int offset, int count)
            {
                if (count % 2 != 0) throw new Exception("Expected a multiple of 2");
                var readpos = audioSource.bufferReadPos % (audioSource.AudioBuffer.Length / 2);
                var writepos = audioSource.bufferReadPos % (audioSource.AudioBuffer.Length / 2);
                if (audioSource.bufferReadPos + count / 2 > audioSource.bufferWritePos)
                {
                    if (audioSource.bufferReadPos > audioSource.bufferWritePos)
                    {
                        audioSource.bufferWritePos = audioSource.bufferReadPos + count / 2;
                    }
                    int copyCount = audioSource.bufferReadPos - audioSource.bufferWritePos;
                    if (copyCount > 0) WrappedCopy(audioSource.AudioBuffer, readpos * 2, buffer, offset, copyCount * 2);
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
                return count;
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

        static WaveFormat format = new WaveFormat(48000, 32, 2);
        BASSMIDI midi;

        Task generatorThread = null;
        CancellationTokenSource cancelGenerator = null;

        AudioBufferStream audioStream;
        ISoundOut soundOut;

        public static void Init()
        {
            BASSMIDI.InitBASS(format);
            BASSMIDI.LoadDefaultSoundfont();
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
            midi = new BASSMIDI(1000);
            AudioBuffer = new float[bufferLength * 2];
            audioStream = new AudioBufferStream(this);
            soundOut = GetSoundOut();
            soundOut.Initialize(audioStream.ToWaveSource());
            soundOut.Play();
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

        void GeneratorFunc(double time, IEnumerable<MIDIEvent> events)
        {
            BASSMIDI bass = new BASSMIDI(1000);
            foreach (var e in events)
            {
                double offset = e.time - time;
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
                    }
                    if (samples != 0) BassWriteWrapped(bass, bufferWritePos, samples);
                    bufferWritePos += samples;
                }

                var ev = e.data;

                byte cmd = (byte)ev;

                if (cmd < 0xA0)
                {
                    bass.SendEvent(BASSMIDIEvent.MIDI_EVENT_NOTE, cmd < 0x90 ? (byte)(ev >> 8) : (ushort)(ev >> 8), (int)ev & 0xF, 0, 0);
                }
            }
        }

        void KillLastGenerator()
        {
            if (cancelGenerator != null) cancelGenerator.Cancel();
            if (generatorThread != null) generatorThread.GetAwaiter().GetResult();
        }

        public void Start(double time, IEnumerable<MIDIEvent> events)
        {
            KillLastGenerator();
            cancelGenerator = new CancellationTokenSource();
            Task.Run(() => GeneratorFunc(time, events));
        }

        public void Dispose()
        {
            midi.Dispose();
        }
    }
}