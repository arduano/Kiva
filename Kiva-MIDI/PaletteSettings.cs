using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Kiva_MIDI
{
    public class PaletteSettings
    {
        public Dictionary<string, Bitmap> Palettes { get; } = new Dictionary<string, Bitmap>();

        public PaletteSettings() { }

        public void Reload()
        {
            float mult = 0.12345f;
            string searchPath = "Palettes";
            if (!Directory.Exists(searchPath)) Directory.CreateDirectory(searchPath);
            using (Bitmap palette = new Bitmap(16, 8))
            {
                for (int i = 0; i < 16 * 8; i++)
                {
                    palette.SetPixel(i % 16, (i - i % 16) / 16, HsvToRgb(i * mult % 1, 1, 1, 1));
                }
                palette.Save(Path.Combine(searchPath, "Random.png"));
            }
            using (Bitmap palette = new Bitmap(32, 8))
            {
                for (int i = 0; i < 32 * 8; i++)
                {
                    palette.SetPixel(i % 32, (i - i % 32) / 32, HsvToRgb(i * mult % 1, 1, 1, 1));
                    i++;
                    palette.SetPixel(i % 32, (i - i % 32) / 32, HsvToRgb(((i - 1) * mult + 0.166f) % 1, 1, 1, 1));
                }
                palette.Save(Path.Combine(searchPath, "Random Gradients.png"));
            }
            using (Bitmap palette = new Bitmap(32, 8))
            {
                for (int i = 0; i < 32 * 8; i++)
                {
                    palette.SetPixel(i % 32, (i - i % 32) / 32, HsvToRgb(i * mult % 1, 1, 1, 0.8));
                    i++;
                    palette.SetPixel(i % 32, (i - i % 32) / 32, HsvToRgb(((i - 1) * mult + 0.166f) % 1, 1, 1, 0.8));
                }
                palette.Save(Path.Combine(searchPath, "Random Alpha Gradients.png"));
            }
            using (Bitmap palette = new Bitmap(16, 8))
            {
                for (int i = 0; i < 16 * 8; i++)
                {
                    palette.SetPixel(i % 16, (i - i % 16) / 16, HsvToRgb(i * mult % 1, 1, 1, 0.8));
                }
                palette.Save(Path.Combine(searchPath, "Random with Alpha.png"));
            }
            var imagePaths = Directory.GetFiles(searchPath).Where(s => s.EndsWith(".png")).ToArray();

            foreach (var img in Palettes.Values) img.Dispose();
            Palettes.Clear();

            Array.Sort(imagePaths, new Comparison<string>((s1, s2) =>
            {
                if (s1.Contains("Random.png")) return -1;
                if (s2.Contains("Random.png")) return 1;
                else return 0;
            }));

            foreach (var i in imagePaths)
            {
                try
                {
                    using (var fs = new System.IO.FileStream(i, System.IO.FileMode.Open))
                    {
                        Bitmap img = new Bitmap(fs);
                        if (!(img.Width == 16 || img.Width == 32) || img.Width < 1) continue;
                        string key = Path.GetFileNameWithoutExtension(i);
                        if (!Palettes.ContainsKey(key))
                            Palettes.Add(key, img);
                    }
                }
                catch
                {

                }
            }
            ReadPFAConfig();
        }

        void ReadPFAConfig()
        {
            try
            {
                var appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var configPath = Path.Combine(appdata, "Piano From Above/Config.xml");
                if (File.Exists(configPath))
                {
                    var data = File.ReadAllText(configPath);
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(data);
                    var colors = doc.GetElementsByTagName("Colors").Item(0);
                    Bitmap img = new Bitmap(16, 1);
                    for (int i = 0; i < 16; i++)
                    {
                        var c = colors.ChildNodes.Item(i);
                        int r = -1;
                        int g = -1;
                        int b = -1;
                        for (int j = 0; j < 3; j++)
                        {
                            var attrib = c.Attributes.Item(j);
                            if (attrib.Name == "R") r = Convert.ToInt32(attrib.InnerText);
                            if (attrib.Name == "G") g = Convert.ToInt32(attrib.InnerText);
                            if (attrib.Name == "B") b = Convert.ToInt32(attrib.InnerText);
                        }
                        img.SetPixel(i, 0, Color.FromArgb(r, g, b));
                    }
                    if (!Palettes.ContainsKey("PFA Config Colors"))
                        Palettes.Add("PFA Config Colors", img);

                }
            }
            catch { }
        }

        Color HsvToRgb(double h, double S, double V, double a)
        {
            int r, g, b;
            HsvToRgb(h * 360, S, V, out r, out g, out b);
            return Color.FromArgb((int)(a * 255), r, g, b);
        }

        void HsvToRgb(double h, double S, double V, out int r, out int g, out int b)
        {
            double H = h;
            while (H < 0) { H += 360; };
            while (H >= 360) { H -= 360; };
            double R, G, B;
            if (V <= 0)
            { R = G = B = 0; }
            else if (S <= 0)
            {
                R = G = B = V;
            }
            else
            {
                double hf = H / 60.0;
                int i = (int)Math.Floor(hf);
                double f = hf - i;
                double pv = V * (1 - S);
                double qv = V * (1 - S * f);
                double tv = V * (1 - S * (1 - f));
                switch (i)
                {

                    // Red is the dominant color

                    case 0:
                        R = V;
                        G = tv;
                        B = pv;
                        break;

                    // Green is the dominant color

                    case 1:
                        R = qv;
                        G = V;
                        B = pv;
                        break;
                    case 2:
                        R = pv;
                        G = V;
                        B = tv;
                        break;

                    // Blue is the dominant color

                    case 3:
                        R = pv;
                        G = qv;
                        B = V;
                        break;
                    case 4:
                        R = tv;
                        G = pv;
                        B = V;
                        break;

                    // Red is the dominant color

                    case 5:
                        R = V;
                        G = pv;
                        B = qv;
                        break;

                    // Just in case we overshoot on our math by a little, we put these here. Since its a switch it won't slow us down at all to put these here.

                    case 6:
                        R = V;
                        G = tv;
                        B = pv;
                        break;
                    case -1:
                        R = V;
                        G = pv;
                        B = qv;
                        break;

                    // The color is not defined, we should throw an error.

                    default:
                        //LFATAL("i Value error in Pixel conversion, Value is %d", i);
                        R = G = B = V; // Just pretend its black/white
                        break;
                }
            }
            r = Clamp((int)(R * 255.0));
            g = Clamp((int)(G * 255.0));
            b = Clamp((int)(B * 255.0));
        }

        int Clamp(int i)
        {
            if (i < 0) return 0;
            if (i > 255) return 255;
            return i;
        }
    }
}
