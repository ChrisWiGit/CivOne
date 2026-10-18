using System.Collections.Generic;
using System.Linq;
using CivOne.Sound.Cvl;
using CivOne.Sound.Cvl.Adlib;
using Xunit;
using Xunit.Abstractions;

namespace CivOne.UnitTests.Sound.Cvl.Adlib
{
    /// <summary>
    /// Covers the voice stream decoder: every opcode, both loop levels, and the two ways a stream
    /// can end.
    /// </summary>
    public sealed class AdlibBytecodeDecoderTests
    {
        private readonly ITestOutputHelper _output;

        public AdlibBytecodeDecoderTests(ITestOutputHelper output) => _output = output;

        private static AsoundParser Parser()
            => AsoundParser.Create(CvlImage.FromBytes(FakeAsoundModule.Build(), "fake-asound.cvl"));

        [Fact]
        public void NoteRecordsCarryPitchAndDuration()
        {
            List<AdlibEvent> events = Parser().DecodeVoice(FakeAsoundModule.PlainVoiceA);

            _output.WriteLine(string.Join("\n", events.Select(e => $"{e.Kind} {e.Note}/{e.Duration}/{e.Value}")));

            Assert.Equal(AdlibEventKind.SetInstrument, events[0].Kind);
            Assert.Equal(0, events[0].Value);

            Assert.Equal(AdlibEventKind.SetVolume, events[1].Kind);
            Assert.Equal(0x40, events[1].Value);

            Assert.Equal(AdlibEventKind.SetGate, events[2].Kind);
            Assert.Equal(2, events[2].Value);

            AdlibEvent note = events[3];
            Assert.Equal(AdlibEventKind.Note, note.Kind);
            Assert.Equal(60, note.Note);
            Assert.Equal(0x20, note.Duration);
            Assert.False(note.IsRest);

            Assert.True(events[6].IsRest);
        }

        [Fact]
        public void AZeroDurationEndsTheStream()
        {
            List<AdlibEvent> events = Parser().DecodeVoice(FakeAsoundModule.PlainVoiceA);

            AdlibEvent last = events[^1];
            Assert.Equal(AdlibEventKind.Note, last.Kind);
            Assert.Equal(0, last.Duration);

            // Nothing behind the terminator is read, so the next voice's data stays out of it.
            Assert.DoesNotContain(events, e => e.SourceOffset >= FakeAsoundModule.PlainVoiceB);
        }

        [Fact]
        public void RestartEndsTheStreamBecauseNothingBehindItIsEverRead()
        {
            List<AdlibEvent> events = Parser().DecodeVoice(FakeAsoundModule.OpcodeVoice);

            Assert.Equal(AdlibEventKind.Restart, events[^1].Kind);
            Assert.DoesNotContain(events, e => e.SourceOffset >= FakeAsoundModule.ArrangementVoice);
        }

        [Fact]
        public void EveryControlOpcodeIsDecodedWithItsOperands()
        {
            List<AdlibEvent> events = Parser().DecodeVoice(FakeAsoundModule.OpcodeVoice);

            foreach (AdlibEvent decoded in events)
            {
                _output.WriteLine($"0x{decoded.SourceOffset:X4} {decoded.Kind} "
                                  + $"note={decoded.Note} dur={decoded.Duration} "
                                  + $"value={decoded.Value} delta={decoded.Delta}");
            }

            AdlibEvent Single(AdlibEventKind kind) => events.Single(e => e.Kind == kind);

            Assert.Equal(2, Single(AdlibEventKind.SetInstrument).Value);
            Assert.Equal(0x40, Single(AdlibEventKind.SetPan).Value);
            Assert.Equal(0x10, Single(AdlibEventKind.SetVolumeOffset).Value);
            Assert.Equal(0x40, Single(AdlibEventKind.SetVolume).Value);
            Assert.Equal(5, Single(AdlibEventKind.SetDetune).Value);
            Assert.Equal(1, Single(AdlibEventKind.SetGate).Value);
            Assert.Equal(2, Single(AdlibEventKind.SetPitchSlide).Value);

            AdlibEvent volume = Single(AdlibEventKind.VolumeEnvelope);
            Assert.Equal(4, volume.Value);
            Assert.Equal(1, volume.Delta);

            AdlibEvent pan = Single(AdlibEventKind.PanEnvelope);
            Assert.Equal(8, pan.Value);
            Assert.Equal(0xFF, pan.Delta);

            // Both loop levels appear twice: once to mark the block, once to repeat it.
            Assert.Equal(2, events.Count(e => e.Kind == AdlibEventKind.LoopOuter));
            Assert.Equal(2, events.Count(e => e.Kind == AdlibEventKind.LoopInner));

            Assert.Equal([0, 3], events.Where(e => e.Kind == AdlibEventKind.LoopOuter).Select(e => e.Value));
            Assert.Equal([0, 2], events.Where(e => e.Kind == AdlibEventKind.LoopInner).Select(e => e.Value));
        }

        [Fact]
        public void RandomVariantResolvesToTheByteItPatches()
        {
            List<AdlibEvent> events = Parser().DecodeVoice(FakeAsoundModule.OpcodeVoice);
            AdlibEvent variant = events.Single(e => e.Kind == AdlibEventKind.RandomVariant);

            Assert.Equal([65, 67], variant.Choices);
            Assert.True(variant.TargetEventIndex.HasValue, "The patched byte was not resolved.");

            AdlibEvent target = events[variant.TargetEventIndex!.Value];
            _output.WriteLine($"patches event {variant.TargetEventIndex} ({target.Kind}) "
                              + $"field {variant.TargetField}");

            Assert.Equal(AdlibEventKind.Note, target.Kind);
            Assert.Equal(AdlibEventField.Note, variant.TargetField);
            Assert.Equal(66, target.Note);
        }

        [Fact]
        public void EveryEventKeepsTheOffsetItCameFrom()
        {
            List<AdlibEvent> events = Parser().DecodeVoice(FakeAsoundModule.OpcodeVoice);

            Assert.Equal(FakeAsoundModule.OpcodeVoice, events[0].SourceOffset);

            for (int index = 1; index < events.Count; index++)
            {
                Assert.True(events[index].SourceOffset > events[index - 1].SourceOffset,
                    $"Event {index} does not lie behind its predecessor.");
            }
        }

        [Fact]
        public void AnOffsetOutsideTheFileDecodesToNothing()
            => Assert.Empty(Parser().DecodeVoice(0x7FFF));
    }
}
