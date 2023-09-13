using ImageMagick;
using LibDeflate;
using System.Buffers;
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

    public void Export(int level, TerrainMode mode = TerrainMode.height, string name = "lod") {
        int cellSize = cellSizes[level];
        int blockCellCount = blockCellCounts[level];
        int blockSize = blockCellCount * cellSize;

        int shortsOffset = mode == TerrainMode.ltex ? blockSize * blockSize * 3 / 4 : 0;
        


        int fullWidth = cellsX * cellSize;
        int fullHeight = cellsY * cellSize;

        ushort[] fullHeights = new ushort[fullWidth * fullHeight];


        using (BinaryReader reader = new BinaryReader(File.OpenRead(path))) {
            using Decompressor decompressor = new ZlibDecompressor();
            for (int cellY = 0; cellY < cellsY; cellY += blockCellCount) {
                for (int cellX = 0; cellX < cellsX; cellX += blockCellCount) {
                    int zlibBlockIndex = (cellX / blockCellCount + cellY / blockCellCount * blockCellsX[level]);
                    reader.BaseStream.Seek(zlibOffset + blockOffsets[level][zlibBlockIndex], SeekOrigin.Begin);
                    var status = decompressor.Decompress(reader.ReadBytes((int)blockSizes[level][zlibBlockIndex]), uncompressedBlockSize, out var decompressed);
                    if (status != OperationStatus.Done) {
                        Console.WriteLine(status);
                        continue;
                    }
                    Span<ushort> blockShorts = MemoryMarshal.Cast<byte, ushort>(decompressed.Memory.Span);
                    int blockWidth = cellX + blockCellCount > cellsX ? (cellsX % blockCellCount) * cellSize : blockSize;
                    int blockHeight = cellY + blockCellCount > cellsY ? (cellsY % blockCellCount) * cellSize : blockSize;

                    for (int y = 0; y < blockHeight; y++) {
                        for (int x = 0; x < blockWidth; x++) {
                            fullHeights[cellX * cellSize + cellY * fullWidth * cellSize + x + y * fullWidth] = blockShorts[x + y * 128 + shortsOffset];
                        }
                    }
                }
            }
        }

        var span = MemoryMarshal.AsBytes(fullHeights.AsSpan());
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

    public void Write(string path) {
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

            using(BinaryReader r = new BinaryReader(File.OpenRead(this.path))) {
                for (int lod = 3; lod >= 0; lod--) {
                    uint[] offsets = blockOffsets[lod];
                    uint[] sizes = blockSizes[lod];
                    for (int i = 0; i < offsets.Length; i++) {
                        newOffsets.Add((uint)(w.BaseStream.Position - newZlibOffset));
                        uint size = sizes[i];
                        newOffsets.Add(size);
                        r.BaseStream.Seek(zlibOffset + offsets[i], SeekOrigin.Begin);
                        w.BaseStream.Write(r.ReadBytes((int)size));
                    }
                }
            }

            w.BaseStream.Seek(newOffsetOffset, SeekOrigin.Begin);
            for(int i = 0; i < newOffsets.Count; i++) w.Write(newOffsets[i]);


        }
    }

}
