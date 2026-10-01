using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Text;

namespace AtroxLauncher
{
    internal static class ReplaySupport
    {
        internal const int Cave = 0xb1ec0;
        internal static void Install(string gameDirectory, bool enabled, int maximum)
        {
            if (maximum < 1 || maximum > 1000) throw new ArgumentOutOfRangeException(nameof(maximum));
            var target = Path.Combine(gameDirectory, "AtroxReplay.dll");
            var staged = target + ".next";
            using (var input = Assembly.GetExecutingAssembly().GetManifestResourceStream("AtroxLauncher.Replay.dll"))
            using (var output = File.Create(staged)) input.CopyTo(output);
            if (File.Exists(target)) File.Replace(staged, target, null);
            else File.Move(staged, target);
            File.WriteAllText(Path.Combine(gameDirectory, "AtroxReplay.ini"),
                "[Replay]\r\nAutoSave=" + (enabled ? "1" : "0") + "\r\nMaximum=" + maximum + "\r\n", Encoding.ASCII);
        }

        internal static void Apply(BinaryWriter writer)
        {
            // Supported image's unused executable padding. Verify every hook before writing.
            var hooks = new[] { 0x630e0, 0x63223, 0xb8a60, 0xb8ef9, 0xc7271 };
            var originals = new[] {
                new byte[] { 0x83,0xec,0x10,0x57,0x8b,0xf9 },
                new byte[] { 0x5f,0x83,0xc4,0x10,0xc3 },
                new byte[] { 0x83,0xec,0x44,0x53,0x55 },
                new byte[] { 0x5f,0x5e,0x5d,0x5b,0x83,0xc4,0x44 },
                new byte[] { 0xff,0x15,0x90,0x6e,0xe6,0 }
            };
            Expect(writer, Cave, Enumerable.Repeat((byte)0xcc, 2160).ToArray());
            for (int i = 0; i < hooks.Length; ++i) Expect(writer, hooks[i], originals[i]);
            const int name = Cave + 1900, function = Cave + 1940;
            var events = new[] { 1, 2, 1, 2, 0 };
            for (int i = 0; i < hooks.Length; ++i)
            {
                int start = Cave + i * 256;
                var code = new List<byte>();
                Action<byte[]> emit = bytes => code.AddRange(bytes);
                Action<int> number = value => code.AddRange(BitConverter.GetBytes(value));
                Action<int> api = address => { emit(new byte[] { 0xff, 0x15 }); number(address); };
                // Preserve WriteFile's stdcall stack before invoking the save event.
                if (i == 4) emit(originals[i]);
                emit(new byte[] { 0x9c, 0x60, 0x68 }); number(0x400000 + name + 2);
                api(0xe66e58); // GetModuleHandleA("AtroxReplay.dll")
                emit(new byte[] { 0x85,0xc0,0x75,11,0x68 }); number(0x400000 + name);
                api(0xe66e64); // LoadLibraryA(".\\AtroxReplay.dll")
                emit(new byte[] { 0x85,0xc0,0x74,23,0x68 }); number(0x400000 + function);
                code.Add(0x50); api(0xe66e68); // GetProcAddress
                emit(new byte[] { 0x85,0xc0,0x74,7,0x68 }); number(events[i]);
                emit(new byte[] { 0xff,0xd0,0x61,0x9d });
                if (i != 4) emit(originals[i]);
                code.Add(0xe9); number(hooks[i] + originals[i].Length - (start + code.Count + 4));
                if (code.Count >= 256) throw new InvalidDataException("Replay hook exceeds reserved space.");
                writer.Seek(start, SeekOrigin.Begin); writer.Write(code.ToArray());
                writer.Seek(hooks[i], SeekOrigin.Begin); writer.Write((byte)0xe9); writer.Write(start - hooks[i] - 5);
                writer.Write(Enumerable.Repeat((byte)0x90, originals[i].Length - 5).ToArray());
            }
            writer.Seek(name, SeekOrigin.Begin); writer.Write(Encoding.ASCII.GetBytes(".\\AtroxReplay.dll\0"));
            writer.Seek(function, SeekOrigin.Begin); writer.Write(Encoding.ASCII.GetBytes("ReplayEvent\0"));
            ApplySavedGame(writer, name);
        }
        static void ApplySavedGame(BinaryWriter writer, int name)
        {
            const int site = 0x8b473, start = Cave + 1536, function = Cave + 1980;
            byte[] original = {0x8b,0x0d,0x08,0x77,0xb2,0};
            Expect(writer, site, original);
            var code = new List<byte>();
            Action<byte[]> emit = bytes => code.AddRange(bytes);
            Action<int> num = n => code.AddRange(BitConverter.GetBytes(n));
            Action<int> api = n => { emit(new byte[] {0xff,0x15}); num(n); };
            emit(new byte[] {0x9c,0x60,0x68}); num(0x400000 + name + 2); api(0xe66e58);
            emit(new byte[] {0x85,0xc0,0x75,11,0x68}); num(0x400000 + name); api(0xe66e64);
            emit(new byte[] {0x85,0xc0,0x74,0}); int missing = code.Count - 1;
            code.Add(0x68); num(0x400000 + function); code.Add(0x50); api(0xe66e68);
            emit(new byte[] {0x85,0xc0,0x74,2,0xff,0xd0});
            code[missing] = (byte)(code.Count - missing - 1);
            emit(new byte[] {0x61,0x9d}); emit(original);
            code.Add(0xe9); num(site + original.Length - (start + code.Count + 4));
            writer.Seek(start, SeekOrigin.Begin); writer.Write(code.ToArray());
            writer.Seek(site, SeekOrigin.Begin); writer.Write((byte)0xe9); writer.Write(start - site - 5); writer.Write((byte)0x90);
            writer.Seek(function, SeekOrigin.Begin); writer.Write(Encoding.ASCII.GetBytes("PrepareSavedMap\0"));

            // Failed initialization can dispose a zero-initialized UI container.
            // Native ClearChildren expects its first index to be -1, not zero.
            const int clear = 0x18730, guard = Cave + 1664;
            byte[] prologue = {0x56,0x57,0x8b,0xf9,0x0f,0xbf,0x77,0x3e};
            Expect(writer, clear, prologue);
            code.Clear();
            emit(new byte[] {0x85,0xc9,0x74,13,0x66,0x83,0x79,0x3e,0,0x75,9,0x83,0x79,0x68,0,0x75,3,0xb0,1,0xc3});
            emit(prologue); code.Add(0xe9); num(clear + prologue.Length - (guard + code.Count + 4));
            writer.Seek(guard, SeekOrigin.Begin); writer.Write(code.ToArray());
            writer.Seek(clear, SeekOrigin.Begin); writer.Write((byte)0xe9); writer.Write(guard - clear - 5); writer.Write(new byte[] {0x90,0x90,0x90});
        }
        static void Expect(BinaryWriter writer, int offset, byte[] expected)
        {
            writer.Flush(); writer.Seek(offset, SeekOrigin.Begin);
            foreach (byte value in expected)
                if (writer.BaseStream.ReadByte() != value) throw new InvalidDataException("리플레이 패치 대상 코드가 예상과 다릅니다.");
        }
    }
}
