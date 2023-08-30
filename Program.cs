using System.Runtime.InteropServices;
using LibDeflate;
using ImageMagick;
using System.Buffers;

namespace BTD_Tests {
    internal class Program {
        static void Main(string[] args) {
            Btd btd = new Btd(@"F:\SteamLibrary\steamapps\common\Fallout76\Data\Terrain\Appalachia.btd");
            Console.WriteLine($"{btd.cellsX}x{btd.cellsY}");
            //btd.WriteLod4Ltex();
            //btd.WriteLod4Vcol();

            btd.WriteLod2Full();

            //for(int i = 0; i < btd.lod3Offsets.Length / 2; i++) btd.WriteLod3Height(i);
            Console.WriteLine("DONE");
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

        public uint[] ltex;
        public ulong[] quadrantLtexIds;

        public uint[] gcvr;
        public ulong[] quadrantGcvrIds;

        public ushort[] globalHeights;
        public ushort[] globalLtex;
        public ushort[] globalVcol;

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
        static int[] cellSizes = { 128, 64, 32, 16 };
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

                ltex = new uint[reader.ReadInt32()];
                for(int i =  0; i < ltex.Length; i++) { ltex[i] = reader.ReadUInt32(); }

                cellMinHeights = new float[cellsX * cellsY];
                cellMaxHeights = new float[cellMinHeights.Length];
                for(int i = 0; i < cellMinHeights.Length; i++) {
                    cellMinHeights[i] = reader.ReadSingle();
                    cellMaxHeights[i] = reader.ReadSingle();
                }

                quadrantLtexIds = new ulong[cellsX * 2 * cellsY * 2];
                for(int i = 0; i < quadrantLtexIds.Length; i++) quadrantLtexIds[i] = reader.ReadUInt64();

                gcvr = new uint[reader.ReadInt32()];
                for (int i = 0; i < gcvr.Length; i++) gcvr[i] = reader.ReadUInt32();

                quadrantGcvrIds = new ulong[cellsX * 2 * cellsY * 2];
                for(int i = 0; i < quadrantGcvrIds.Length; i++) quadrantGcvrIds[i] = reader.ReadUInt64();

                globalHeights = new ushort[cellsX * 8 * cellsY * 8];
                for (int i = 0; i < globalHeights.Length; i++) globalHeights[i] = reader.ReadUInt16();

                globalLtex = new ushort[cellsX * 8 * cellsY * 8];
                for (int i = 0; i < globalLtex.Length; i++) globalLtex[i] = reader.ReadUInt16();

                globalVcol = new ushort[cellsX * 8 * cellsY * 8];
                for (int i = 0; i < globalVcol.Length; i++) globalVcol[i] = reader.ReadUInt16();

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

        //public void WriteLod2Full() {
        //    ushort[] lod3Full = 
        //}

        public ushort[] GetFullHeights(ushort[] prev, int level) {
            int cellSize = cellSizes[level];
            int blockCellCount = blockCellCounts[level];
            int blockSize = blockCellCount * cellSize;


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
                        int zlibBlockIndex = (cellX / blockCellCount + cellY / blockCellCount * blockCellsX[level]) * 2;
                        reader.BaseStream.Seek(zlibOffset + blockOffsets[level][zlibBlockIndex], SeekOrigin.Begin);
                        var status = decompressor.Decompress(reader.ReadBytes(blockSizes[level][zlibBlockIndex]), blockSize * blockSize * 3, out var decompressed);
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
                                fullHeights[cellX * cellSize + cellY * fullWidth * cellSize + x + 1 + y * fullWidth] = blockShorts[x / 2 + y / 2 * 192];
                            }
                            for (int x = 0; x < blockWidth; x += 2) {
                                fullHeights[cellX * cellSize + cellY * fullWidth * cellSize + x + y * fullWidth + fullWidth] = blockShorts[x + y / 2 * 192 + 64];
                                fullHeights[cellX * cellSize + cellY * fullWidth * cellSize + x + 1 + y * fullWidth + fullWidth] = blockShorts[x + 1 + y / 2 * 192 + 64];
                            }
                        }
                    }
                }
            }
            return fullHeights;
        }

        public void WriteLod2Full() {
            ushort[] lod3Heights = GetFullHeights(globalHeights, 3);
            ushort[] lod2Heights = GetFullHeights(lod3Heights, 2);

            var span = MemoryMarshal.AsBytes(lod2Heights.AsSpan());
            MagickImage image = new MagickImage();
            image.Read(span, new MagickReadSettings() { Width = cellsX * 32, Height = cellsY * 32, Depth = 16, Format = MagickFormat.Gray });
            string filename = $"lod2heights.png";
            Console.WriteLine(filename);
            image.Quality = 0;
            image.Write(filename);

        }

        public void WriteLod3Full() {

            int blockCellCount = 8;
            int cellSize = 16;
            ushort[] lod3Heights = GetFullHeights(globalHeights, 3);
            

            var span = MemoryMarshal.AsBytes(lod3Heights.AsSpan());
            MagickImage image = new MagickImage();
            image.Read(span, new MagickReadSettings() { Width = cellsX * cellSize, Height = cellsY * cellSize, Depth = 16, Format = MagickFormat.Gray });
            string filename = $"lod3heights.png";
            Console.WriteLine(filename);
            image.Quality = 0;
            image.Write(filename);


        }

        public void WriteLod3Height(int index) {
            using (BinaryReader reader = new BinaryReader(File.OpenRead(path))) {
                //reader.BaseStream.Seek(zlibOffset + lod3Offsets[0], SeekOrigin.Begin);
                //File.WriteAllBytes("test.data", reader.ReadBytes(lod3Sizes[0]));

                //return;
                
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

        public void WriteLod4Heights() {
            MagickImage heights = new MagickImage();
            var span = MemoryMarshal.AsBytes(globalHeights.AsSpan());
            heights.Read(span, new MagickReadSettings() { Width = cellsX * 8, Height = cellsY * 8, Depth = 16, Format = MagickFormat.Gray });
            heights.Write("test.png");
        }

        public void WriteLod4Ltex() {
            MagickImage heights = new MagickImage();
            var span = MemoryMarshal.AsBytes(globalLtex.AsSpan());
            heights.Read(span, new MagickReadSettings() { Width = cellsX * 8, Height = cellsY * 8, Depth = 16, Format = MagickFormat.Gray });
            heights.Write("ltex.png");
        }


        public void WriteLod4Vcol() {
            byte[] terrainColors = new byte[cellsX * 8 * cellsY * 8 * 3];
            for(int i = 0; i < globalVcol.Length; i++) {
                terrainColors[i * 3] = (byte)((globalVcol[i] & 0b11111) * 8);
                terrainColors[i * 3 + 1] = (byte)(((globalVcol[i] >> 5) & 0b11111) * 8);
                terrainColors[i * 3 + 2] = (byte)(((globalVcol[i] >> 10) & 0b11111) * 8);
            }
            MagickImage heights = new MagickImage();
            heights.Read(terrainColors, new MagickReadSettings() { Width = cellsX * 8, Height = cellsY * 8, Depth = 8, Format = MagickFormat.Bgr });
            heights.Write("vcol.png");
        }

    }
}