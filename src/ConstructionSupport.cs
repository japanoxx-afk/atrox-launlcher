using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AtroxLauncher
{
    internal static class ConstructionSupport
    {
        internal const int Cave = 0x99de0;
        internal static void Apply(BinaryWriter writer)
        {
            var sites = new[] { 0x13b440, 0x13deb2, 0x66330, 0xc6f20 };
            var originals = new[] {
                new byte[] { 0xe8,0x74,0x8e,0xec,0xff },
                new byte[] { 0xe8,0x02,0x64,0xec,0xff },
                new byte[] { 0x53,0x8b,0xd9,0x56,0x57 },
                new byte[] { 0x6a,0xff,0x68,0x38,0xc7,0x5c,0 }
            };
            Expect(writer, Cave, Enumerable.Repeat((byte)0xcc, 1248).ToArray());
            for (int i = 0; i < sites.Length; ++i) Expect(writer, sites[i], originals[i]);
            // Existing support admission is >= 2 => rejected. Keep the native
            // limit and count its accepted helper list only at successful completion.
            Expect(writer, 0x13b63f, new byte[] { 0x83,0x79,4,2,0x7d,0x2c });
            const int name = Cave + 1100, export = Cave + 1140;
            for (int i = 0; i < sites.Length; ++i)
            {
                var code = new List<byte>();
                int start = Cave + i * 256;
                Action<byte[]> emit = bytes => code.AddRange(bytes);
                Action<int> num = n => code.AddRange(BitConverter.GetBytes(n));
                Action<int> api = a => { emit(new byte[] {0xff,0x15}); num(a); };
                if (i < 2) { code.Add(0xe8); num(0x4042b9 - (0x400000 + start + code.Count + 4)); }
                emit(new byte[] {0x9c,0x60,0x68}); num(0x400000 + name + 2); api(0xe66e58);
                emit(new byte[] {0x85,0xc0,0x75,11,0x68}); num(0x400000 + name); api(0xe66e64);
                emit(new byte[] {0x85,0xc0,0x74,0}); int missingDll = code.Count - 1;
                code.Add(0x68); num(0x400000 + export); code.Add(0x50); api(0xe66e68);
                emit(new byte[] {0x85,0xc0,0x74,0}); int missingExport = code.Count - 1;
                if (i < 2) code.Add(0x56); else emit(new byte[] {0x6a,0});
                emit(new byte[] {0x6a,(byte)(i < 2 ? 0 : i - 1),0xff,0xd0});
                code[missingDll] = (byte)(code.Count - missingDll - 1);
                code[missingExport] = (byte)(code.Count - missingExport - 1);
                emit(new byte[] {0x61,0x9d});
                if (i >= 2) emit(originals[i]);
                code.Add(0xe9); num(sites[i] + originals[i].Length - (start + code.Count + 4));
                writer.Seek(start, SeekOrigin.Begin); writer.Write(code.ToArray());
                writer.Seek(sites[i], SeekOrigin.Begin); writer.Write((byte)0xe9); writer.Write(start - sites[i] - 5);
                writer.Write(Enumerable.Repeat((byte)0x90, originals[i].Length - 5).ToArray());
            }
            writer.Seek(name, SeekOrigin.Begin); writer.Write(Encoding.ASCII.GetBytes(".\\AtroxReplay.dll\0"));
            writer.Seek(export, SeekOrigin.Begin); writer.Write(Encoding.ASCII.GetBytes("ConstructionEvent\0"));
        }
        static void Expect(BinaryWriter writer, int offset, byte[] expected)
        {
            writer.Flush(); writer.Seek(offset, SeekOrigin.Begin);
            foreach (byte b in expected) if (writer.BaseStream.ReadByte() != b)
                throw new InvalidDataException("건설 패치 대상 코드가 예상과 다릅니다.");
        }
    }
}
