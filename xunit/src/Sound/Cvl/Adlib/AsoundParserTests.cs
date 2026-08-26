using System.Collections.Generic;
using System.Linq;
using CivOne.Sound.Cvl;
using CivOne.Sound.Cvl.Adlib;
using Xunit;
using Xunit.Abstractions;

namespace CivOne.UnitTests.Sound.Cvl.Adlib
{
    /// <summary>
    /// Covers the ASOUND parser against a synthetic module, so it runs without any original game
    /// data. <see cref="AsoundRealModuleTests"/> adds the checks against the real driver.
    /// </summary>
    public sealed class AsoundParserTests
    {
        private readonly ITestOutputHelper _output;

        public AsoundParserTests(ITestOutputHelper output) => _output = output;

        private static CvlImage Image() => CvlImage.FromBytes(FakeAsoundModule.Build(), "fake-asound.cvl");

        private static AsoundParser Parser() => AsoundParser.Create(Image());

        [Fact]
        public void DeviceDetectorRecognisesTheAdlibDriver()
            => Assert.Equal(CvlDevice.AdLib, CvlDeviceDetector.Detect(Image()));

        [Fact]
        public void ConverterAcceptsTheModule()
        {
            var converter = new AsoundCvlConverter();

            Assert.True(converter.CanConvert(Image(), out string? reason), reason);
            Assert.Equal("adlib", converter.PackId);
            Assert.Equal(CvlDevice.AdLib, converter.Device);
        }

        [Fact]
        public void LayoutIsDerivedFromTheModuleCode()
        {
            AsoundLayout layout = Parser().Layout;

            Assert.Equal(FakeAsoundModule.DispatchTable, layout.DispatchTable);
            Assert.Equal(FakeAsoundModule.MaxTuneId, layout.MaxTuneId);
            Assert.Equal(FakeAsoundModule.VoiceCount, layout.VoiceCount);
            Assert.Equal(FakeAsoundModule.InstrumentBank, layout.InstrumentBank);
            Assert.Equal(FakeAsoundModule.InstrumentStride, layout.InstrumentStride);
            Assert.Equal(FakeAsoundModule.OperatorStride, layout.OperatorStride);
            Assert.Equal(FakeAsoundModule.ChannelOperatorTable, layout.ChannelOperatorTable);
            Assert.Equal(FakeAsoundModule.OperatorRegisterTable, layout.OperatorRegisterTable);
            Assert.Equal(FakeAsoundModule.FrequencyTable, layout.FrequencyNumberTable);
            Assert.Equal(FakeAsoundModule.DefaultPan, layout.DefaultPan);

            Assert.Equal(
                Enumerable.Range(0, FakeAsoundModule.VoiceCount).Select(FakeAsoundModule.Thunk),
                layout.VoiceThunks);
        }

        [Fact]
        public void TryCreateFailsWithAReasonWhenTheModuleIsNotAsound()
        {
            byte[] bytes = FakeAsoundModule.Build();

            // Wipe the patch loader, which is where the bank and the tables are found.
            for (int index = 0; index < 0x80; index++)
            {
                bytes[FakeAsoundModule.ImageStart + FakeAsoundModule.PatchLoader + index] = 0x90;
            }

            Assert.False(AsoundParser.TryCreate(CvlImage.FromBytes(bytes, "broken.cvl"),
                out AsoundParser? parser, out string? error));

            Assert.Null(parser);
            Assert.NotNull(error);
            _output.WriteLine(error!);
        }

        [Fact]
        public void ChipTablesAreReadBack()
        {
            AsoundParser parser = Parser();

            Assert.Equal(FakeAsoundModule.FrequencyNumbers.Select(f => (int)f), parser.ReadFrequencyNumbers());

            (bool tremolo, bool vibrato, bool noteSelect) = parser.ReadChipFlags();
            Assert.True(tremolo);
            Assert.True(vibrato);
            Assert.True(noteSelect);

            (int Modulator, int Carrier)[] operators = parser.ReadChannelOperators();
            Assert.Equal(0x00, operators[0].Modulator);
            Assert.Equal(0x03, operators[0].Carrier);
            Assert.Equal(0x08, operators[3].Modulator);
            Assert.Equal(0x0B, operators[3].Carrier);
        }

        [Fact]
        public void InstrumentsAreReadWithTheirOplFields()
        {
            List<AdlibInstrument> instruments = Parser().ReadInstruments();

            Assert.Equal(FakeAsoundModule.InstrumentCount, instruments.Count);

            AdlibInstrument first = instruments[0];
            Assert.Equal(10, first.Modulator.AttackRate);
            Assert.Equal(2, first.Modulator.DecayRate);
            Assert.Equal(0x28, first.Modulator.Level);
            Assert.Equal(1, first.Modulator.FrequencyMultiplier);
            Assert.Equal(3, first.Modulator.Feedback);
            Assert.True(first.Modulator.FrequencyModulation);
            Assert.True(first.Modulator.Sustaining);
            Assert.Equal(0x3F, first.Carrier.Level);
            Assert.False(first.IsNoise);

            AdlibInstrument noise = instruments[3];
            Assert.True(noise.IsNoise);
            Assert.Equal(0x20, noise.NoiseDuration);
            Assert.Equal(0xFF, noise.NoiseMask);
            Assert.Equal(0x200, noise.NoiseBase);
            Assert.Equal(0x10, noise.NoiseStep);
        }

        [Fact]
        public void PlainHandlerStartsTwoVoices()
        {
            AsoundTuneInfo info = Parser().ParseTune(FakeAsoundModule.TunePlain);

            Assert.Equal(TuneScoreKind.Music, info.Kind);
            Assert.Null(info.Diagnostic);
            Assert.Single(info.Arrangements);

            Assert.Equal(
                [new AsoundVoiceRef(0, FakeAsoundModule.PlainVoiceA),
                 new AsoundVoiceRef(1, FakeAsoundModule.PlainVoiceB)],
                info.Arrangements[0]);
        }

        [Fact]
        public void ArrangementSelectorYieldsFourVersions()
        {
            AsoundTuneInfo info = Parser().ParseTune(FakeAsoundModule.TuneArrangements);

            Assert.Equal(4, info.Arrangements.Count);
            Assert.All(info.Arrangements, arrangement => Assert.Single(arrangement));

            for (int index = 0; index < info.Arrangements.Count; index++)
            {
                Assert.Equal(FakeAsoundModule.ArrangementVoice + (index * 0x10),
                    info.Arrangements[index][0].DataOffset);
            }
        }

        [Fact]
        public void ReturningHandlerIsSilentAndControlFunctionIsUnsupported()
        {
            AsoundParser parser = Parser();

            Assert.Equal(TuneScoreKind.Silent, parser.ParseTune(FakeAsoundModule.TuneSilent).Kind);

            AsoundTuneInfo unsupported = parser.ParseTune(FakeAsoundModule.TuneUnsupported);
            Assert.Equal(TuneScoreKind.Unsupported, unsupported.Kind);
            Assert.Empty(unsupported.Arrangements);
            Assert.NotNull(unsupported.Diagnostic);
        }

        [Fact]
        public void HandlerWithExtraCodeStillFindsItsVoiceAndIsFlagged()
        {
            AsoundTuneInfo info = Parser().ParseTune(FakeAsoundModule.TuneNoise);

            Assert.Single(info.Arrangements);
            Assert.Equal(new AsoundVoiceRef(0, FakeAsoundModule.PlainVoiceA), info.Arrangements[0][0]);

            // The 0xC3 inside 'add bx,5' must not have been read as a return.
            Assert.NotNull(info.Diagnostic);
            _output.WriteLine(info.Diagnostic!);
        }

        [Fact]
        public void TuneIdOutsideTheDispatchTableIsRejected()
        {
            AsoundTuneInfo info = Parser().ParseTune(FakeAsoundModule.MaxTuneId + 1);

            Assert.Equal(TuneScoreKind.Unsupported, info.Kind);
            Assert.Contains("außerhalb", info.Diagnostic);
        }
    }
}
