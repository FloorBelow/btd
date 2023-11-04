using ImageMagick;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BTD_Tests {
    internal class Esm {
        public static void TestSFTR() {
            Dictionary<string, uint> legend = new Dictionary<string, uint>();

            MagickImageCollection images = new MagickImageCollection();


            {
                uint[] data = new uint[256 * 512];

                Random r = new Random();
                int i = 0;
                foreach (string line in File.ReadAllLines(@"E:\Anna\Anna\Delphi\TES5Edit\Build\Edit Scripts\aaaSFTR_NesoiSurfaceTree.txt")) {
                    string trimLine = line;
                    while (char.IsDigit(trimLine[trimLine.Length - 1])) trimLine = trimLine.Substring(0, trimLine.Length - 1);
                    if (!legend.ContainsKey(trimLine)) {
                        legend[trimLine] = (uint)(r.Next(256) + (r.Next(256) << 8) + (r.Next(256) << 16) + (0xff << 24));
                    }
                    data[i] = legend[trimLine];
                    i++; if (i == 256 * 512) break;
                }
                Span<byte> byteData = MemoryMarshal.AsBytes(data.AsSpan());
                MagickImage image = new MagickImage(byteData, new MagickReadSettings() { Width = 256, Height = 512, Depth = 8, Format = MagickFormat.Rgba });
                images.Add(image);

            }






            {
                List<string> patterns = new List<string>();
                foreach (string pattern in legend.Keys) patterns.Add(pattern);
                patterns.Sort();
                int keyHeight = 24;
                uint[] data = new uint[256 * keyHeight];
                Span<byte> keyByteData = MemoryMarshal.AsBytes(data.AsSpan());
                MagickReadSettings readSettings = new MagickReadSettings() { Width = 256, Height = keyHeight, Depth = 8, Format = MagickFormat.Rgba };

                foreach (string pattern in patterns) {
                    uint val = legend[pattern];
                    for(int i = 0; i < data.Length; i++) { data[i] = val; }



                    MagickImage key = new MagickImage(keyByteData, readSettings);
                    key.Draw(new Drawables().Gravity(Gravity.Center).FontPointSize(14).FillColor(MagickColors.White).Text(0, 0, pattern));
                    images.Add(key);
                }

            }

            var montage = images.AppendVertically();
            montage.Depth = 8;
            montage.Write("TestSFTR.png");

            //image.Write("TestSFTR.png");
        }

    }
}
