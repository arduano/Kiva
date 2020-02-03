using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO.Compression;
using KivaShared;
using System.Threading;

namespace Kiva_MIDI
{
    public class Settings
    {
        class loadingSettings { public dynamic version, midi, general; };
        class versionSettings { public string version; public bool enableUpdates; public bool installed; };

        public string VersionName { get; } = "v1.1.12";
        public bool Installed { get; } = false;
        public bool EnableUpdates { get; } = false;

        public string InstallPath;
        static readonly string SettingsFolderPath = Path.Combine("Settings");

        public static readonly string CommonSoundfonts = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Common SoundFonts", "SoundFontList.csflist");

        string versionPath;
        string midiPath;
        string generalPath;

        public VolatileSettings Volatile { get; set; } = new VolatileSettings();
        public GeneralSettings General { get; set; } = new GeneralSettings();
        public SoundfontSettings Soundfonts { get; set; } = new SoundfontSettings();
        MIDILoaderSettings loaderSettings;

        FileSystemWatcher soundfontWatcher;

        public PaletteSettings PaletteSettings { get; } = new PaletteSettings();

        public MIDILoaderSettings GetMIDILoaderSettings()
        {
            return loaderSettings.Clone();
        }

        public void UpdateMIDILoaderSettings(MIDILoaderSettings s)
        {
            SaveSetings(s, midiPath);
        }

        public Settings()
        {
            if (!Directory.Exists(SettingsFolderPath)) Directory.CreateDirectory(SettingsFolderPath);

            versionPath = Path.Combine(SettingsFolderPath, "meta.kvs");
            midiPath = Path.Combine(SettingsFolderPath, "midi.kvs");
            generalPath = Path.Combine(SettingsFolderPath, "general.kvs");

            loadingSettings loading = new loadingSettings();

            bool saveSettings = false;
            if (File.Exists(versionPath)) loading.version = LoadSetings<versionSettings>(versionPath);
            else
            {
                loading.version = new versionSettings() { version = VersionName, enableUpdates = EnableUpdates, installed = Installed };
            }
            if (File.Exists(midiPath)) loading.midi = LoadSetings<MIDILoaderSettings>(midiPath);
            else
            {
                loading.midi = new MIDILoaderSettings();
                saveSettings = true;
            }
            if (File.Exists(generalPath)) loading.general = LoadSetings<GeneralSettings>(generalPath);
            else
            {
                loading.general = new GeneralSettings();
                saveSettings = true;
            }

            VersionName = (string)loading.version.version;
            EnableUpdates = (bool)loading.version.enableUpdates;
            Installed = (bool)loading.version.installed;

            if (saveSettings)
            {
                SaveSetings(loading.midi, midiPath);
                SaveSetings(loading.general, generalPath);
            }

            loaderSettings = (MIDILoaderSettings)loading.midi;

            General = loading.general;

            PaletteSettings.Reload();
            if (!PaletteSettings.Palettes.ContainsKey(General.PaletteName))
            {
                General.PaletteName = PaletteSettings.Palettes.Keys.First();
            }

            General.PropertyChanged += (s, e) =>
        {
            SaveSetings(General, generalPath);
        };
        }

        dynamic UpdateSettings(dynamic settings)
        {
            switch (((versionSettings)settings.version).version)
            {

            }
            return settings;
        }

        public void SaveSetings(dynamic obj, string path)
        {
            var stream = new StreamWriter(new GZipStream(File.Open(path, FileMode.Create), CompressionMode.Compress));
            stream.Write(JsonConvert.SerializeObject(obj));
            stream.Close();
        }

        public dynamic LoadSetings<T>(string path)
        {
            var stream = new StreamReader(new GZipStream(File.Open(path, FileMode.Open), CompressionMode.Decompress));
            var text = stream.ReadToEnd();
            var obj = JsonConvert.DeserializeObject<T>(text);
            stream.Close();
            return obj;
        }

        bool justWroteSF = false;

        string lastWrittenText = null;

        void ParseSoundfonts()
        {
            try
            {
                string[] lines = null;
                for (int i = 0; i < 100; i++)
                {
                    try
                    {
                        var text = File.ReadAllText(CommonSoundfonts);
                        if (text == lastWrittenText) return;
                        lines = text.Split('\n');
                        break;
                    }
                    catch
                    {
                        Thread.Sleep(10);
                    }
                }
                if (lines == null || lines.Length == 0)
                    Soundfonts.Soundfonts = new SoundfontData[0];
                Soundfonts.ParseFile(lines);
            }
            catch (Exception e)
            {
                try
                {
                    justWroteSF = true;
                    File.WriteAllText(CommonSoundfonts, "");
                    lastWrittenText = "";
                    Soundfonts.Soundfonts = new SoundfontData[0];
                }
                catch
                {
                }

                Soundfonts.Soundfonts = new SoundfontData[0];
            }
        }

        public void InitSoundfontListner()
        {
            if (!File.Exists(CommonSoundfonts))
            {
                if (!Directory.Exists(Path.GetDirectoryName(CommonSoundfonts))) Directory.CreateDirectory(Path.GetDirectoryName(CommonSoundfonts));
                File.WriteAllText(CommonSoundfonts, "");
                lastWrittenText = "";
            }
            soundfontWatcher = new FileSystemWatcher();
            soundfontWatcher.Path = Path.GetDirectoryName(CommonSoundfonts);
            soundfontWatcher.NotifyFilter = NotifyFilters.LastWrite;
            soundfontWatcher.Filter = Path.GetFileName(CommonSoundfonts);
            soundfontWatcher.Changed += OnSoundfontsChanged;
            soundfontWatcher.EnableRaisingEvents = true;

            ParseSoundfonts();
            Soundfonts.OnSave += (s) =>
            {
                justWroteSF = true;
                for (int i = 0; i < 100; i++)
                {
                    try
                    {
                        File.WriteAllText(CommonSoundfonts, s);
                        lastWrittenText = s;
                        break;
                    }
                    catch
                    {
                        Thread.Sleep(10);
                    }
                }
            };
        }

        private void OnSoundfontsChanged(object sender, FileSystemEventArgs e)
        {
            if (justWroteSF)
            {
                justWroteSF = false;
                return;
            }
            ParseSoundfonts();
        }
    }
}
