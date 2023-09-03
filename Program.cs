using System.Runtime.InteropServices;
using LibDeflate;
using ImageMagick;
using System.Buffers;

namespace BTD_Tests {
    internal class Program {
        static void Main(string[] args) {
            foreach(string path in Directory.EnumerateFiles(@"F:\Extracted\Starfield\terrain", "*.btd", SearchOption.AllDirectories)) {
                BtdStarfield btd = new BtdStarfield(path);
                btd.Export(0, BtdStarfield.TerrainMode.height, Path.GetFileName(path));
            }

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


    class Btd {

        public string path;

        public int version;
        public float minHeight;  
        public float maxHeight;
        public int sizeX; 
        public int sizeY;
        public int minX;  
        public int minY;
        public int maxX;  
        public int maxY;
        public float[] cellMinHeights;
        public float[] cellMaxHeights;

        public uint[] ltexIds;
        public ulong[] quadrantLtexIds;

        public uint[] gcvrIds;
        public ulong[] quadrantGcvrIds;

        public ushort[] globalHeights;
        public ushort[] globalLtex;
        public ushort[] globalVclr;

        public uint[][] blockOffsets;
        public int[][] blockSizes;

        public uint[] gcvrOffsets;
        public int[] gcvrSizes;

        public long zlibOffset;


        //helpful helper variaables
        public int cellsX;
        public int cellsY;
        public int[] blockCellsX;
        public int[] blockCellsY;

        //static
        static int[] cellSizes = { 128, 64, 32, 16, 8 };
        static int[] blockCellCounts = { 1, 2, 4, 8 };

        public Btd(string path) {
            using(BinaryReader reader = new BinaryReader(File.OpenRead(path))) {
                this.path = path;
                string magic = new string(reader.ReadChars(4)); if (magic != "BTDB") throw new Exception("MAGIC WRONG " + magic);
                version = reader.ReadInt32();
                minHeight = reader.ReadInt32(); 
                maxHeight = reader.ReadInt32();
                sizeX = reader.ReadInt32(); 
                sizeY = reader.ReadInt32();
                minX = reader.ReadInt32(); 
                minY = reader.ReadInt32();
                maxX = reader.ReadInt32(); 
                maxY = reader.ReadInt32();

                cellsX = maxX - minX + 1; //does this only work if max and min are different sign?
                cellsY = maxY - minY + 1;
                blockCellsX = new int[] { cellsX, (cellsX + 1) / 2, (cellsX + 3) / 4, (cellsX + 7) / 8 };
                blockCellsY = new int[] { cellsY, (cellsY + 1) / 2, (cellsY + 3) / 4, (cellsY + 7) / 8 };

                ltexIds = new uint[reader.ReadInt32()];
                for(int i =  0; i < ltexIds.Length; i++) { ltexIds[i] = reader.ReadUInt32(); }

                cellMinHeights = new float[cellsX * cellsY];
                cellMaxHeights = new float[cellMinHeights.Length];
                for(int i = 0; i < cellMinHeights.Length; i++) {
                    cellMinHeights[i] = reader.ReadSingle();
                    cellMaxHeights[i] = reader.ReadSingle();
                }

                quadrantLtexIds = new ulong[cellsX * 2 * cellsY * 2];
                for(int i = 0; i < quadrantLtexIds.Length; i++) quadrantLtexIds[i] = reader.ReadUInt64();

                gcvrIds = new uint[reader.ReadInt32()];
                for (int i = 0; i < gcvrIds.Length; i++) gcvrIds[i] = reader.ReadUInt32();

                quadrantGcvrIds = new ulong[cellsX * 2 * cellsY * 2];
                for(int i = 0; i < quadrantGcvrIds.Length; i++) quadrantGcvrIds[i] = reader.ReadUInt64();

                globalHeights = new ushort[cellsX * 8 * cellsY * 8];
                for (int i = 0; i < globalHeights.Length; i++) globalHeights[i] = reader.ReadUInt16();

                globalLtex = new ushort[cellsX * 8 * cellsY * 8];
                for (int i = 0; i < globalLtex.Length; i++) globalLtex[i] = reader.ReadUInt16();

                globalVclr = new ushort[cellsX * 8 * cellsY * 8];
                for (int i = 0; i < globalVclr.Length; i++) globalVclr[i] = reader.ReadUInt16();

                blockOffsets = new uint[4][];
                blockSizes = new int[4][];

                blockOffsets[3] = new uint[blockCellsX[3] * blockCellsY[3] * 2]; blockSizes[3] = new int[blockOffsets[3].Length];
                for(int i = 0; i < blockOffsets[3].Length; i++) {
                    blockOffsets[3][i] = reader.ReadUInt32();
                    blockSizes[3][i] = reader.ReadInt32();
                }

                blockOffsets[2] = new uint[blockCellsX[2] * blockCellsY[2] * 2]; blockSizes[2] = new int[blockOffsets[2].Length];
                for (int i = 0; i < blockOffsets[2].Length; i++) {
                    blockOffsets[2][i] = reader.ReadUInt32();
                    blockSizes[2][i] = reader.ReadInt32();
                }

                blockOffsets[1] = new uint[blockCellsX[1] * blockCellsY[1]]; blockSizes[1] = new int[blockOffsets[1].Length];
                for (int i = 0; i < blockOffsets[1].Length; i++) {
                    blockOffsets[1][i] = reader.ReadUInt32();
                    blockSizes[1][i] = reader.ReadInt32();
                }


                blockOffsets[0] = new uint[cellsX * cellsY]; blockSizes[0] = new int[blockOffsets[0].Length];
                for (int i = 0; i < blockOffsets[0].Length; i++) {
                    blockOffsets[0][i] = reader.ReadUInt32();
                    blockSizes[0][i] = reader.ReadInt32();
                }

                gcvrOffsets = new uint[cellsX * cellsY]; gcvrSizes = new int[gcvrOffsets.Length];
                for (int i = 0; i < gcvrOffsets.Length; i++) {
                    gcvrOffsets[i] = reader.ReadUInt32();
                    gcvrSizes[i] = reader.ReadInt32();
                }

                zlibOffset = reader.BaseStream.Position;
            }
        }
        
        public enum TerrainMode {
            height,
            ltex,
            color
        }

        public ushort[] GetDataForLevel(ushort[] prev, int level, TerrainMode mode = TerrainMode.height) {
            Console.WriteLine($"Loading level {level}");
            int cellSize = cellSizes[level];
            int blockCellCount = blockCellCounts[level];
            int blockSize = blockCellCount * cellSize;

            int shortsOffset = mode == TerrainMode.ltex ? blockSize * blockSize * 3 / 4 : 0;
            int uncompressedBlockSize = mode == TerrainMode.color  ? blockSize * blockSize * 2 : blockSize * blockSize * 3;


            int fullWidth = cellsX * cellSize;
            int fullHeight = cellsY * cellSize;

            ushort[] fullHeights = new ushort[fullWidth * fullHeight];
            //take the top-left of each 2x2 block from prev
            for (int y = 0; y < fullHeight; y += 2) {
                for (int x = 0; x < fullWidth; x += 2) {
                    fullHeights[x + y * fullWidth] = prev[x / 2 + y / 2 * fullWidth / 2];
                }
            }

            using (BinaryReader reader = new BinaryReader(File.OpenRead(path))) {
                using Decompressor decompressor = new ZlibDecompressor();
                for (int cellY = 0; cellY < cellsY; cellY += blockCellCount) {
                    for (int cellX = 0; cellX < cellsX; cellX += blockCellCount) {                    
                        int zlibBlockIndex = (cellX / blockCellCount + cellY / blockCellCount * blockCellsX[level]);
                        if (level > 1) {
                            zlibBlockIndex *= 2;
                            if (mode == TerrainMode.color) zlibBlockIndex += 1;
                        }

                        reader.BaseStream.Seek(zlibOffset + blockOffsets[level][zlibBlockIndex], SeekOrigin.Begin);
                        var status = decompressor.Decompress(reader.ReadBytes(blockSizes[level][zlibBlockIndex]), uncompressedBlockSize, out var decompressed);
                        if (status != OperationStatus.Done) {
                            Console.WriteLine(status);
                            continue;
                        }
                        Span<ushort> blockShorts = MemoryMarshal.Cast<byte, ushort>(decompressed.Memory.Span);
                        int blockWidth = cellX + blockCellCount > cellsX ? (cellsX % blockCellCount) * cellSize : blockSize;
                        int blockHeight = cellY + blockCellCount > cellsY ? (cellsY % blockCellCount) * cellSize : blockSize;


                        int i = 0;
                        for (int y = 0; y < blockHeight; y += 2) {
                            for (int x = 0; x < blockWidth; x += 2) {
                                fullHeights[cellX * cellSize + cellY * fullWidth * cellSize + x + 1 + y * fullWidth] = blockShorts[x / 2 + y / 2 * 192 + shortsOffset];
                            }
                            for (int x = 0; x < blockWidth; x += 2) {
                                fullHeights[cellX * cellSize + cellY * fullWidth * cellSize + x + y * fullWidth + fullWidth] = blockShorts[x + y / 2 * 192 + 64 + shortsOffset];
                                fullHeights[cellX * cellSize + cellY * fullWidth * cellSize + x + 1 + y * fullWidth + fullWidth] = blockShorts[x + 1 + y / 2 * 192 + 64 + shortsOffset];
                            }
                        }
                    }
                }
            }
            return fullHeights;
        }

        public void Export(int level, TerrainMode mode = TerrainMode.height) {

            if(level < 2 && mode == TerrainMode.color) {
                Console.WriteLine("Color only available for lod 2 and up");
                level = 2;
            }

            ushort[] heights = mode switch {
                TerrainMode.height => globalHeights,
                TerrainMode.ltex => globalLtex,
                TerrainMode.color => globalVclr
            };
            for(int i = 3; i >= level; i--) {
                heights = GetDataForLevel(heights, i, mode);
            }

            

            if (mode == TerrainMode.color) {
                byte[] colors = new byte[heights.Length * 3];

                for (int i = 0; i < heights.Length; i++) {
                    colors[i * 3] = (byte)((heights[i] & 0b11111) * 8);
                    colors[i * 3 + 1] = (byte)(((heights[i] >> 5) & 0b11111) * 8);
                    colors[i * 3 + 2] = (byte)(((heights[i] >> 10) & 0b11111) * 8);
                }
                MagickImage image = new MagickImage();
                image.Read(colors, new MagickReadSettings() { Width = cellsX * cellSizes[level], Height = cellsY * cellSizes[level], Depth = 8, Format = MagickFormat.Bgr });
                string filename = $"lod{level}_vcol.png";
                Console.WriteLine(filename);
                image.Quality = 0;
                image.Write(filename);
            } else {
                var span = MemoryMarshal.AsBytes(heights.AsSpan());
                MagickImage image = new MagickImage();
                image.Read(span, new MagickReadSettings() { Width = cellsX * cellSizes[level], Height = cellsY * cellSizes[level], Depth = 16, Format = MagickFormat.Gray });
                string filename = mode == TerrainMode.ltex ? $"lod{level}_ltex.png" : $"lod{level}_heights.png";
                Console.WriteLine(filename);
                image.Quality = 0;
                image.Write(filename);
            }
        }

        public void WriteLod3Height(int index) {
            using (BinaryReader reader = new BinaryReader(File.OpenRead(path))) {
                reader.BaseStream.Seek(zlibOffset + blockOffsets[3][index * 2], SeekOrigin.Begin);
                using(Decompressor decompressor = new ZlibDecompressor()) {
                    var status = decompressor.Decompress(reader.ReadBytes(blockSizes[3][index * 2]), 49152, out var decompressed);
                    //if (status != OperationStatus.Done) {
                    //    Console.WriteLine(status);
                    //} else {
                    //    File.WriteAllBytes("test.data", decompressed.Memory.ToArray());
                    //}

                    using(BinaryReader heightReader = new BinaryReader(new MemoryStream(decompressed.Memory.ToArray()))) {
                        ushort[] heights = new ushort[128 * 128];
                        for(int y = 0; y < 128; y+=2) {
                            for(int x = 0; x < 128; x+=2) {
                                heights[x + y * 128 + 1] = heightReader.ReadUInt16();
                                heights[x + y * 128] = heights[x + y * 128 + 1]; //test
                            }
                            for (int x = 0; x < 128; x += 2) {
                                heights[x + y * 128 + 128] = heightReader.ReadUInt16();
                                heights[x + y * 128 + 129] = heightReader.ReadUInt16();
                            }

                        }

                        var span = MemoryMarshal.AsBytes(heights.AsSpan());
                        MagickImage image = new MagickImage();
                        image.Read(span, new MagickReadSettings() { Width = 128, Height = 128, Depth = 16, Format = MagickFormat.Gray });
                        string filename = $"lod3height_{index}.png";
                        Console.WriteLine(filename);
                        image.Write(filename);
                    }
                }

            }
        }

        public void WriteLod4Ltex() {
            MagickImage heights = new MagickImage();
            var span = MemoryMarshal.AsBytes(globalLtex.AsSpan());
            heights.Read(span, new MagickReadSettings() { Width = cellsX * 8, Height = cellsY * 8, Depth = 16, Format = MagickFormat.Gray });
            heights.Write("ltex.png");
        }


        public void WriteLod4Vcol() {
            byte[] terrainColors = new byte[cellsX * 8 * cellsY * 8 * 3];
            for(int i = 0; i < globalVclr.Length; i++) {
                terrainColors[i * 3] = (byte)((globalVclr[i] & 0b11111) * 8);
                terrainColors[i * 3 + 1] = (byte)(((globalVclr[i] >> 5) & 0b11111) * 8);
                terrainColors[i * 3 + 2] = (byte)(((globalVclr[i] >> 10) & 0b11111) * 8);
            }
            MagickImage heights = new MagickImage();
            heights.Read(terrainColors, new MagickReadSettings() { Width = cellsX * 8, Height = cellsY * 8, Depth = 8, Format = MagickFormat.Bgr });
            heights.Write("vcol.png");
        }

    }
}