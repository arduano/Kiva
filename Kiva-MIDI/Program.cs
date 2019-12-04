using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using KivaShared;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;

namespace Kiva_MIDI
{
    class Program
    {
        public static bool UpdateReady { get; private set; } = false;
        public static bool UpdateDownloading { get; private set; } = false;

        [STAThread]
        static void Main(string[] args)
        {
#if !DEBUG
            try
            {
#endif
            Directory.SetCurrentDirectory(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName));

            MIDIAudio.Init();

            var s = new Settings();
            s.InitSoundfontListner();
            if (s.EnableUpdates)
            {
                if (File.Exists(KivaUpdates.DefaultUpdatePackagePath))
                {
                    try
                    {
                        using (var z = File.OpenRead(KivaUpdates.DefaultUpdatePackagePath))
                        using (ZipArchive archive = new ZipArchive(z))
                        { }
                        UpdateReady = true;
                        if (!KivaUpdates.IsAnotherKivaRunning())
                        {
                            if (args.Length == 0) Process.Start(KivaUpdates.InstallerPath, "update -Reopen");
                            else Process.Start(KivaUpdates.InstallerPath, "update -Reopen -ReopenArg \"" + args[0] + "\"");
                        }
                    }
                    catch (Exception e) { TryDownloadUpdatePackage(s.VersionName); }
                }
                else
                {
                    TryDownloadUpdatePackage(s.VersionName);
                }
            }

            var window = new MainWindow(s);
            if (args.Length != 0)
            {
                window.LoadMidi(args[0]);
            }
            window.ShowDialog();
#if !DEBUG
            }
            catch (Exception e)
            {
                string msg = e.Message + "\n" + e.Data + "\n";
                msg += e.StackTrace;
                MessageBox.Show(msg, "Kiva has crashed!");
            }
#endif
        }

        static void TryDownloadUpdatePackage(string currVersion)
        {
            Task.Run(() =>
            {
                bool updateAvailable = false;
                try
                {
                    if (KivaUpdates.GetLatestVersion() != currVersion) updateAvailable = true;
                }
                catch { }
                if (updateAvailable)
                {
                    UpdateDownloading = true;
                    try
                    {
                        var data = KivaUpdates.DownloadAssetData(KivaUpdates.DataAssetName);
                        var dest = File.OpenWrite(KivaUpdates.DefaultUpdatePackagePath);
                        data.CopyTo(dest);
                        data.Close();
                        dest.Close();
                        UpdateReady = true;
                        UpdateDownloading = false;
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show("Couldn't download and save update package", "Update failed");
                        UpdateDownloading = false;
                    }
                }
            });
        }
    }
}
