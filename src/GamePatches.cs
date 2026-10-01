using System;
using System.IO;
using System.Linq;

namespace AtroxLauncher
{
    internal static class GamePatches
    {
        internal const int TerrainCave = 0x63610;
        internal const int ScrollHook = 0x2d473;
        internal const int ScrollCave = 0x2d2b0;

        internal static byte[] BuildScroll(int percent)
        {
            if (!new[] { 5, 10, 20, 40, 60, 80, 100 }.Contains(percent))
                throw new ArgumentOutOfRangeException(nameof(percent));
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(new byte[] { 0x50, 0x51, 0x52, 0xb9 }); // Save scratch registers.
                writer.Write(100);
                foreach (var reg in new byte[] { 0xf0, 0xd8 }) // mov eax, esi / ebx
                {
                    writer.Write(new byte[] { 0x89, reg, 0x6b, 0xc0, (byte)percent, 0x99, 0xf7, 0xf9 });
                    // Signed division treats both directions equally. Keep a nonzero input
                    // at least one pixel so the game's zero-delta acceleration reset cannot stall it.
                    writer.Write(new byte[] { 0x85, 0xc0, 0x75, 12, 0x85, (byte)(reg == 0xf0 ? 0xf6 : 0xdb), 0x74, 8,
                        0x89, reg, 0x99, 0x83, 0xca, 1, 0x89, 0xd0, 0x89, (byte)(reg == 0xf0 ? 0xc6 : 0xc3) });
                }
                writer.Write(new byte[] { 0x5a, 0x59, 0x58, 0x8b, 0x4c, 0x24, 0x18, 0x56, 0xe8 });
                writer.Write(0x40727a - (0x400000 + ScrollCave + (int)stream.Position + 4));
                writer.Write((byte)0xe9);
                writer.Write(0x42d47d - (0x400000 + ScrollCave + (int)stream.Position + 4));
                return stream.ToArray();
            }
        }
        internal static byte[] BuildTileRows()
        {
            // ceil(viewportHeight / 40), using the original signed division sequence.
            var code = new byte[] { 0x83, 0xc1, 39, 0xf7, 0xe9, 0xc1, 0xfa, 4, 0xe9, 0, 0, 0, 0 };
            BitConverter.GetBytes(0x4dc8f8 - (0x400000 + TerrainCave + code.Length)).CopyTo(code, 9);
            return code;
        }

        internal static byte[] BuildTerrainWrap()
        {
            // EAX is source Y. The wrapped part ends at sourceY - (cacheHeight - viewportHeight).
            // The original hardcoded subtraction of 40 only worked for heights divisible by 40.
            var code = new byte[] {
                0x8b, 0x8e, 0x3c, 0xc5, 0x35, 0, 0x6b, 0xc9, 40,
                0x2b, 0x0d, 0x7c, 0x6e, 0xb2, 0, 0x29, 0xc8,
                0x55, 0x57, 0xb9, 0xf0, 0x8c, 0xde, 0, 0xe9, 0, 0, 0, 0 };
            BitConverter.GetBytes(0x4b8e73 - (0x400000 + TerrainCave + 32 + code.Length)).CopyTo(code, 25);
            return code;
        }

        internal static void Apply(BinaryWriter writer, bool highResolution, int scrollPercent)
        {
            var scroll = BuildScroll(scrollPercent);
            // Both input paths converge here, before map boundary checks and camera updates.
            Expect(writer, ScrollHook, new byte[] { 0x8b, 0x4c, 0x24, 0x18, 0x56, 0xe8, 0xfd, 0x9d, 0xfd, 0xff });
            Expect(writer, ScrollCave, Enumerable.Repeat((byte)0xcc, 128).ToArray());
            writer.Seek(ScrollCave, SeekOrigin.Begin); writer.Write(scroll);
            writer.Seek(ScrollHook, SeekOrigin.Begin);
            writer.Write((byte)0xe9); writer.Write(ScrollCave - ScrollHook - 5);
            writer.Write(Enumerable.Repeat((byte)0x90, 5).ToArray());
            if (!highResolution) return;
            // Render the world to the bottom; the centered HUD draws over it afterwards.
            // Remove the old black side fills entirely, retaining the game's normal UI/cursor pass.
            Expect(writer, 0xdc87d, new byte[] { 0xb0, 3, 0, 0 });
            writer.Seek(0xdc87d, SeekOrigin.Begin); writer.Write(1024);
            Expect(writer, TerrainCave, Enumerable.Repeat((byte)0xcc, 176).ToArray());
            Expect(writer, 0xdc8f3, new byte[] { 0xf7, 0xe9, 0xc1, 0xfa, 4 });
            Expect(writer, 0xb8e69, new byte[] { 0x83, 0xc0, 0xd8, 0x55, 0x57, 0xb9, 0xf0, 0x8c, 0xde, 0 });
            // Secondary terrain surface: 1024 + 56 = 1080, matching 27 cached 40-pixel rows.
            Expect(writer, 0x17c60d, new byte[] { 0x83, 0xc2, 0xd8 });
            Expect(writer, 0x17561d, new byte[] { 0x8d, 0x46, 0xd8 });
            writer.Seek(0x17c60f, SeekOrigin.Begin); writer.Write((byte)56);
            writer.Seek(0x17561f, SeekOrigin.Begin); writer.Write((byte)56);
            writer.Seek(TerrainCave, SeekOrigin.Begin); writer.Write(BuildTileRows());
            writer.Seek(TerrainCave + 32, SeekOrigin.Begin); writer.Write(BuildTerrainWrap());
            WriteJump(writer, 0xdc8f3, TerrainCave, 5);
            WriteJump(writer, 0xb8e69, TerrainCave + 32, 10);
        }

        static void WriteJump(BinaryWriter writer, int offset, int target, int length)
        {
            writer.Seek(offset, SeekOrigin.Begin);
            writer.Write((byte)0xe9); writer.Write(target - offset - 5);
            writer.Write(Enumerable.Repeat((byte)0x90, length - 5).ToArray());
        }

        static void Expect(BinaryWriter writer, int offset, byte[] expected)
        {
            writer.Flush(); writer.Seek(offset, SeekOrigin.Begin);
            foreach (var value in expected)
                if (writer.BaseStream.ReadByte() != value)
                    throw new InvalidDataException("게임 패치 대상 코드가 예상과 다릅니다. 원본을 확인해 주세요.");
        }
    }
}
