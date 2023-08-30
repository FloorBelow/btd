using System;
using System.Runtime.InteropServices;
using LibDeflate;
using ImageMagick;
using System.Buffers;
using System.Reflection;

namespace BTD_Tests {
    internal class Program {
        static void Main(string[] args) {
            Btd btd = new Btd(@"F:\SteamLibrary\steamapps\common\Fallout76\Data\Terrain\Appalachia.btd");
            Console.WriteLine($"{btd.cellsX}x{btd.cellsY}");
            //btd.WriteLod4Ltex();
            //btd.WriteLod4Vcol();

            btd.WriteLod3Full();

            //for(int i = 0; i < btd.lod3Offsets.Length / 2; i++) btd.WriteLod3Height(i);
            Console.WriteLine("DONE");
        }
    }


    class Btd {

        public string path;
        public long zlibOffset;

        //helpful helper variaables
        public int cellsX;
        public int cellsY;
        public int lod3CellsX;
        public int lod3CellsY;
        public int lod2CellsX;
        public int lod2CellsY;
        public int lod1CellsX;
        public int lod1CellsY;


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

        public ushort[] lod4Heights;
        public ushort[] lod4Ltex;
        public ushort[] lod4Vcol;

        public uint[] lod3Offsets;
        public int[] lod3Sizes;
        public uint[] lod2Offsets;
        public int[] lod2Sizes;
        public uint[] lod1Offsets;
        public int[] lod1Sizes;
        public uint[] lod0Offsets;
        public int[] lod0Sizes;
        public uint[] gcvrOffsets;
        public int[] gcvrSizes;

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
                lod3CellsX = (cellsX + 7) / 8;
                lod3CellsY = (cellsY + 7) / 8;
                lod2CellsX = (cellsX + 3) / 4;
                lod2CellsY = (cellsY + 3) / 4;
                lod1CellsX = (cellsX + 1) / 2;
                lod1CellsY = (cellsY + 1) / 2;

                ltex = new uint[reader.ReadInt32()];
                for(int i =  0; i < ltex.Length; i++) { ltex[i] = reader.ReadUInt32(); }

                cellMinHeights = new float[cellsX * cellsY];
                cellMaxHeights = new float[cellsY * cellsX];
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

                lod4Heights = new ushort[cellsX * 8 * cellsY * 8];
                for (int i = 0; i < lod4Heights.Length; i++) lod4Heights[i] = reader.ReadUInt16();

                lod4Ltex = new ushort[cellsX * 8 * cellsY * 8];
                for (int i = 0; i < lod4Ltex.Length; i++) lod4Ltex[i] = reader.ReadUInt16();

                lod4Vcol = new ushort[cellsX * 8 * cellsY * 8];
                for (int i = 0; i < lod4Vcol.Length; i++) lod4Vcol[i] = reader.ReadUInt16();

                lod3Offsets = new uint[lod3CellsX * lod3CellsY * 2]; lod3Sizes = new int[lod3Offsets.Length];
                for(int i = 0; i < lod3Offsets.Length; i++) {
                    lod3Offsets[i] = reader.ReadUInt32();
                    lod3Sizes[i] = reader.ReadInt32();
                }

                lod2Offsets = new uint[lod2CellsX * lod2CellsY * 2]; lod2Sizes = new int[lod2Offsets.Length];
                for (int i = 0; i < lod2Offsets.Length; i++) {
                    lod2Offsets[i] = reader.ReadUInt32();
                    lod2Sizes[i] = reader.ReadInt32();
                }

                lod1Offsets = new uint[lod1CellsX * lod1CellsY]; lod1Sizes = new int[lod1Offsets.Length];
                for (int i = 0; i < lod1Offsets.Length; i++) {
                    lod1Offsets[i] = reader.ReadUInt32();
                    lod1Sizes[i] = reader.ReadInt32();
                }


                lod0Offsets = new uint[cellsX * cellsY]; lod0Sizes = new int[lod0Offsets.Length];
                for (int i = 0; i < lod0Offsets.Length; i++) {
                    lod0Offsets[i] = reader.ReadUInt32();
                    lod0Sizes[i] = reader.ReadInt32();
                }

                gcvrOffsets = new uint[cellsX * cellsY]; gcvrSizes = new int[gcvrOffsets.Length];
                for (int i = 0; i < gcvrOffsets.Length; i++) {
                    gcvrOffsets[i] = reader.ReadUInt32();
                    gcvrSizes[i] = reader.ReadInt32();
                }

                zlibOffset = reader.BaseStream.Position;
            }
        }

        public void WriteLod3Full() {
            int blockCellCount = 8;
            int cellSize = 16;
            int blockSize = blockCellCount * cellSize;

            int fullWidth = cellsX * cellSize;
            int fullHeight = cellsY * cellSize;
            ushort[] fullHeights = new ushort[fullWidth * fullHeight];
            for(int y = 0; y < fullHeight; y+=2) {
                for(int x = 0; x < fullWidth; x+=2) {
                    fullHeights[x + y * fullWidth] = lod4Heights[x / 2 + y / 2 * fullWidth / 2];
                }
            }

            using (BinaryReader reader = new BinaryReader(File.OpenRead(path))) {
                using Decompressor decompressor = new ZlibDecompressor();
                for(int cellY = 0; cellY < cellsY; cellY += blockCellCount) {
                    for (int cellX = 0; cellX < cellsX; cellX += blockCellCount) {
                        int zlibBlockIndex = (cellX / blockCellCount + cellY / blockCellCount * lod3CellsX) * 2;

                        reader.BaseStream.Seek(zlibOffset + lod3Offsets[zlibBlockIndex], SeekOrigin.Begin);
                        var status = decompressor.Decompress(reader.ReadBytes(lod3Sizes[zlibBlockIndex]), blockSize * blockSize * 3, out var decompressed);
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


            var span = MemoryMarshal.AsBytes(fullHeights.AsSpan());
            MagickImage image = new MagickImage();
            image.Read(span, new MagickReadSettings() { Width = fullWidth, Height = fullHeight, Depth = 16, Format = MagickFormat.Gray });
            string filename = $"lod3heights.png";
            Console.WriteLine(filename);
            image.Write(filename);


        }

        public void WriteLod3Height(int index) {
            using (BinaryReader reader = new BinaryReader(File.OpenRead(path))) {
                //reader.BaseStream.Seek(zlibOffset + lod3Offsets[0], SeekOrigin.Begin);
                //File.WriteAllBytes("test.data", reader.ReadBytes(lod3Sizes[0]));

                //return;
                
                reader.BaseStream.Seek(zlibOffset + lod3Offsets[index * 2], SeekOrigin.Begin);
                using(Decompressor decompressor = new ZlibDecompressor()) {
                    var status = decompressor.Decompress(reader.ReadBytes(lod3Sizes[index * 2]), 49152, out var decompressed);
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
            var span = MemoryMarshal.AsBytes(lod4Heights.AsSpan());
            heights.Read(span, new MagickReadSettings() { Width = cellsX * 8, Height = cellsY * 8, Depth = 16, Format = MagickFormat.Gray });
            heights.Write("test.png");
        }

        public void WriteLod4Ltex() {
            MagickImage heights = new MagickImage();
            var span = MemoryMarshal.AsBytes(lod4Ltex.AsSpan());
            heights.Read(span, new MagickReadSettings() { Width = cellsX * 8, Height = cellsY * 8, Depth = 16, Format = MagickFormat.Gray });
            heights.Write("ltex.png");
        }


        public void WriteLod4Vcol() {
            byte[] terrainColors = new byte[cellsX * 8 * cellsY * 8 * 3];
            for(int i = 0; i < lod4Vcol.Length; i++) {
                terrainColors[i * 3] = (byte)((lod4Vcol[i] & 0b11111) * 8);
                terrainColors[i * 3 + 1] = (byte)(((lod4Vcol[i] >> 5) & 0b11111) * 8);
                terrainColors[i * 3 + 2] = (byte)(((lod4Vcol[i] >> 10) & 0b11111) * 8);
            }
            MagickImage heights = new MagickImage();
            heights.Read(terrainColors, new MagickReadSettings() { Width = cellsX * 8, Height = cellsY * 8, Depth = 8, Format = MagickFormat.Bgr });
            heights.Write("vcol.png");
        }

    }
}