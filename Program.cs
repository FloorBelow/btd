using System.Runtime.InteropServices;
using LibDeflate;
using ImageMagick;
using System.Buffers;

namespace BTD_Tests {
    internal class Program {
        static void Main(string[] args) {


            //BtdStarfield btd2 = new BtdStarfield(@"F:\Extracted\Starfield\terrain\craterssharplarge2k\craterssharplarge2k01.btd");

            //btd2.Write("a.btd");
            //return;

            // for (int i = 3; i >= 0; i--) btd2.Export(i, BtdStarfield.TerrainMode.ltex, "ltex");
            //return;

            MagickImage skyrimImage = new MagickImage(@"E:\Anna\Anna\Visual Studio\BTD_Tests\bin\Debug\net6.0\skyrim.png");
            ushort[] skyrimHeights = new ushort[skyrimImage.Width * skyrimImage.Height];
            {
                int i = 0;
                foreach(var pixel in skyrimImage.GetPixels()) {
                    skyrimHeights[i] = pixel[0];
                    i++;
                }
            }

            //skyrimImage.Write(skyrimHeights, )

            int startLength = @"F:\Extracted\Starfield\".Length;
            foreach (string path in Directory.EnumerateFiles(@"F:\Extracted\Starfield\terrain", "*.btd", SearchOption.AllDirectories)) {
                string filename = Path.GetFileName(path);
                if (!filename.Contains("mountainssharp") || (!filename.Contains("2k"))) continue; //&& !filename.Contains("1k")
                

                BtdStarfield btd = new BtdStarfield(path);
                if (btd.sizeX != 3328 || btd.sizeY != 3328) continue;
                //btd.maxHeight =  btd.maxHeight / 4;
                //btd.minHeight = btd.minHeight / 4;

                //Console.WriteLine($"{Path.GetFileNameWithoutExtension(path)}|{btd.maxHeight - btd.minHeight}");

                //continue;
                string outPath = path.Substring(startLength);
                if (!Directory.Exists(Path.GetDirectoryName(outPath))) Directory.CreateDirectory(Path.GetDirectoryName(outPath));
                Console.WriteLine(outPath);

                //File.Copy(@"F:\Extracted\Starfield\terrain\craterssharplarge2k\craterssharplarge2k01.btd", outPath, true);
                btd.Write(outPath, skyrimHeights, 0.6f);


            }
            return;


            Biom biom = new Biom(@"E:\Extracted\Starfield\planetdata\biomemaps\volii beta.biom");
            for(int i = 0; i < biom.hemispheres[0].unk1.Length; i++) { biom.hemispheres[0].unk1[i] = 0; }
            for (int i = 0; i < biom.hemispheres[1].unk1.Length; i++) { biom.hemispheres[1].unk1[i] = 0; }
            biom.ListResourceVals();
            biom.Write();

            /*
            foreach(string path in Directory.EnumerateFiles(@"E:\Extracted\Starfield\planetdata\biomemaps\", "*.biom")) {
                //Biom biom = new Biom(@"E:\Extracted\Starfield\planetdata\biomemaps\nesoi.biom");
                Biom biom = new Biom(path);
                biom.SaveUnk1();

            }
            return;

            foreach (string path in Directory.EnumerateFiles(@"F:\Extracted\Starfield\terrain", "*.btd", SearchOption.AllDirectories)) {
                BtdStarfield btd = new BtdStarfield(path);
                btd.Export(0, BtdStarfield.TerrainMode.height, Path.GetFileName(path));
            }
            */

            /*
            BtdStarfield btd = new BtdStarfield(@"F:\Extracted\Starfield\terrain\canyonssharplarge1k\canyonssharplarge1kinnercurve01.btd");
            for(int i = 0; i < 4; i++) {
                btd.Export(i);
            }
            //btd.DumpZlibBlocks();

            
            Btd btd = new Btd(@"F:\SteamLibrary\steamapps\common\Fallout76\Data\Terrain\Appalachia.btd");
            Console.WriteLine($"{btd.cellsX}x{btd.cellsY}");

            btd.Export(2, Btd.TerrainMode.height);
            btd.Export(2, Btd.TerrainMode.ltex);
            btd.Export(2, Btd.TerrainMode.color);

            //for(int i = 0; i < btd.lod3Offsets.Length / 2; i++) btd.WriteLod3Height(i);
            Console.WriteLine("DONE");
            */
        }
    }


}