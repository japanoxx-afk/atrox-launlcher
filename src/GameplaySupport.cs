using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AtroxLauncher
{
    internal static class GameplaySupport
    {
        internal const int Cave = 0x44070;
        internal static void Apply(BinaryWriter writer)
        {
            int[] sites = {0x103f90, 0x3d850, 0x6b8e0};
            byte[][] originals = {
                new byte[] {0x8b,0x54,0x24,4,0x8b,0x49,4},
                new byte[] {0x81,0xec,0xa0,0,0,0},
                new byte[] {0x83,0xec,0x58,0x53,0x55}
            };
            Expect(writer, Cave, Enumerable.Repeat((byte)0xcc, 896).ToArray());
            for (int i = 0; i < sites.Length; ++i) Expect(writer, sites[i], originals[i]);
            const int name = Cave + 800, export = Cave + 832;
            for (int i = 0; i < sites.Length; ++i)
            {
                int start = Cave + i * 256;
                var code = new List<byte>();
                Action<byte[]> emit = bytes => code.AddRange(bytes);
                Action<int> num = n => code.AddRange(BitConverter.GetBytes(n));
                Action<int> api = a => { emit(new byte[] {0xff,0x15}); num(a); };
                emit(new byte[] {0x9c,0x60,0x68}); num(0x400000 + name + 2); api(0xe66e58);
                emit(new byte[] {0x85,0xc0,0x75,11,0x68}); num(0x400000 + name); api(0xe66e64);
                emit(new byte[] {0x85,0xc0,0x74,0}); int missingDll = code.Count - 1;
                code.Add(0x68); num(0x400000 + export); code.Add(0x50); api(0xe66e68);
                emit(new byte[] {0x85,0xc0,0x74,0}); int missingExport = code.Count - 1;
                // Saved ECX is [esp+24], original argument list starts at +40.
                emit(new byte[] {0x8b,0x4c,0x24,24});
                if (i == 0) emit(new byte[] {0x8d,0x54,0x24,40,0x52});
                else if (i == 1) emit(new byte[] {0xff,0x74,0x24,40});
                else emit(new byte[] {0x6a,0});
                emit(new byte[] {0x51,0x6a,(byte)i,0xff,0xd0});
                if (i < 2)
                {
                    emit(new byte[] {0x85,0xc0,0x74,9,0x89,0x44,0x24,28,0x61,0x9d,0xc2,(byte)(i == 0 ? 8 : 4),0});
                }
                code[missingDll] = (byte)(code.Count - missingDll - 1);
                code[missingExport] = (byte)(code.Count - missingExport - 1);
                emit(new byte[] {0x61,0x9d}); emit(originals[i]);
                code.Add(0xe9); num(sites[i] + originals[i].Length - (start + code.Count + 4));
                if (code.Count > 256) throw new InvalidDataException("Gameplay stub overflow");
                writer.Seek(start, SeekOrigin.Begin); writer.Write(code.ToArray());
                writer.Seek(sites[i], SeekOrigin.Begin); writer.Write((byte)0xe9); writer.Write(start - sites[i] - 5);
                writer.Write(Enumerable.Repeat((byte)0x90, originals[i].Length - 5).ToArray());
            }
            writer.Seek(name, SeekOrigin.Begin); writer.Write(Encoding.ASCII.GetBytes(".\\AtroxReplay.dll\0"));
            writer.Seek(export, SeekOrigin.Begin); writer.Write(Encoding.ASCII.GetBytes("GameplayEvent\0"));
            ApplyExtendedSelection(writer, name, export);
        }
        static void ApplyExtendedSelection(BinaryWriter writer, int name, int export)
        {
            const int start = 0x1a7c00, site = 0x3de80, gateway = 0x1a7dc0;
            Expect(writer, start, Enumerable.Repeat((byte)0xcc, 512).ToArray());
            byte[] original = {0x53,0x55,0x8b,0x6c,0x24,0x10};
            Expect(writer, site, original);
            var code = new List<byte>();
            Action<byte[]> emit = bytes => code.AddRange(bytes);
            Action<int> num = n => code.AddRange(BitConverter.GetBytes(n));
            Action<int> api = n => {emit(new byte[] {0xff,0x15});num(n);};
            emit(new byte[] {0x9c,0x60,0x68}); num(0x400000 + name + 2); api(0xe66e58);
            emit(new byte[] {0x85,0xc0,0x75,11,0x68}); num(0x400000 + name); api(0xe66e64);
            emit(new byte[] {0x85,0xc0,0x74,0}); int missing = code.Count - 1;
            code.Add(0x68); num(0x400000 + export); code.Add(0x50); api(0xe66e68);
            emit(new byte[] {0x85,0xc0,0x74,14,0x8b,0x4c,0x24,24,0x8d,0x54,0x24,40,0x52,0x51,0x6a,3,0xff,0xd0});
            code[missing] = (byte)(code.Count - missing - 1);
            emit(new byte[] {0x61,0x9d}); emit(original);
            code.Add(0xe9); num(site + original.Length - (start + code.Count + 4));
            writer.Seek(start, SeekOrigin.Begin); writer.Write(code.ToArray());
            writer.Seek(site, SeekOrigin.Begin); writer.Write((byte)0xe9); writer.Write(start - site - 5); writer.Write((byte)0x90);
            writer.Seek(gateway, SeekOrigin.Begin);
            writer.Write(new byte[] {0x81,0xec,0xa0,0,0,0,0xe9});
            writer.Write(0x3d856 - (gateway + 11));
        }
        static void Expect(BinaryWriter writer, int offset, byte[] expected)
        {
            writer.Flush(); writer.Seek(offset, SeekOrigin.Begin);
            foreach (byte b in expected) if (writer.BaseStream.ReadByte() != b)
                throw new InvalidDataException("조작 개선 패치 대상 코드가 예상과 다릅니다.");
        }
    }
}
