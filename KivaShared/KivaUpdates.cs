using IWshRuntimeLibrary;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using File = System.IO.File;

namespace KivaShared
{
    public static class KivaUpdates
    {
        public static readonly string DefaultUpdatePackagePath = "Updates\\pkg.zip";
        public static readonly string DataAssetName = "KivaPortable.zip";
        public static readonly string InstallerPath = "Updates\\ins.exe";

        public static dynamic GetHTTPJSON(string uri)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.UserAgent = "Kiva";

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                using (Stream stream = response.GetResponseStream())
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        return JsonConvert.DeserializeObject(reader.ReadToEnd());
                    }
                }
            }
        }

        public static Stream GetHTTPData(string uri)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.UserAgent = "Kiva";

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                using (Stream stream = response.GetResponseStream())
                {
                    MemoryStream data = new MemoryStream();
                    stream.CopyTo(data);
                    data.Position = 0;
                    return data;
                }
            }
        }

        public static string GetLatestVersion()
        {
            var data = GetHTTPJSON("https://api.github.com/repos/arduano/Kiva/releases/latest");
            return (string)data.tag_name;
        }

        public static Stream DownloadAssetData(string filename)
        {
            var data = GetHTTPJSON("https://api.github.com/repos/arduano/Kiva/releases/latest");
            var assets = (JArray)data.assets;
            var asset = (dynamic)assets.Where(a => ((dynamic)a).name == filename).First();
            var url = (string)asset.browser_download_url;
            return GetHTTPData(url);
        }

        public static bool IsAnotherKivaRunning()
        {
            var kivas = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetEntryAssembly().Location)).ToArray();
            if (kivas.Length > 1) return true;
            return false;
        }

        public static void KillAllKivas()
        {
            var kivas = Process.GetProcesses().Where(p => p.ProcessName == "Kiva" || p.ProcessName == "Kiva-MIDI");
            var current = Process.GetCurrentProcess();
            foreach (var k in kivas)
            {
                if (k.Id == current.Id) continue;
                if (!k.HasExited) k.Kill();
                try
                {
                    k.WaitForExit(10000);
                }
                catch { continue; }
                if (!k.HasExited) throw new InstallFailedException("Could not kill process \"Kiva\" with pid " + k.Id);
            }
        }

        public static void InstallFromStream(Stream s)
        {
            var basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Kiva");
            using (ZipArchive archive = new ZipArchive(s))
            {
                foreach (var e in archive.Entries)
                {
                    if (e.FullName.EndsWith("\\") || e.FullName.EndsWith("/")) continue;
                    if (!Directory.Exists(Path.Combine(basePath, Path.GetDirectoryName(e.FullName))))
                        Directory.CreateDirectory(Path.Combine(basePath, Path.GetDirectoryName(e.FullName)));
                    try
                    {
                        e.ExtractToFile(Path.Combine(basePath, e.FullName), true);
                    }
                    catch (IOException ex)
                    {
                        throw new InstallFailedException("Could not overwrite file " + Path.Combine(basePath, e.FullName));
                    }
                }
            }
        }

        public static void WriteVersionSettings(string version, bool autoUpdate = true, bool installed = true)
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Kiva/Settings/meta.kvs");
            if (!Directory.Exists(Path.GetDirectoryName(path))) Directory.CreateDirectory(Path.GetDirectoryName(path));
            var stream = new StreamWriter(new GZipStream(File.Open(path, FileMode.Create), CompressionMode.Compress));
            stream.Write("{\"version\":\"" + version + "\",\"enableUpdates\":" + (autoUpdate ? "true" : "false") + ",\"installed\":" + (installed ? "true" : "false") + "}");
            stream.Close();
        }

        public static void CopySelfInside(string path)
        {
            var p = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Kiva", path);
            if (!Directory.Exists(Path.GetDirectoryName(p))) Directory.CreateDirectory(Path.GetDirectoryName(p));
            File.Copy(System.Reflection.Assembly.GetEntryAssembly().Location, p, true);
        }

        public static void CreateStartShortcut()
        {
            string shortcutLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs\\Kiva.lnk");
            if (File.Exists(shortcutLocation)) File.Delete(shortcutLocation);
            WshShell shell = new WshShell();
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutLocation);

            shortcut.Description = "Kiva";
            shortcut.TargetPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Kiva\\Kiva.exe");
            shortcut.WorkingDirectory = Path.GetDirectoryName(shortcut.TargetPath);
            shortcut.Save();
        }

        public static void DeleteStartShortcut()
        {
            string shortcutLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs\\Kiva.lnk");
            if (File.Exists(shortcutLocation)) File.Delete(shortcutLocation);
        }

        public static void CreateDesktopShortcut()
        {
            string shortcutLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Kiva.lnk");
            if (File.Exists(shortcutLocation)) File.Delete(shortcutLocation);
            WshShell shell = new WshShell();
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutLocation);

            shortcut.Description = "Kiva";
            shortcut.TargetPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Kiva\\Kiva.exe");
            shortcut.WorkingDirectory = Path.GetDirectoryName(shortcut.TargetPath);
            shortcut.Save();
        }

        public static void DeleteDesktopShortcut()
        {
            string shortcutLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Kiva.lnk");
            if (File.Exists(shortcutLocation)) File.Delete(shortcutLocation);
        }

        public static void DeleteKivaFolder()
        {
            Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Kiva"), true);
        }

        public static void RegisterControlPanelProgram()
        {
            string installLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Kiva");
            string Install_Reg_Loc = @"Software\Microsoft\Windows\CurrentVersion\Uninstall";
            RegistryKey hKey = (Registry.LocalMachine).OpenSubKey(Install_Reg_Loc, true);
            if (hKey.GetSubKeyNames().Contains("Kiva")) hKey.DeleteSubKey("Kiva");
            RegistryKey appKey = hKey.CreateSubKey("Kiva");

            appKey.SetValue("DisplayName", "Kiva", RegistryValueKind.String);
            appKey.SetValue("Publisher", "Arduano", RegistryValueKind.String);
            appKey.SetValue("InstallLocation", installLocation, RegistryValueKind.ExpandString);
            appKey.SetValue("DisplayIcon", Path.Combine(installLocation, "Kiva.exe"), RegistryValueKind.String);
            appKey.SetValue("UninstallString", Path.Combine(installLocation, "Updates\\ins.exe uninstall"), RegistryValueKind.ExpandString);
            appKey.SetValue("NoModify", 1, RegistryValueKind.DWord);
            appKey.SetValue("NoRepair", 1, RegistryValueKind.DWord);
        }

        public static void RemoveControlPanelProgram()
        {
            string Install_Reg_Loc = @"Software\Microsoft\Windows\CurrentVersion\Uninstall";
            RegistryKey hKey = (Registry.LocalMachine).OpenSubKey(Install_Reg_Loc, true);
            if (hKey.GetSubKeyNames().Contains("Kiva")) hKey.DeleteSubKey("Kiva");
        }

        public static void CreateUninstallScript()
        {
            File.WriteAllText(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Kiva\\uninstall.bat"),
                @"
cd ..
copy %appdata%\Kiva\Updates\ins.exe %temp%\kivains.exe
%temp%\kivains.exe uninstall
del %temp%\kivains.exe
"
            );
        }
    }
}
