using System.Collections.Generic;
using System.Linq;
using CivOne.Sound.Cvl;
using CivOne.Sound.Cvl.Adlib;
using CivOne.Sound.Playback.Adlib;
using CivOne.UnitTests.Sound.Cvl.Adlib;
using Xunit;
using Xunit.Abstractions;

namespace CivOne.UnitTests.Sound.Playback
{
    /// <summary>
    /// Covers the driver player by watching what it writes to the chip.
    /// </summary>
    public sealed class AdlibTunePlayerTests
    {
        private const int KeyOn = 0x20;

        private readonly ITestOutputHelper _output;

        public AdlibTunePlayerTests(ITestOutputHelper output) => _output = output;

        private static AsoundParser Parser()
            => AsoundParser.Create(CvlImage.FromBytes(FakeAsoundModule.Build(), "fake-asound.cvl"));

        private static (AdlibSoundBank Bank, AdlibTuneScore Tune) Load(int tuneId)
        {
            AsoundParser parser = Parser();
            var exporter = new AsoundScoreExporter();

            AdlibSoundBank bank = exporter.ExportBank(parser);
            AdlibTuneScore tune = exporter.ExportTunes(parser).Single(t => t.TuneId == tuneId);

            return (bank, tune);
        }

        /// <summary>
        /// Runs a tune for at most <paramref name="ticks"/> steps of the fast timer.
        /// </summary>
        private static (RecordingOplChip Chip, AdlibTunePlayer Player, int Ticks) Play(int tuneId, int ticks)
        {
            (AdlibSoundBank bank, AdlibTuneScore tune) = Load(tuneId);

            var chip = new RecordingOplChip();
            var player = new AdlibTunePlayer(bank, chip, AsoundScoreExporter.WorkerTickDivider, new AdlibRandomDelegate(0));
            player.Start(tune.Arrangements[0]);

            int done = 0;
            for (; done < ticks; done++)
            {
                if (!player.Tick() || player.PassCompleted) break;
            }

            return (chip, player, done);
        }

        [Fact]
        public void StartingResetsTheChipAndEnablesWaveformSelect()
        {
            (AdlibSoundBank bank, AdlibTuneScore tune) = Load(FakeAsoundModule.TunePlain);

            var chip = new RecordingOplChip();
            new AdlibTunePlayer(bank, chip, AsoundScoreExporter.WorkerTickDivider).Start(tune.Arrangements[0]);

            // Register 0x01 bit 5 must be the last thing the reset does, or the waveforms the
            // instruments select would be ignored.
            Assert.Equal(0x20, chip.Register(0x01));

            OplWrite last = chip.Writes[^1];
            Assert.Equal(0x01, last.Register);
            Assert.Equal(0x20, last.Value);

            // Every operator is muted before the first instrument arrives.
            Assert.Equal(0x3F, chip.Register(0x40));
            Assert.Equal(0x3F, chip.Register(0x43));
        }

        [Fact]
        public void InstrumentIsWrittenToBothOperatorsOfItsChannel()
        {
            (AdlibSoundBank bank, AdlibTuneScore tune) = Load(FakeAsoundModule.TunePlain);

            var chip = new RecordingOplChip();
            var player = new AdlibTunePlayer(bank, chip, AsoundScoreExporter.WorkerTickDivider, new AdlibRandomDelegate(0));
            player.Start(tune.Arrangements[0]);
            chip.Clear();

            player.Tick();

            var writes = chip.Writes.ToLookup(w => w.Register, w => w.Value);
            _output.WriteLine(string.Join(", ", chip.Writes.Select(w => $"{w.Register:X2}={w.Value:X2}")));

            // Attack 10, decay 2 for the modulator of channel 0.
            Assert.Equal(0xA2, writes[0x60].Last());

            // Sustain level 8, release 3.
            Assert.Equal(0x83, writes[0x80].Last());

            // Sustaining, multiplier 1.
            Assert.Equal(0x21, writes[0x20].Last());

            // Feedback 3 shifted up, frequency modulation means the connection bit stays clear.
            Assert.Equal(0x06, writes[0xC0].Last());

            // The chip-wide settings the driver applies with every patch.
            Assert.Equal(0xC0, chip.Register(0xBD));
            Assert.Equal(0x40, chip.Register(0x08));
        }

        [Fact]
        public void NoteOnSetsThePitchFromTheSemitoneTableAndHoldsTheKey()
        {
            (RecordingOplChip chip, _, _) = Play(FakeAsoundModule.TunePlain, 1);

            // Note 60 is semitone 0 of octave 5.
            Assert.Equal(512 & 0xFF, chip.Register(0xA0));
            Assert.Equal((5 << 2) | (512 >> 8) | KeyOn, chip.Register(0xB0));

            // The second voice plays note 48, semitone 0 of octave 4, on channel 1.
            Assert.Equal(512 & 0xFF, chip.Register(0xA1));
            Assert.Equal((4 << 2) | (512 >> 8) | KeyOn, chip.Register(0xB1));
        }

        [Fact]
        public void VolumeIsWrittenAsAttenuationOfTheCarrier()
        {
            (RecordingOplChip chip, _, _) = Play(FakeAsoundModule.TunePlain, 1);

            // Volume 0x40 is stored halved, so the carrier is attenuated by 0x3F - 0x20.
            Assert.Equal(0x3F - 0x20, chip.Register(0x43) & 0x3F);
        }

        [Fact]
        public void GateReleasesTheKeyBeforeTheNoteIsOver()
        {
            (AdlibSoundBank bank, AdlibTuneScore tune) = Load(FakeAsoundModule.TunePlain);

            var chip = new RecordingOplChip();
            var player = new AdlibTunePlayer(bank, chip, AsoundScoreExporter.WorkerTickDivider, new AdlibRandomDelegate(0));
            player.Start(tune.Arrangements[0]);

            // The first note lasts 0x20 ticks and asks to be released two ticks early.
            int sequencerTicks = 0;
            int pressedAt = -1;
            int releasedAt = -1;

            for (int tick = 0; tick < 5 * 0x20 * 2; tick++)
            {
                player.Tick();
                if (tick % AsoundScoreExporter.WorkerTickDivider != 0) continue;

                sequencerTicks++;
                bool down = (chip.Register(0xB0) & KeyOn) != 0;

                if (pressedAt < 0 && down) pressedAt = sequencerTicks;
                if (pressedAt >= 0 && releasedAt < 0 && !down) releasedAt = sequencerTicks;
            }

            _output.WriteLine($"key down on tick {pressedAt}, released on tick {releasedAt}");

            Assert.Equal(1, pressedAt);
            Assert.Equal(0x20 - 2, releasedAt - pressedAt);
        }

        [Fact]
        public void RepeatsAreFollowedAtBothLevels()
        {
            (RecordingOplChip chip, AdlibTunePlayer player, int ticks) =
                Play(FakeAsoundModule.TuneOpcodes, 5 * 4000);

            _output.WriteLine($"stopped after {ticks} fast ticks, {chip.Writes.Count} register writes");

            // The tune ends on a restart, so a full pass must have been reached.
            Assert.True(player.PassCompleted);

            // The inner block plays three times inside four passes of the outer one, so the note
            // of the inner block is keyed far more often than the tune has events.
            int keyOns = chip.Writes.Count(w => w.Register == 0xB2 && (w.Value & KeyOn) != 0);
            _output.WriteLine($"{keyOns} key-ons on channel 2");
            Assert.True(keyOns >= 12, $"Only {keyOns} notes were played.");
        }

        [Fact]
        public void PitchSlideMovesTheRunningNote()
        {
            (AdlibSoundBank bank, AdlibTuneScore tune) = Load(FakeAsoundModule.TuneOpcodes);

            var chip = new RecordingOplChip();
            var player = new AdlibTunePlayer(bank, chip, AsoundScoreExporter.WorkerTickDivider, new AdlibRandomDelegate(0));
            player.Start(tune.Arrangements[0]);

            // Run until the first note of the tune is sounding.
            while ((chip.Register(0xB2) & KeyOn) == 0) player.Tick();

            int before = ((chip.Register(0xB2) & 0x03) << 8) | chip.Register(0xA2);
            for (int tick = 0; tick < AsoundScoreExporter.WorkerTickDivider * 4; tick++) player.Tick();
            int after = ((chip.Register(0xB2) & 0x03) << 8) | chip.Register(0xA2);

            _output.WriteLine($"pitch {before} -> {after}");
            Assert.True(after > before, "The slide did not raise the pitch.");
        }

        [Fact]
        public void TheSameSeedAlwaysProducesTheSameRegisterWrites()
        {
            static List<OplWrite> Run()
            {
                (AdlibSoundBank bank, AdlibTuneScore tune) = Load(FakeAsoundModule.TunePlain);

                var chip = new RecordingOplChip();
                var player = new AdlibTunePlayer(bank, chip, AsoundScoreExporter.WorkerTickDivider, new AdlibRandomDelegate(0));
                player.Start(tune.Arrangements[0]);

                for (int tick = 0; tick < 500; tick++) player.Tick();
                return [.. chip.Writes];
            }

            Assert.Equal(Run(), Run());
        }

        [Fact]
        public void StoppingSilencesEveryChannel()
        {
            (AdlibSoundBank bank, AdlibTuneScore tune) = Load(FakeAsoundModule.TunePlain);

            var chip = new RecordingOplChip();
            var player = new AdlibTunePlayer(bank, chip, AsoundScoreExporter.WorkerTickDivider, new AdlibRandomDelegate(0));
            player.Start(tune.Arrangements[0]);

            for (int tick = 0; tick < 20; tick++) player.Tick();
            Assert.True(player.IsPlaying);

            player.Stop();

            Assert.False(player.IsPlaying);
            for (int channel = 0; channel < 9; channel++)
            {
                Assert.Equal(0, chip.Register(0xB0 + channel) & KeyOn);
            }
        }

        [Fact]
        public void TheDriversRandomGeneratorRepeatsItsOwnSequence()
        {
            var first = new AdlibRandomDelegate(0);
            var second = new AdlibRandomDelegate(0);

            var values = new List<int>();
            for (int index = 0; index < 8; index++) values.Add(first.Next());

            _output.WriteLine(string.Join(", ", values.Select(v => $"0x{v:X4}")));

            Assert.All(values, value => Assert.InRange(value, 0, 0xFFFF));
            Assert.Equal(values, Enumerable.Range(0, 8).Select(_ => second.Next()));

            // The first value is the seed of zero run through the driver's formula once.
            Assert.Equal(((0x9248 >> 3) | (0x9248 << 13)) & 0xFFFF, values[0]);
        }
    }
}
