using System;
using System.IO;
using System.Linq;

namespace AtroxLauncher
{
    internal static class GamePatches
    {
        internal const int HudHook = 0x63209;
        internal const int HudCave = 0x63610;
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
            if (scrollPercent < 20 || scrollPercent > 100 || scrollPercent % 20 != 0)
                throw new ArgumentOutOfRangeException(nameof(scrollPercent));
            Expect(writer, 0x2d39a, new byte[] { 0x8d, 0x04, 0x80 });
            Expect(writer, 0x2d3ad, new byte[] { 0x8d, 0x0c, 0x80 });
            // Original speed is 5 * 4 * (setting + 1); keep acceleration and boundary checks.
            writer.Seek(0x2d39a, SeekOrigin.Begin);
            writer.Write(new byte[] { 0x6b, 0xc0, (byte)(scrollPercent / 20) });
            writer.Seek(0x2d3ad, SeekOrigin.Begin);
            writer.Write(new byte[] { 0x6b, 0xc8, (byte)(scrollPercent / 20) });
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
