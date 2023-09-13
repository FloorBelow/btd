using System.Runtime.InteropServices;
using LibDeflate;
using ImageMagick;
using System.Buffers;

namespace BTD_Tests {
    internal class Program {
        static void Main(string[] args) {

            int startLength = @"F:\Extracted\Starfield\".Length;
            foreach (string path in Directory.EnumerateFiles(@"F:\Extracted\Starfield\terrain", "*.btd", SearchOption.AllDirectories)) {
                string filename = Path.GetFileName(path);
                if (!filename.Contains("hillsrocky")  || (!filename.Contains("2k"))) continue; //&& !filename.Contains("1k")

                //BtdStarfield btd = new BtdStarfield(path);
                //btd.maxHeight =  btd.maxHeight / 4;
                //btd.minHeight = btd.minHeight / 4;

                string outPath = path.Substring(startLength);
                if (!Directory.Exists(Path.GetDirectoryName(outPath))) Directory.CreateDirectory(Path.GetDirectoryName(outPath));
                Console.WriteLine(outPath);

                File.Copy(@"F:\Extracted\Starfield\terrain\craterssharplarge2k\craterssharplarge2k01.btd", outPath, true);
                //btd.Write(outPath);


            }
            return;



            BtdStarfield btd2 = new BtdStarfield(@"F:\Extracted\Starfield\terrain\craterssharplarge2k\craterssharplarge2k01.btd");
            btd2.Write(Path.GetFileNameWithoutExtension(btd2.path) + "_edit.btd");

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