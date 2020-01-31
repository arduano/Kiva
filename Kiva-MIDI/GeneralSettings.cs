using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Kiva_MIDI
{
    public enum KeyRangeTypes
    {
        Key88,
        Key128,
        Key256,
        KeyMIDI,
        KeyDynamic,
        Custom
    }

    public enum KeyboardStyle
    {
        None,
        Big,
        Small,
    }

    public enum AudioEngine
    {
        KDMAPI,
        WinMM,
        PreRender
    }

    public enum CardParams
    {
        FPS = 1,
        NoteCount = 2,
        NPS = 4,
        Polyphony = 8,
        Time = 16,
        RenderedNotes = 32,
        AudioBuffer = 64,
        FakeFps = 128
    }

    public class GeneralSettings : INotifyPropertyChanged
    {
        public KeyRangeTypes KeyRange { get; set; } = KeyRangeTypes.KeyDynamic;
        public int CustomFirstKey { get; set; } = 0;
        public int CustomLastKey { get; set; } = 127;

        public KeyboardStyle KeyboardStyle { get; set; } = KeyboardStyle.Big;

        public int FPSLock { get; set; } = 60;
        public bool CompatibilityFPS { get; set; } = false;
        public bool SyncFPS { get; set; } = true;

        public Color BackgroundColor { get; set; } = Color.FromArgb(255, 142, 142, 142);
        public Color ForegroundColor { get; set; } = Color.FromArgb(255, 142, 142, 142);
        public Color BarColor { get; set; } = Color.FromArgb(255, 0x00, 0x68, 0xC9);

        public string PaletteName { get; set; } = "Random.png";
        public bool PaletteRandomized { get; set; } = true;

        public bool HideInfoCard { get; set; } = false;
        public CardParams InfoCardParams { get; set; } = CardParams.AudioBuffer | CardParams.FPS | CardParams.NoteCount | CardParams.NPS | CardParams.Polyphony | CardParams.RenderedNotes | CardParams.Time;

        public bool MainWindowTopmost { get; set; } = false;

        public AudioEngine SelectedAudioEngine { get; set; } = AudioEngine.PreRender;

        public int RenderBufferLength { get; set; } = 60;
        public int RenderVoices { get; set; } = 1000;
        public bool RenderNoFx { get; set; } = false;
        public double RenderSimulateLag { get; set; } = 0;

        public bool DiscordRP { get; set; } = false;

        public int SelectedMIDIDevice { get; set; } = -1;
        public string SelectedMIDIDeviceName { get; set; } = "";

        public bool MultiThreadedRendering { get; set; } = true;
        public int MaxRenderThreads { get; set; } = 0;

        public bool DisableTransparency { get; set; } = false;

        public event PropertyChangedEventHandler PropertyChanged;

        public GeneralSettings()
        {
            try
            {
                SelectedMIDIDevice = KDMAPI.IsKDMAPIAvailable() ? -1 : 0;
            }
            catch { SelectedMIDIDevice = 0; }
            if (MaxRenderThreads <= 0) MaxRenderThreads = Environment.ProcessorCount;
            if (MaxRenderThreads > Environment.ProcessorCount) MaxRenderThreads = Environment.ProcessorCount;
        }
    }
}
