using KivaShared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KivaInstaller
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            var window = new MainWindow();

            try
            {
                window.ShowDialog();
            }
            catch (Exception e)
            {
                string msg = e.Message + "\n" + e.Data + "\n";
                msg += e.StackTrace;
                MessageBox.Show(msg, "Kiva installer has crashed!");
            }
        }
    }
}
