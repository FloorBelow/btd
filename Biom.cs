using ImageMagick;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BTD_Tests {
    public class Biom {
        string name;
        int[] biomeList;
        byte version;
        byte unk0;
        Hemisphere[] hemispheres;

        public Biom(string path) {
            name = Path.GetFileNameWithoutExtension(path);
            using(BinaryReader reader = new BinaryReader(File.OpenRead(path))) {
                version = reader.ReadByte();
                unk0 = reader.ReadByte();
                biomeList = new int[reader.ReadInt32()];
                for (int i = 0; i < biomeList.Length; i++) biomeList[i] = reader.ReadInt32();
                hemispheres = new Hemisphere[reader.ReadInt32()];
                for(int i = 0; i < hemispheres.Length; i++) {
                    hemispheres[i] = new Hemisphere(reader);
                }
            }
        }

        public void SaveBiomeMap() {
            for (int i = 0; i < hemispheres.Length; i++) hemispheres[i].SaveMap(biomeList, $"{name}_{i}.png");
        }

        class Hemisphere {

            static MagickColor[] colors = new MagickColor[] { MagickColors.Blue, MagickColors.Green, MagickColors.Red, MagickColors.Yellow, MagickColors.Purple, MagickColors.Cyan, MagickColors.White, MagickColors.Black };
            static uint[] colorsHex = new uint[] { 0xffff0000, 0xff00ff00, 0xff0000ff, 0xff00ffff, 0xffff00ff, 0xffffff00, 0xffffffff, 0xff000000 };

            int x;
            int y;
            int[] biomes;
            byte[] unk1;

            public Hemisphere(BinaryReader reader) {
                x = reader.ReadInt32();
                y = reader.ReadInt32();
                biomes = new int[reader.ReadInt32()];
                for(int i = 0; i < biomes.Length; i++) {
                    biomes[i] = reader.ReadInt32();
                }
                unk1 = new byte[reader.ReadInt32()];
                for (int i = 0; i < unk1.Length; i++) unk1[i] = reader.ReadByte();
            }

            public void SaveMap(int[] biomeList, string name) {


                Dictionary<int, uint> colorDict = new Dictionary<int, uint>();
                for(int i = 0; i < biomeList.Length; i++) {
                    colorDict[biomeList[i]] = colorsHex[i];
                }

                uint[] data = new uint[x * y];
                for (int i = 0; i < data.Length; i++) {
                    data[i] = colorDict[biomes[i]];
                }

                Span<byte> span = MemoryMarshal.AsBytes(data.AsSpan());

                MagickImage image = new MagickImage();
                image.Read(span, new MagickReadSettings() { Width = x, Height = y, Depth = 8, Format = MagickFormat.Rgba });
                image.Write(name);
                Console.WriteLine(name);
            }
        }
    }
}
