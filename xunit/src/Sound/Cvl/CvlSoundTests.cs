using CivOne.Sound.Cvl;
using Xunit;

namespace CivOne.UnitTests.Sound.Cvl
{
    public class CvlSoundTests
    {
        // ── Hilfsmethode: Beispiel-SoundPack ────────────────────────────────────

        private static SoundPack BuildSamplePack() => new SoundPack
        {
            SchemaVersion = 1,
            Id = "civdos-adlib-pack",
            DisplayName = "Civilization DOS - AdLib Pack",
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
                        new SoundEvent { T = 0, Type = SoundEventType.Worker, Kind = "sound" },
                        new SoundEvent { T = 1, Type = SoundEventType.PortWrite, Port = 0x388, Value = 0x20 },
                        new SoundEvent { T = 16, Type = SoundEventType.Interrupt, IntNo = 0x08 }
                    ]
                },
                new TuneDefinition
                {
                    TuneId = 4,
                    Title = "Evolution Music",
                    EndlessLoop = false,
                    Events =
                    [
                        new SoundEvent { T = 0, Type = SoundEventType.Worker, Kind = "sound" },
                        new SoundEvent { T = 2, Type = SoundEventType.PortWrite, Port = 0x389, Value = 0x7F }
                    ]
                }
            ]
        };

        // ── SoundPack Schema ─────────────────────────────────────────────────────

        [Fact]
        public void SchemaRoundtrip_LoadedPackMatchesSaved()
        {
            var pack = BuildSamplePack();
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "phase1-roundtrip.sound.json");

            SoundPackJson.Save(path, pack);
            var loaded = SoundPackJson.Load(path);

            Assert.Equal(2, loaded.Tunes.Count);
            Assert.Equal("Title Music", loaded.Tunes[0].Title);
            Assert.Equal("Evolution Music", loaded.Tunes[1].Title);
        }

        [Fact]
        public void SchemaRoundtrip_EndlessLoopPreserved()
        {
            var pack = BuildSamplePack();
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "phase1-loop.sound.json");

            SoundPackJson.Save(path, pack);
            var loaded = SoundPackJson.Load(path);

            Assert.True(loaded.Tunes[0].EndlessLoop);   // Tune 3: endlessLoop=true
            Assert.False(loaded.Tunes[1].EndlessLoop);  // Tune 4: endlessLoop=false
        }

        [Fact]
        public void Validation_ThrowsOnDuplicateTuneId()
        {
            var pack = BuildSamplePack();
            pack.Tunes.Add(new TuneDefinition { TuneId = 3, Title = "Duplicate", Events = [] });

            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "phase1-invalid.sound.json");

            Assert.Throws<System.InvalidOperationException>(() => SoundPackJson.Save(path, pack));
        }

        // ── CivPlayScheduler ─────────────────────────────────────────────────────

        [Fact]
        public void Scheduler_SoundWorkerEvery5Ticks()
        {
            var events = CivPlayScheduler.BuildWorkerTimeline(ticks: 32);
            var soundWorkers = events.FindAll(x => x.Type == SoundEventType.Worker && x.Kind == "sound");

            // 32 ticks / 5 = 6 sound workers
            Assert.Equal(6, soundWorkers.Count);
        }

        [Fact]
        public void Scheduler_FastWorkerOnlyAfterNonZeroReturn()
        {
            // Ohne non-zero return: kein FastWorker
            var noFast = CivPlayScheduler.BuildWorkerTimeline(ticks: 32, workerReturnNonZero: _ => false);
            Assert.Empty(noFast.FindAll(x => x.Kind == "fast"));

            // Mit non-zero return ab Tick 5: FastWorker danach vorhanden
            var withFast = CivPlayScheduler.BuildWorkerTimeline(ticks: 32, workerReturnNonZero: t => t == 5);
            Assert.NotEmpty(withFast.FindAll(x => x.Kind == "fast"));
        }

        [Fact]
        public void Scheduler_OldInt8ChainedEvery16Ticks()
        {
            var events = CivPlayScheduler.BuildWorkerTimeline(ticks: 32);
            var interrupts = events.FindAll(x => x.Type == SoundEventType.Interrupt && x.IntNo == 0x08);

            // 32 ticks / 16 = 2 BIOS-Ketten
            Assert.Equal(2, interrupts.Count);
        }

        // ── EventPlayer ──────────────────────────────────────────────────────────

        [Fact]
        public void EventPlayer_EndlessLoopRepeatsEvents()
        {
            var pack = BuildSamplePack();
            var sink = new CollectingEventSink();
            var player = new EventPlayer();

            player.PlayTune(pack, tuneId: 3, sink, loopIterations: 2);

            // Tune 3 hat 3 Events, 2 Iterationen => 6 Calls
            Assert.Equal(6, sink.Calls.Count);
        }

        [Fact]
        public void EventPlayer_NoLoopPlaysOnce()
        {
            var pack = BuildSamplePack();
            var sink = new CollectingEventSink();
            var player = new EventPlayer();

            player.PlayTune(pack, tuneId: 4, sink, loopIterations: 99);

            // Tune 4: endlessLoop=false => trotz loopIterations=99 nur 1 Durchlauf
            Assert.Equal(2, sink.Calls.Count);
        }

        [Fact]
        public void EventPlayer_DeterministicReplay()
        {
            var pack = BuildSamplePack();
            var player = new EventPlayer();

            var sink1 = new CollectingEventSink();
            player.PlayTune(pack, tuneId: 4, sink1, loopIterations: 3);

            var sink2 = new CollectingEventSink();
            player.PlayTune(pack, tuneId: 4, sink2, loopIterations: 3);

            Assert.Equal(string.Join("|", sink1.Calls), string.Join("|", sink2.Calls));
        }

        [Fact]
        public void EventPlayer_UnknownTuneThrows()
        {
            var pack = BuildSamplePack();
            var sink = new CollectingEventSink();
            var player = new EventPlayer();

            Assert.Throws<System.InvalidOperationException>(() =>
                player.PlayTune(pack, tuneId: 99, sink));
        }

        [Fact]
        public void EventPlayer_SequenceNumbersMonotonicallyIncreasing()
        {
            var pack = BuildSamplePack();
            var player = new EventPlayer();

            ulong lastSeq = 0;
            var sink = new DelegateSink(ctx =>
            {
                Assert.True(ctx.Sequence > lastSeq);
                lastSeq = ctx.Sequence;
            });

            player.PlayTune(pack, tuneId: 3, sink, loopIterations: 2);
        }

        [Fact]
        public void EventPlayer_VirtualTimeNsNonNegative()
        {
            var pack = BuildSamplePack();
            var player = new EventPlayer();

            var sink = new DelegateSink(ctx => Assert.True(ctx.VirtualTimeNs >= 0));
            player.PlayTune(pack, tuneId: 3, sink);
        }

                [Fact]
                public void EventPlayer_EndlessLoopWithZeroIterations_PlaysOnce()
                {
                        var pack = BuildSamplePack();
                        var sink = new CollectingEventSink();
                        var player = new EventPlayer();

                        player.PlayTune(pack, tuneId: 3, sink, loopIterations: 0);

                        Assert.Equal(3, sink.Calls.Count);
                }

                [Fact]
                public void Validation_ThrowsOnEmptyEvents()
                {
                        var pack = BuildSamplePack();
                        pack.Tunes[0].Events.Clear();
                        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "phase1-empty-events.sound.json");

                        var ex = Assert.Throws<System.InvalidOperationException>(() => SoundPackJson.Save(path, pack));
                        Assert.Contains("keine events", ex.Message);
                }

                [Fact]
                public void Validation_ThrowsOnInvalidPortValue()
                {
                        var pack = BuildSamplePack();
                        pack.Tunes[0].Events[1].Value = 0x1FF;
                        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "phase1-invalid-port-value.sound.json");

                        var ex = Assert.Throws<System.InvalidOperationException>(() => SoundPackJson.Save(path, pack));
                        Assert.Contains("portWrite", ex.Message);
                }

                [Fact]
                public void Validation_ThrowsOnUnknownEventTypeFromJson()
                {
                        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "phase1-unknown-event.sound.json");
                        var json = """
                        {
                            "schemaVersion": 1,
                            "id": "bad-type-pack",
                            "displayName": "Bad Type",
                            "format": "opl2",
                            "tickRate": 300,
                            "tunes": [
                                {
                                    "tuneId": 3,
                                    "title": "Title",
                                    "endlessLoop": true,
                                    "events": [
                                        { "t": 0, "type": "noteEvent", "kind": "x" }
                                    ]
                                }
                            ]
                        }
                        """;
                        System.IO.File.WriteAllText(path, json);

                        Assert.Throws<System.Text.Json.JsonException>(() => SoundPackJson.Load(path));
                }
    }

    // ── Test-Hilfsklasse ─────────────────────────────────────────────────────────

    internal sealed class DelegateSink(System.Action<EventContext> onEvent) : IEventSink
    {
        public void OnWorkerCall(string kind, EventContext context) => onEvent(context);
        public void OnPortWrite(int port, int value, EventContext context) => onEvent(context);
        public void OnInterrupt(int intNo, EventContext context) => onEvent(context);
    }
}
