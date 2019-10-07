using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kiva_MIDI
{
    class VelocityEase
    {
        public double Duration { get; set; } = 1;
        public double Slope { get; set; } = 2;
        public double Supress { get; set; } = 1;

        public double Start { get; private set; }
        public double End { get; private set; }

        double v;
        DateTime start;

        double pow(double a, double b) => Math.Pow(a, b);

        double getInertiaPos(double t) => -2 * (-0.5 * t + t * t / 2) * v;
        double getInertiaVel(double t) => (0.5 - t) * v * 2;

        double getEasePos(double t) => pow(t, Slope) / (pow(1 - t, Slope) + pow(t, Slope));
        double getEaseVel(double t) =>
            (pow(-(-1 + t) * t, Slope - 1) * Slope) /
            pow(pow(1 - t, Slope) + pow(t, Slope), 2);

        public double GetValue()
        {
            double t = (DateTime.UtcNow - start).TotalSeconds / Duration;
            if (t > 1)
            {
                v = 0;
                return End;
            }

            double pos = getEasePos(t) * (End - Start) + getInertiaPos(t);
            pos += Start;
            return pos;
        }

        public double GetValue(double min, double max)
        {
            var val = GetValue();
            if (val < min) val = min;
            if (val > max) val = max;
            return val;
        }

        public void SetEnd(double e)
        {
            double t = (DateTime.UtcNow - start).TotalSeconds / Duration;
            double vel;
            if (t > 1)
            {
                vel = 0;
                t = 1;
            }
            else
                vel = getEaseVel(t) * (End - Start) + getInertiaVel(t);
            vel /= Duration * Supress;
            double pos = getEasePos(t) * (End - Start) + Start + getInertiaPos(t);

            Start = pos;
            End = e;
            v = vel;
            start = DateTime.UtcNow;
        }

        public VelocityEase(double initial)
        {
            Start = initial;
            End = initial;
            start = DateTime.UtcNow;
        }
    }
}
