using System;
using System.IO;
using System.Linq;
using CivOne.Sound.Cvl;
using Xunit;

namespace CivOne.UnitTests.Sound.Cvl
{
    public class CvlConverterTests
    {
        [Fact]
        public void Integration_Convert_RealAsoundCvl_WhenAvailable()
        {
            var cvlPath = TryResolveAsoundCvlPath();
            if (cvlPath == null)
            {
                // Optional integration test: if no local CVL is available, do nothing.
                return;
            }

            var outPath = Path.Combine(Path.GetTempPath(), $"asound-real-{Guid.NewGuid():N}.sound.json");
            try
            {
                var options = new CvlConversionOptions
                {
                    PackId = "asound-real",
                    DisplayName = "ASOUND Real",
                    Format = "opl2",
                    Driver = "ASOUND",
                    TuneIds = new[] { 3, 4, 34, 35 },
                    SchedulerTicks = 32,
                    MaxRecordedInstructions = 512
                };

                CvlToSoundPackConverter.ConvertToFile(cvlPath, outPath, options);
                var loaded = SoundPackJson.Load(outPath);

                Assert.Equal("asound-real", loaded.Id);
                Assert.Equal("ASOUND", loaded.Driver);
                Assert.NotEmpty(loaded.Tunes);
                Assert.All(loaded.Tunes, t => Assert.NotEmpty(t.Events));
            }
            finally
            {
                if (File.Exists(outPath)) File.Delete(outPath);
            }
        }

        [Fact]
        public void Integration_Convert_RealIsoundCvl_WhenAvailable()
        {
            var cvlPath = TryResolveIsoundCvlPath();
            if (cvlPath == null)
            {
                // Optional integration test: if no local CVL is available, do nothing.
                return;
            }

            var outPath = Path.Combine(Path.GetTempPath(), $"isound-real-{Guid.NewGuid():N}.sound.json");
            try
            {
                var options = new CvlConversionOptions
                {
                    PackId = "isound-real",
                    DisplayName = "ISOUND Real",
                    Format = "speaker",
                    Driver = "ISOUND",
                    TuneIds = new[] { 3, 4, 34, 35 },
                    SchedulerTicks = 32,
                    MaxRecordedInstructions = 512
                };

                CvlToSoundPackConverter.ConvertToFile(cvlPath, outPath, options);
                var loaded = SoundPackJson.Load(outPath);

                Assert.Equal("isound-real", loaded.Id);
                Assert.Equal("speaker", loaded.Format);
                Assert.Equal("ISOUND", loaded.Driver);
                Assert.NotEmpty(loaded.Tunes);
                Assert.All(loaded.Tunes, t => Assert.NotEmpty(t.Events));
                Assert.All(
                    loaded.Tunes.SelectMany(t => t.Events).Where(e => e.Type == SoundEventType.PortWrite),
                    e => Assert.Contains(e.Port!.Value, new[] { 0x42, 0x43, 0x61 }));

                var serializedEvents = loaded.Tunes
                    .Select(t => string.Join("|", t.Events.Select(e => $"{e.T}:{e.Type}:{e.Port}:{e.Value}:{e.IntNo}")))
                    .Distinct()
                    .Count();
                Assert.True(serializedEvents > 1, "ISOUND conversion should yield tune-specific event streams.");
            }
            finally
            {
                if (File.Exists(outPath)) File.Delete(outPath);
            }
        }

        [Fact]
        public void Convert_BuildsPackWithRequestedTunesAndKnownTitles()
        {
            var cvlPath = CreateFakeCvlFile(withPortWrites: true);
            try
            {
                var options = new CvlConversionOptions
                {
                    PackId = "converted-asound",
                    DisplayName = "Converted ASOUND",
                    Format = "opl2",
                    Driver = "ASOUND",
                    TuneIds = new[] { 3, 35 },
                    SchedulerTicks = 16
                };

                var pack = CvlToSoundPackConverter.Convert(cvlPath, options);

                Assert.Equal("converted-asound", pack.Id);
                Assert.Equal(2, pack.Tunes.Count);
                Assert.Contains(pack.Tunes, t => t.TuneId == 3 && t.Title == "Title Music");
                Assert.Contains(pack.Tunes, t => t.TuneId == 35 && t.Title == "Lose Music");
            }
            finally
            {
                File.Delete(cvlPath);
            }
        }

        [Fact]
        public void Convert_ProducesOplPortWrites_ForOplFormat()
        {
            var cvlPath = CreateFakeCvlFile(withPortWrites: true);
            try
            {
                var options = new CvlConversionOptions
                {
                    PackId = "converted-ports",
                    DisplayName = "Converted Ports",
                    Format = "opl2",
                    TuneIds = new[] { 3 },
                    SchedulerTicks = 8,
                    MaxRecordedInstructions = 8
                };

                var pack = CvlToSoundPackConverter.Convert(cvlPath, options);
                var tune = pack.Tunes.Single();
                var ports = tune.Events.Where(e => e.Type == SoundEventType.PortWrite).Select(e => e.Port).ToArray();

                Assert.Contains(0x388, ports);
                Assert.Contains(0x389, ports);
                Assert.DoesNotContain(ports, p => p is < 0x388 or > 0x38B);
            }
            finally
            {
                File.Delete(cvlPath);
            }
        }

        [Fact]
        public void Convert_UsesFallbackPorts_WhenNoStaticPortWritesExist()
        {
            var cvlPath = CreateFakeCvlFile(withPortWrites: false);
            try
            {
                var options = new CvlConversionOptions
                {
                    PackId = "converted-fallback",
                    DisplayName = "Converted Fallback",
                    Format = "opl2",
                    TuneIds = new[] { 4 }
                };

                var pack = CvlToSoundPackConverter.Convert(cvlPath, options);
                var ports = pack.Tunes.Single().Events
                    .Where(e => e.Type == SoundEventType.PortWrite)
                    .Select(e => e.Port)
                    .ToArray();

                Assert.Contains(0x388, ports);
                Assert.Contains(0x389, ports);
            }
            finally
            {
                File.Delete(cvlPath);
            }
        }

        [Fact]
        public void ConvertToFile_WritesLoadableSoundPackJson()
        {
            var cvlPath = CreateFakeCvlFile(withPortWrites: true);
            var outPath = Path.Combine(Path.GetTempPath(), $"converted-{Guid.NewGuid():N}.sound.json");

            try
            {
                var options = new CvlConversionOptions
                {
                    PackId = "converted-file",
                    DisplayName = "Converted File",
                    Format = "opl2",
                    TuneIds = new[] { 3 }
                };

                CvlToSoundPackConverter.ConvertToFile(cvlPath, outPath, options);
                var loaded = SoundPackJson.Load(outPath);

                Assert.Equal("converted-file", loaded.Id);
                Assert.Single(loaded.Tunes);
            }
            finally
            {
                File.Delete(cvlPath);
                if (File.Exists(outPath)) File.Delete(outPath);
            }
        }

        [Fact]
        public void Convert_IsDeterministic_ForSameInput()
        {
            var cvlPath = CreateFakeCvlFile(withPortWrites: true);
            try
            {
                var options = new CvlConversionOptions
                {
                    PackId = "deterministic-pack",
                    DisplayName = "Deterministic Pack",
                    Format = "opl2",
                    TuneIds = new[] { 3, 4 },
                    SchedulerTicks = 16,
                    MaxRecordedInstructions = 32
                };

                var pack1 = CvlToSoundPackConverter.Convert(cvlPath, options);
                var pack2 = CvlToSoundPackConverter.Convert(cvlPath, options);

                var s1 = SoundPackJsonRoundtrip(pack1);
                var s2 = SoundPackJsonRoundtrip(pack2);

                Assert.Equal(s1, s2);
            }
            finally
            {
                File.Delete(cvlPath);
            }
        }

        [Fact]
        public void Convert_IncludesInterruptEvents_WhenPresentInCvl()
        {
            var cvlPath = CreateFakeCvlFile(withPortWrites: true);
            try
            {
                var options = new CvlConversionOptions
                {
                    PackId = "interrupt-pack",
                    DisplayName = "Interrupt Pack",
                    Format = "opl2",
                    TuneIds = new[] { 3 },
                    MaxRecordedInstructions = 64
                };

                var pack = CvlToSoundPackConverter.Convert(cvlPath, options);
                var tune = pack.Tunes.Single();

                Assert.Contains(tune.Events, e => e.Type == SoundEventType.Interrupt && e.IntNo == 0x08);
            }
            finally
            {
                File.Delete(cvlPath);
            }
        }

        private static string CreateFakeCvlFile(bool withPortWrites)
        {
            var bytes = new byte[0x120];

            // Minimal gültiger Headerbereich bis 0x3C
            WriteU16(bytes, 0x28, 0x1000);
            WriteU16(bytes, 0x2A, 0x2000);
            WriteU16(bytes, 0x2C, 0x0030);
            WriteU16(bytes, 0x32, 0x0100);
            WriteU16(bytes, 0x34, 0x0110);
            WriteU16(bytes, 0x36, 0x0120);
            WriteU16(bytes, 0x38, 0x0130);
            WriteU16(bytes, 0x3A, 0x0140);
            WriteU16(bytes, 0x3C, 0x0150);

            if (withPortWrites)
            {
                // OUT imm8, AL opcodes (E6 ib)
                bytes[0x80] = 0xE6;
                bytes[0x81] = 0x88;
                bytes[0x90] = 0xE6;
                bytes[0x91] = 0x89;
            }

            // INT 08h opcode for interrupt trace
            bytes[0xA0] = 0xCD;
            bytes[0xA1] = 0x08;

            var path = Path.Combine(Path.GetTempPath(), $"fake-{Guid.NewGuid():N}.cvl");
            File.WriteAllBytes(path, bytes);
            return path;
        }

        private static void WriteU16(byte[] buffer, int offset, ushort value)
        {
            buffer[offset] = (byte)(value & 0xFF);
            buffer[offset + 1] = (byte)((value >> 8) & 0xFF);
        }

        private static string TryResolveAsoundCvlPath()
        {
            var fromEnv = Environment.GetEnvironmentVariable("CIVONE_ASOUND_CVL");
            if (!string.IsNullOrWhiteSpace(fromEnv) && File.Exists(fromEnv))
            {
                return fromEnv;
            }

            var candidates = new System.Collections.Generic.List<string>();

            var xunitRoot = FindXunitRootFrom(AppContext.BaseDirectory)
                ?? FindXunitRootFrom(Environment.CurrentDirectory);

            if (xunitRoot != null)
            {
                candidates.Add(Path.Combine(xunitRoot, "TestData", "Cvl", "asound.cvl"));

                var repoRoot = Directory.GetParent(xunitRoot)?.FullName;
                if (!string.IsNullOrWhiteSpace(repoRoot))
                {
                    candidates.Add(Path.Combine(repoRoot, "temp", "Sound", "asound.cvl"));
                    candidates.Add(Path.Combine(repoRoot, "temp", "asound.cvl"));
                }
            }

            candidates.Add(Path.Combine(Environment.CurrentDirectory, "xunit", "TestData", "Cvl", "asound.cvl"));
            candidates.Add(Path.Combine(Environment.CurrentDirectory, "temp", "Sound", "asound.cvl"));

            return candidates.FirstOrDefault(File.Exists);
        }

        private static string TryResolveIsoundCvlPath()
        {
            var fromEnv = Environment.GetEnvironmentVariable("CIVONE_ISOUND_CVL");
            if (!string.IsNullOrWhiteSpace(fromEnv) && File.Exists(fromEnv))
            {
                return fromEnv;
            }

            var candidates = new System.Collections.Generic.List<string>();

            var xunitRoot = FindXunitRootFrom(AppContext.BaseDirectory)
                ?? FindXunitRootFrom(Environment.CurrentDirectory);

            if (xunitRoot != null)
            {
                candidates.Add(Path.Combine(xunitRoot, "TestData", "Cvl", "isound.cvl"));

                var repoRoot = Directory.GetParent(xunitRoot)?.FullName;
                if (!string.IsNullOrWhiteSpace(repoRoot))
                {
                    candidates.Add(Path.Combine(repoRoot, "temp", "Sound", "isound.cvl"));
                    candidates.Add(Path.Combine(repoRoot, "temp", "isound.cvl"));
                }
            }

            candidates.Add(Path.Combine(Environment.CurrentDirectory, "xunit", "TestData", "Cvl", "isound.cvl"));
            candidates.Add(Path.Combine(Environment.CurrentDirectory, "temp", "Sound", "isound.cvl"));

            return candidates.FirstOrDefault(File.Exists);
        }

        private static string FindXunitRootFrom(string startPath)
        {
            if (string.IsNullOrWhiteSpace(startPath)) return null;

            var dir = new DirectoryInfo(startPath);
            if (!dir.Exists && dir.Parent != null)
            {
                dir = dir.Parent;
            }

            while (dir != null)
            {
                if (string.Equals(dir.Name, "xunit", StringComparison.OrdinalIgnoreCase))
                {
                    return dir.FullName;
                }

                dir = dir.Parent;
            }

            return null;
        }

        private static string SoundPackJsonRoundtrip(SoundPack pack)
        {
            var path = Path.Combine(Path.GetTempPath(), $"roundtrip-{Guid.NewGuid():N}.sound.json");
            try
            {
                SoundPackJson.Save(path, pack);
                return File.ReadAllText(path);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }
    }
}
