using ImageMagick;
using LibDeflate;
using System.Buffers;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;

class BtdStarfield {

    const int uncompressedBlockSize = 128 * 128 * 4;

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

    public ushort[] globalHeights;
    public ushort[] globalLtex;

    public uint[][] blockOffsets;
    public uint[][] blockSizes;


    public long zlibOffset;


    //helpful helper variaables
    public int cellsX;
    public int cellsY;
    public int[] blockCellsX;
    public int[] blockCellsY;

    //static
    static int[] cellSizes = { 128, 64, 32, 16, 8 };
    static int[] blockCellCounts = { 1, 2, 4, 8 };

    public BtdStarfield(string path) {
        using (BinaryReader reader = new BinaryReader(File.OpenRead(path))) {
            this.path = path;
            string magic = new string(reader.ReadChars(4)); if (magic != "BTDB") throw new Exception("MAGIC WRONG " + magic);
            version = reader.ReadInt32();
            minHeight = reader.ReadSingle();
            maxHeight = reader.ReadSingle();
            sizeX = reader.ReadInt32();
            sizeY = reader.ReadInt32();
            minX = reader.ReadInt32();
            minY = reader.ReadInt32();
            maxX = reader.ReadInt32();
            maxY = reader.ReadInt32();

            cellsX = sizeX / 128;
            cellsY = sizeY / 128;
            blockCellsX = new int[] { cellsX, (cellsX + 1) / 2, (cellsX + 3) / 4, (cellsX + 7) / 8 };
            blockCellsY = new int[] { cellsY, (cellsY + 1) / 2, (cellsY + 3) / 4, (cellsY + 7) / 8 };

            ltexIds = new uint[reader.ReadInt32()];
            for (int i = 0; i < ltexIds.Length; i++) { ltexIds[i] = reader.ReadUInt32(); }

            cellMinHeights = new float[cellsX * cellsY];
            cellMaxHeights = new float[cellMinHeights.Length];
            for (int i = 0; i < cellMinHeights.Length; i++) {
                cellMinHeights[i] = reader.ReadSingle();
                cellMaxHeights[i] = reader.ReadSingle();
            }

            quadrantLtexIds = new ulong[cellsX * 2 * cellsY * 2];
            for (int i = 0; i < quadrantLtexIds.Length; i++) quadrantLtexIds[i] = reader.ReadUInt64();

            globalHeights = new ushort[cellsX * 8 * cellsY * 8];
            for (int i = 0; i < globalHeights.Length; i++) globalHeights[i] = reader.ReadUInt16();

            globalLtex = new ushort[cellsX * 8 * cellsY * 8];
            for (int i = 0; i < globalLtex.Length; i++) globalLtex[i] = reader.ReadUInt16();

            blockOffsets = new uint[4][];
            blockSizes = new uint[4][];

            blockOffsets[3] = new uint[blockCellsX[3] * blockCellsY[3]]; blockSizes[3] = new uint[blockOffsets[3].Length];
            for (int i = 0; i < blockOffsets[3].Length; i++) {
                blockOffsets[3][i] = reader.ReadUInt32();
                blockSizes[3][i] = reader.ReadUInt32();
            }

            blockOffsets[2] = new uint[blockCellsX[2] * blockCellsY[2]]; blockSizes[2] = new uint[blockOffsets[2].Length];
            for (int i = 0; i < blockOffsets[2].Length; i++) {
                blockOffsets[2][i] = reader.ReadUInt32();
                blockSizes[2][i] = reader.ReadUInt32();
            }

            blockOffsets[1] = new uint[blockCellsX[1] * blockCellsY[1]]; blockSizes[1] = new uint[blockOffsets[1].Length];
            for (int i = 0; i < blockOffsets[1].Length; i++) {
                blockOffsets[1][i] = reader.ReadUInt32();
                blockSizes[1][i] = reader.ReadUInt32();
            }


            blockOffsets[0] = new uint[cellsX * cellsY]; blockSizes[0] = new uint[blockOffsets[0].Length];
            for (int i = 0; i < blockOffsets[0].Length; i++) {
                blockOffsets[0][i] = reader.ReadUInt32();
                blockSizes[0][i] = reader.ReadUInt32();
            }

            zlibOffset = reader.BaseStream.Position;
        }
    }

    public enum TerrainMode {
        height,
        ltex
    }

    IMemoryOwner<byte> DecompressBlock(BinaryReader reader, Decompressor decompressor, int level, int block) {
        reader.BaseStream.Seek(zlibOffset + blockOffsets[level][block], SeekOrigin.Begin);
        var status = decompressor.Decompress(reader.ReadBytes((int)blockSizes[level][block]), uncompressedBlockSize, out var decompressed);
        if (status != OperationStatus.Done) {
            Console.WriteLine(status);
            return null;
        }
        return decompressed;
    }

    public ushort[] GetFullData(int level, TerrainMode mode = TerrainMode.height) {
        int cellSize = cellSizes[level];
        int blockCellCount = blockCellCounts[level];
        int blockSize = blockCellCount * cellSize;

        int shortsOffset = mode == TerrainMode.ltex ? blockSize * blockSize: 0;



        int fullWidth = cellsX * cellSize;
        int fullHeight = cellsY * cellSize;

        ushort[] fullData = new ushort[fullWidth * fullHeight];


        using (BinaryReader reader = new BinaryReader(File.OpenRead(path))) {
            using Decompressor decompressor = new ZlibDecompressor();
            for (int cellY = 0; cellY < cellsY; cellY += blockCellCount) {
                for (int cellX = 0; cellX < cellsX; cellX += blockCellCount) {
                    int zlibBlockIndex = (cellX / blockCellCount + cellY / blockCellCount * blockCellsX[level]);
                    var decompressed = DecompressBlock(reader, decompressor, level, zlibBlockIndex);
                    /*
                    reader.BaseStream.Seek(zlibOffset + blockOffsets[level][zlibBlockIndex], SeekOrigin.Begin);
                    var status = decompressor.Decompress(reader.ReadBytes((int)blockSizes[level][zlibBlockIndex]), uncompressedBlockSize, out var decompressed);
                    if (status != OperationStatus.Done) {
                        Console.WriteLine(status);
                        continue;
                    }
                    */
                    Span<ushort> blockShorts = MemoryMarshal.Cast<byte, ushort>(decompressed.Memory.Span);
                    int blockWidth = cellX + blockCellCount > cellsX ? (cellsX % blockCellCount) * cellSize : blockSize;
                    int blockHeight = cellY + blockCellCount > cellsY ? (cellsY % blockCellCount) * cellSize : blockSize;

                    for (int y = 0; y < blockHeight; y++) {
                        for (int x = 0; x < blockWidth; x++) {
                            fullData[cellX * cellSize + cellY * fullWidth * cellSize + x + y * fullWidth] = blockShorts[x + y * 128 + shortsOffset];
                        }
                    }
                }
            }
        }
        return fullData;

    }

    public void Export(int level, TerrainMode mode = TerrainMode.height, string name = "lod") {
        var data = GetFullData(level, mode);

        var span = MemoryMarshal.AsBytes(data.AsSpan());
        MagickImage image = new MagickImage();
        image.Read(span, new MagickReadSettings() { Width = cellsX * cellSizes[level], Height = cellsY * cellSizes[level], Depth = 16, Format = MagickFormat.Gray });
        string filename = mode == TerrainMode.ltex ? $"{name}_ltex_{level}.png" : $"{name}_height_{level}.png";
        Console.WriteLine(filename);
        image.Quality = 0;
        image.Write(filename);
    }

    public void DumpZlibBlocks() {
        using (BinaryReader reader = new BinaryReader(File.OpenRead(path))) {
            using (Decompressor decompressor = new ZlibDecompressor()) {
                for(int level = 3; level >= 0; level--) {
                    for (int i = 0; i < blockOffsets[level].Length; i++) {
                        reader.BaseStream.Seek(zlibOffset + blockOffsets[level][i], SeekOrigin.Begin);
                        int testSize = 128 * 128 * 4;
                        var status = decompressor.Decompress(reader.ReadBytes((int)blockSizes[level][i]), testSize, out var decompressed);
                        if (status != OperationStatus.Done) {
                            Console.WriteLine(status);
                        } else {
                            MagickImage image = new MagickImage();
                            image.Read(decompressed.Memory.Span, new MagickReadSettings() { Width = 128, Height = 128, Depth = 16, Format = MagickFormat.Gray });
                            string filename = $"test_L{level}_{i}.png";
                            Console.WriteLine(filename);
                            image.Write(filename);
                        }
                    }
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


    ushort[] DownSize(ushort[] input, int width, int height) {
        ushort[] output = new ushort[width * height];
        for(int y = 0; y < height; y++) {
            for(int x = 0; x < width; x++) {
                output[x + y * width] = input[x * 2 + y * 4 * width];
            }
        }
        return output;
    }


    public void Write(string path, ushort[] fullHeights = null, float scale = 1.0f) {

        int[] fullSizesX = new int[5]; fullSizesX[0] = sizeX;
        int[] fullSizesY = new int[5]; fullSizesY[0] = sizeY;
        for(int i = 1; i <= 4; i++) {
            fullSizesX[i] = fullSizesX[i - 1] / 2;
            fullSizesY[i] = fullSizesY[i - 1] / 2;
        }
        

        ushort[][] heights = new ushort[5][];
        heights[0] = fullHeights is null ? GetFullData(0, TerrainMode.height) : fullHeights;
        for(int i = 0; i < heights[0].Length; i++) {
            heights[0][i] = (ushort)(heights[0][i] * scale);
        }

        ushort[][] ltex = new ushort[5][];
        ltex[0] = GetFullData(0, TerrainMode.ltex);

        for (int i = 1; i <= 4; i++) {
            heights[i] = DownSize(heights[i - 1], fullSizesX[i], fullSizesY[i]);
            ltex[i] = DownSize(ltex[i - 1], fullSizesX[i], fullSizesY[i]);
        }

        /*
        for(int i = 0; i <= 4; i++) {
            MagickImage testImage = new MagickImage();
            var bytes = MemoryMarshal.AsBytes(heights[i].AsSpan());
            testImage.Read(bytes, new MagickReadSettings() { Width = sizeX >> i, Height = sizeY >> i, Depth = 16, Format = MagickFormat.Gray });
            testImage.Quality = 0;
            testImage.Write($"TEST_height_{i}.png");

            MagickImage testImage2 = new MagickImage();
            var bytes2 = MemoryMarshal.AsBytes(ltex[i].AsSpan());
            testImage2.Read(bytes2, new MagickReadSettings() { Width = sizeX >> i, Height = sizeY >> i, Depth = 16, Format = MagickFormat.Gray });
            testImage2.Quality = 0;
            testImage2.Write($"TEST_ltex_{i}.png");
        }
        */

        using (BinaryWriter w = new BinaryWriter(File.Open(path, FileMode.Create))) {
            w.Write(1111774274); //magic
            w.Write(version);
            w.Write(minHeight); w.Write(maxHeight);
            w.Write(sizeX); w.Write(sizeY);
            w.Write(minX); w.Write(minY); w.Write(maxX); w.Write(maxX); //these are all zero anyway, right?
            w.Write(ltexIds.Length); for (int i = 0; i < ltexIds.Length; i++) { w.Write(ltexIds[i]); }
            for (int i = 0; i < cellsX * cellsY; i++) {
                w.Write(cellMinHeights[i]);
                w.Write(cellMaxHeights[i]);
            }
            for (int i = 0; i < cellsX * cellsY * 4; i++) w.Write(quadrantLtexIds[i]);
            for (int i = 0; i < heights[4].Length; i++) w.Write(heights[4][i]);
            for (int i = 0; i < ltex[4].Length; i++) w.Write(ltex[4][i]);

            long newOffsetOffset = w.BaseStream.Position;


            int offsetCount = blockOffsets[3].Length + blockOffsets[2].Length + blockOffsets[1].Length + blockOffsets[0].Length;
            List<uint> newOffsets = new List<uint>(offsetCount * 2);
            w.Write(new byte[offsetCount * 8]);


            long newZlibOffset = w.BaseStream.Position;



            using (Compressor compressor = new ZlibCompressor(5)) {
                for (int lod = 3; lod >= 0; lod--) {

                    for(int y = 0; y < blockCellsY[lod]; y++) {
                        for(int x = 0; x < blockCellsX[lod]; x++) {
                            ushort[] block = GetBlock(fullSizesX[lod], fullSizesY[lod], heights[lod], ltex[lod], lod, x, y);

                            var bytes = MemoryMarshal.AsBytes(block.AsSpan());
                            var compressed = compressor.Compress(bytes);

                            newOffsets.Add((uint)(w.BaseStream.Position - newZlibOffset));
                            newOffsets.Add((uint)compressed.Memory.Length);
                            w.Write(compressed.Memory.Span);

                            /*
                            MagickImage testImage = new MagickImage();
                            var bytes = MemoryMarshal.AsBytes(block.AsSpan());
                            testImage.Read(bytes, new MagickReadSettings() { Width = 128, Height = 128, Depth = 16, Format = MagickFormat.Gray });
                            testImage.Quality = 0;
                            testImage.Write($"TEST_heightlod_{lod}_{x}_{y}.png");
                            */


                        }
                    }

                    /*

                    for (int i = 0; i < blockOffsets[lod].Length; i++) {

                        newOffsets.Add((uint)(w.BaseStream.Position - newZlibOffset));

                        r.BaseStream.Seek(zlibOffset + offsets[i], SeekOrigin.Begin);
                        var decompressed = DecompressBlock(r, decompressor, lod, i);

                        var uintSpan = MemoryMarshal.Cast<byte, ushort>(decompressed.Memory.Span);
                        for (int b = 0; b < 128 * 128; b++) uintSpan[b] = 0;

                        var recompressed = compressor.Compress(decompressed.Memory.Span);
                        uint size = (uint)recompressed.Memory.Length;


                        newOffsets.Add(size);
                        w.BaseStream.Write(recompressed.Memory.Span);
                    }
                    */
                }
            }
                
            

            w.BaseStream.Seek(newOffsetOffset, SeekOrigin.Begin);
            for (int i = 0; i < newOffsets.Count; i++) w.Write(newOffsets[i]);


        }
    }

    ushort[] GetBlock(int fullSizeX, int fullSizeY, ushort[] heights, ushort[] ltex, int level, int blockX, int blockY) {
        ushort[] block = new ushort[128 * 128 * 2];

        int startX = blockX * 128; int startY = blockY * 128;
        int endX = Math.Min(128, fullSizeX - startX);
        int endY = Math.Min(128, fullSizeY - startY);
        for(int y = 0; y < endY; y++) {
            for(int x = 0; x < endX; x++) {
                block[x + y * 128] = heights[startX + x + (startY + y) * fullSizeX];
                block[x + y * 128 + 128 * 128] = ltex[startX + x + (startY + y) * fullSizeX];

            }
        }
        return block;
    }


    public void WriteOld(string path) {
        using(BinaryWriter w = new BinaryWriter(File.Open(path, FileMode.Create))) {
            w.Write(1111774274); //magic
            w.Write(version);
            w.Write(minHeight); w.Write(maxHeight);
            w.Write(sizeX); w.Write(sizeY);
            w.Write(minX); w.Write(minY); w.Write(maxX); w.Write(maxX); //these are all zero anyway, right?
            w.Write(ltexIds.Length); for (int i = 0; i < ltexIds.Length; i++) { w.Write(ltexIds[i]); }
            for(int i = 0; i < cellsX * cellsY; i++) {
                w.Write(cellMinHeights[i]);
                w.Write(cellMaxHeights[i]);
            }
            for (int i = 0; i < cellsX * cellsY * 4; i++) w.Write(quadrantLtexIds[i]);
            for (int i = 0; i < globalHeights.Length; i++) w.Write(globalHeights[i]);
            for (int i = 0; i < globalLtex.Length; i++) w.Write(globalLtex[i]);

            long newOffsetOffset = w.BaseStream.Position;


            int offsetCount = blockOffsets[3].Length + blockOffsets[2].Length + blockOffsets[1].Length + blockOffsets[0].Length;
            List<uint> newOffsets = new List<uint>(offsetCount * 2);
            w.Write(new byte[offsetCount * 8]);


            long newZlibOffset = w.BaseStream.Position;

            using (Decompressor decompressor = new ZlibDecompressor()) {
                using(Compressor compressor = new ZlibCompressor(5)) {
                    using (BinaryReader r = new BinaryReader(File.OpenRead(this.path))) {
                        for (int lod = 3; lod >= 0; lod--) {
                            uint[] offsets = blockOffsets[lod];
                            uint[] sizes = blockSizes[lod];
                            for (int i = 0; i < offsets.Length; i++) {

                                newOffsets.Add((uint)(w.BaseStream.Position - newZlibOffset));

                                r.BaseStream.Seek(zlibOffset + offsets[i], SeekOrigin.Begin);
                                var decompressed = DecompressBlock(r, decompressor, lod, i);
                                
                                var uintSpan = MemoryMarshal.Cast<byte, ushort>(decompressed.Memory.Span);
                                for (int b = 0; b < 128 * 128; b++) uintSpan[b] = 0;

                                var recompressed = compressor.Compress(decompressed.Memory.Span);
                                uint size = (uint)recompressed.Memory.Length;


                                newOffsets.Add(size);
                                w.BaseStream.Write(recompressed.Memory.Span);
                            }
                        }
                    }
                }
            }

            w.BaseStream.Seek(newOffsetOffset, SeekOrigin.Begin);
            for(int i = 0; i < newOffsets.Count; i++) w.Write(newOffsets[i]);


        }
    }

}
