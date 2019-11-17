using CSCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kiva_MIDI
{
    public class LoudMaxStream : ISampleSource
    {
        public LoudMaxStream(ISampleSource provider)
        {
            Provider = provider;
            WaveFormat = provider.WaveFormat;
        }
        public LoudMaxStream(ISampleSource provider, double frequencyReduce) : this(provider)
        {
            reduceHighPitch = true;
            velocityThresh = 1 / frequencyReduce;
        }

        public WaveFormat WaveFormat { get; }
        public ISampleSource Provider { get; }
        public long Position { get => Provider.Position; set => Provider.Position = value; }

        public long Length => Provider.Length;

        public bool CanSeek => Provider.CanSeek;

        public double Strength { get => strength; set => strength = value; }

        bool reduceHighPitch = false;

        double loudnessL = 1;
        double loudnessR = 1;
        double velocityR = 0;
        double velocityL = 0;
        double attack = 100;
        double falloff = 48000 / 3;
        double strength = 1;
        double minThresh = 0.4;
        double velocityThresh = 1;
        public int Read(float[] buffer, int offset, int count)
        {
            int read = Provider.Read(buffer, offset, count);
            int end = offset + read;
            if (read % 2 != 0) throw new Exception("Must be a multiple of 2");
            for (int i = offset; i < end; i += 2)
            {
                double l = Math.Abs(buffer[i]);
                double r = Math.Abs(buffer[i + 1]);

                if (loudnessL > l)
                    loudnessL = (loudnessL * falloff + l) / (falloff + 1);
                else
                    loudnessL = (loudnessL * attack + l) / (attack + 1);

                if (loudnessR > r)
                    loudnessR = (loudnessR * falloff + r) / (falloff + 1);
                else
                    loudnessR = (loudnessR * attack + r) / (attack + 1);

                if (loudnessL < minThresh) loudnessL = minThresh;
                if (loudnessR < minThresh) loudnessR = minThresh;

                l = buffer[i] / (loudnessL * strength + 2 * (1 - strength)) / 2;
                r = buffer[i + 1] / (loudnessR * strength + 2 * (1 - strength)) / 2;

                if (i != offset)
                {
                    double dl = Math.Abs(buffer[i] - l);
                    double dr = Math.Abs(buffer[i + 1] - r);

                    if (velocityL > dl)
                        velocityL = (velocityL * falloff + dl) / (falloff + 1);
                    else
                        velocityL = (velocityL * attack + dl) / (attack + 1);

                    if (velocityR > dr)
                        velocityR = (velocityR * falloff + dr) / (falloff + 1);
                    else
                        velocityR = (velocityR * attack + dr) / (attack + 1);
                }

                if (reduceHighPitch)
                {
                    if (velocityL > velocityThresh)
                        l = l / velocityL * velocityThresh;
                    if (velocityR > velocityThresh)
                        r = r / velocityR * velocityThresh;
                }

                buffer[i] = (float)l;
                buffer[i + 1] = (float)r;
            }
            return read;
        }

        public void Dispose()
        {
            Provider.Dispose();
        }
    }
}
