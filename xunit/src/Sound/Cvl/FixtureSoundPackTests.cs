using System;
using System.IO;
using System.Linq;
using CivOne.Sound.Cvl;
using Xunit;

namespace CivOne.UnitTests.Sound.Cvl
{
    public class FixtureSoundPackTests
    {
        [Fact]
        public void Fixture_ValidPack_CanBeLoaded()
        {
            var path = Path.Combine(GetFixturesRoot(), "Valid", "adlib-pack.sound.json");
            var pack = SoundPackJson.Load(path);

            Assert.Equal("fixture-adlib-pack", pack.Id);
            Assert.Equal(2, pack.Tunes.Count);
            Assert.Equal("Title Music", pack.Tunes[0].Title);
        }

        [Fact]
        public void FixtureRegistry_LoadsAllValidPacks()
        {
            var folder = Path.Combine(GetFixturesRoot(), "Valid");
            var result = SoundPackRegistry.LoadFromFolder(folder);

            Assert.Equal(2, result.Packs.Count);
            Assert.Empty(result.Errors);
            Assert.Contains(result.Packs, x => x.Id == "fixture-adlib-pack");
            Assert.Contains(result.Packs, x => x.Id == "fixture-speaker-pack");
        }

        [Fact]
        public void FixtureRegistry_InvalidPack_ReportedAsError()
        {
            var folder = Path.Combine(GetFixturesRoot(), "Invalid");
            var result = SoundPackRegistry.LoadFromFolder(folder);

            Assert.Empty(result.Packs);
            Assert.Single(result.Errors);
            Assert.Contains("keine events", result.Errors[0], StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void EventPlayer_WithAudioBackend_RecordsPortWrites()
        {
            var path = Path.Combine(GetFixturesRoot(), "Valid", "adlib-pack.sound.json");
            var pack = SoundPackJson.Load(path);
            var player = new EventPlayer();
            var backend = new RecordingAudioBackend();
            var sink = new AudioBackendEventSink(backend);

            player.PlayTune(pack, tuneId: 3, sink);

            Assert.Single(backend.PortWrites);
            var write = backend.PortWrites[0];
            Assert.Equal(0x388, write.Port);
            Assert.Equal(0x20, write.Value);
        }

        [Fact]
        public void EventPlayer_WithAudioBackendAndInnerSink_ForwardsToBoth()
        {
            var path = Path.Combine(GetFixturesRoot(), "Valid", "adlib-pack.sound.json");
            var pack = SoundPackJson.Load(path);
            var player = new EventPlayer();
            var backend = new RecordingAudioBackend();
            var collector = new CollectingEventSink();
            var sink = new AudioBackendEventSink(backend, collector);

            player.PlayTune(pack, tuneId: 3, sink);

            Assert.Single(backend.PortWrites);
            Assert.Equal(3, collector.Calls.Count);
            Assert.Contains(collector.Calls, x => x.Contains(":port:"));
        }

        private static string GetFixturesRoot()
        {
            // from xunit/bin/Debug/net9.0 -> up to repo/xunit
            var xunitRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
            return Path.Combine(xunitRoot, "src", "Sound", "Cvl", "Fixtures");
        }
    }
}
