using System;
using System.IO;
using CivOne.Sound.Cvl;
using Xunit;

namespace CivOne.UnitTests.Sound.Cvl
{
    public class SoundPackRegistryTests
    {
        [Fact]
        public void LoadFromFolder_UsesProvidedFolderAndPattern()
        {
            var root = CreateTempDir();
            var folderA = Path.Combine(root, "A");
            var folderB = Path.Combine(root, "B");
            Directory.CreateDirectory(folderA);
            Directory.CreateDirectory(folderB);

            try
            {
                SoundPackJson.Save(Path.Combine(folderA, "a.sound.json"), CreatePack("pack-a", "Alpha"));
                SoundPackJson.Save(Path.Combine(folderB, "b.sound.json"), CreatePack("pack-b", "Beta"));

                File.WriteAllText(Path.Combine(folderA, "ignore.json"), "{}");
                File.WriteAllText(Path.Combine(folderA, "readme.txt"), "ignore");

                var resultA = SoundPackRegistry.LoadFromFolder(folderA);
                var resultB = SoundPackRegistry.LoadFromFolder(folderB);

                Assert.Single(resultA.Packs);
                Assert.Equal("pack-a", resultA.Packs[0].Id);

                Assert.Single(resultB.Packs);
                Assert.Equal("pack-b", resultB.Packs[0].Id);
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

        [Fact]
        public void LoadFromFolder_SkipsInvalidFilesAndCollectsErrors()
        {
            var root = CreateTempDir();
            try
            {
                SoundPackJson.Save(Path.Combine(root, "good.sound.json"), CreatePack("good", "Good"));
                File.WriteAllText(Path.Combine(root, "bad.sound.json"), "{ \"schemaVersion\": 999 }");

                var result = SoundPackRegistry.LoadFromFolder(root);

                Assert.Single(result.Packs);
                Assert.Single(result.Errors);
                Assert.Contains("bad.sound.json", result.Errors[0], StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

        [Fact]
        public void LoadFromFolder_ReportsDuplicatePackIds()
        {
            var root = CreateTempDir();
            try
            {
                SoundPackJson.Save(Path.Combine(root, "one.sound.json"), CreatePack("dup-id", "One"));
                SoundPackJson.Save(Path.Combine(root, "two.sound.json"), CreatePack("dup-id", "Two"));

                var result = SoundPackRegistry.LoadFromFolder(root);

                Assert.Single(result.Packs);
                Assert.Single(result.Errors);
                Assert.Contains("Duplicate pack id", result.Errors[0]);
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

        [Fact]
        public void LoadFromFolder_SortsPacksByDisplayName()
        {
            var root = CreateTempDir();
            try
            {
                SoundPackJson.Save(Path.Combine(root, "b.sound.json"), CreatePack("b", "Zulu"));
                SoundPackJson.Save(Path.Combine(root, "a.sound.json"), CreatePack("a", "Alpha"));

                var result = SoundPackRegistry.LoadFromFolder(root);

                Assert.Equal(2, result.Packs.Count);
                Assert.Equal("Alpha", result.Packs[0].DisplayName);
                Assert.Equal("Zulu", result.Packs[1].DisplayName);
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

        [Fact]
        public void LoadFromFolder_MissingFolder_ReturnsEmptyResult()
        {
            var path = Path.Combine(Path.GetTempPath(), "civone-sound-missing-" + Guid.NewGuid().ToString("N"));
            var result = SoundPackRegistry.LoadFromFolder(path);

            Assert.Empty(result.Packs);
            Assert.Empty(result.Errors);
        }

        private static string CreateTempDir()
        {
            var path = Path.Combine(Path.GetTempPath(), "civone-sound-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }

        private static SoundPack CreatePack(string id, string displayName)
        {
            return new SoundPack
            {
                SchemaVersion = 1,
                Id = id,
                DisplayName = displayName,
                Format = "opl2",
                Driver = "ASOUND",
                TickRate = 300,
                Tunes =
                [
                    new TuneDefinition
                    {
                        TuneId = 3,
                        Title = "Title Music",
                        EndlessLoop = true,
                        Events =
                        [
                            new SoundEvent { T = 0, Type = SoundEventType.Worker, Kind = "sound" }
                        ]
                    }
                ]
            };
        }
    }
}
