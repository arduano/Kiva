using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kiva
{
    public class Constants
    {
        public const int TicksPerSecond = 16384;
        //public const bool Is256Key = false;
        //public const int KeyCount = Is256Key ? 256 : 128;

        public static int TimeToInt(double time)
        {
            return (int)Math.Round(time * TicksPerSecond);
        }

        public static double IntTimeToSeconds(int time)
        {
            return time / (double)TicksPerSecond;
        }
    }
}
