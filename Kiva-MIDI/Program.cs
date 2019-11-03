using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kiva_MIDI
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            var window = new MainWindow();
            try
            {
                if (args.Length != 0)
                {
                    window.LoadMidi(args[0]);
                }
                window.ShowDialog();
            }
            catch (Exception e)
            {
                string msg = e.Message + "\n" + e.Data + "\n";
                //msg += e.StackTrace.Length > 1000 ? e.StackTrace.Substring(0, 1000) + "......." : e.StackTrace;
                msg += e.StackTrace;
                MessageBox.Show("Kiva has crashed!", msg);
            }
        }
    }
}
