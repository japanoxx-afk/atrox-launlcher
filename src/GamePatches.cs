using System;
using System.IO;
using System.Linq;

namespace AtroxLauncher
{
    internal static class GamePatches
    {
        internal const int HudHook = 0x63209;
        internal const int HudCave = 0x63610;
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
        internal static byte[] BuildHudClear()
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(new byte[] { 0x9c, 0x60 }); // Preserve flags and all general registers.
                foreach (var x in new[] { 0, 1040 })
                {
                    // The game's own indexed-color fill draws inclusive endpoints.
                    foreach (var argument in new[] { 0, 107, 239, 916, x, 0 })
                    { writer.Write((byte)0x68); writer.Write(argument); }
                    writer.Write((byte)0xb9); writer.Write(0xde8cf0);
                    writer.Write((byte)0xe8);
                    writer.Write(0x405e43 - (0x400000 + HudCave + (int)stream.Position + 4));
                }
                writer.Write(new byte[] { 0x61, 0x9d, 0x8b, 0x0d, 0x30, 0x77, 0xb2, 0x00 });
                writer.Write((byte)0xe9);
                writer.Write(0x400000 + HudHook + 6 - (0x400000 + HudCave + (int)stream.Position + 4));
                return stream.ToArray();
            }
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
            Expect(writer, HudHook, new byte[] { 0x8b, 0x0d, 0x30, 0x77, 0xb2, 0x00 });
            Expect(writer, HudCave, Enumerable.Repeat((byte)0xcc, 176).ToArray());
            writer.Seek(HudCave, SeekOrigin.Begin);
            writer.Write(BuildHudClear());
            writer.Seek(HudHook, SeekOrigin.Begin);
            writer.Write((byte)0xe9); writer.Write(HudCave - HudHook - 5); writer.Write((byte)0x90);
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
