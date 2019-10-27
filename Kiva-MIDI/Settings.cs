using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO.Compression;

namespace Kiva_MIDI
{
    public class Settings
    {
        class loadingSettings { public dynamic version, midi, general; };
        class versionSettings { public string version; };

        public static readonly string VersionName = "Beta";
        public static readonly string SettingsVersion = "1";
        public string InstallPath;
        //static readonly string SettingsFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Kiva/Settings");
        static readonly string SettingsFolderPath = Path.Combine("Settings");

        string versionPath;
        string midiPath;
        string generalPath;

        public VolatileSettings Volatile { get; set; } = new VolatileSettings();
        public GeneralSettings General { get; set; } = new GeneralSettings();
        MIDILoaderSettings loaderSettings;

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
                loading.version = new versionSettings() { version = SettingsVersion };
                saveSettings = true;
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

            while ((loading.version).version != SettingsVersion)
            {
                loading = UpdateSettings(loading);
                saveSettings = true;
            }

            if (saveSettings)
            {
                SaveSetings(loading.version, versionPath);
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
    }
}
