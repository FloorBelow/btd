using System;
using System.Runtime.InteropServices;
using LibDeflate;
using ImageMagick;

namespace BTD_Tests {
    internal class Program {
        static void Main(string[] args) {
            Btd btd = new Btd(@"F:\SteamLibrary\steamapps\common\Fallout76\Data\Terrain\Appalachia.btd");
            Console.WriteLine($"{btd.cellsX}x{btd.cellsY}");
            //btd.WriteLod4Ltex();
            btd.WriteLod4Vcol();
            Console.WriteLine("DONE");
        }
    }


    class Btd {

        public string path;
        public long zlibOffset;

        public int cellsX;
        public int cellsY;


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
        public uint[] lod3Sizes;
        public uint[] lod2Offsets;
        public uint[] lod2Sizes;
        public uint[] lod1Offsets;
        public uint[] lod1Sizes;
        public uint[] lod0Offsets;
        public uint[] lod0Sizes;
        public uint[] gcvrOffsets;
        public uint[] gcvrSizes;

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

                cellsX = maxX - minX + 1; //note this only works if max and min are different sign?
                cellsY = maxY - minY + 1;

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

                lod3Offsets = new uint[(cellsX + 7) / 8 * (cellsY + 7) / 8 * 2]; lod3Sizes = new uint[lod3Offsets.Length];
                lod2Offsets = new uint[(cellsX + 3) / 4 * (cellsY + 3) / 4 * 2]; lod2Sizes = new uint[lod2Offsets.Length];
                lod1Offsets = new uint[(cellsX + 1) / 2 * (cellsY + 1) / 2]; lod2Sizes = new uint[lod1Offsets.Length];
                lod0Offsets = new uint[cellsX * cellsY]; lod0Sizes = new uint[lod0Offsets.Length];
                gcvrOffsets = new uint[cellsX * cellsY]; gcvrSizes = new uint[gcvrOffsets.Length];

                zlibOffset = reader.BaseStream.Position;
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