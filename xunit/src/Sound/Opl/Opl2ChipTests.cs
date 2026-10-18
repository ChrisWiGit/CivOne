using System;
using System.Linq;
using CivOne.Sound.Opl;
using Xunit;
using Xunit.Abstractions;

namespace CivOne.UnitTests.Sound.Opl
{
    /// <summary>
    /// Covers the OPL2 core: that it plays the pitch the registers ask for, that the envelope
    /// behaves, and that the waveform settings do what the chip documentation says.
    /// </summary>
    public sealed class Opl2ChipTests
    {
        private const int Channel = 0;
        private const int Modulator = 0x00;
        private const int Carrier = 0x03;

        private readonly ITestOutputHelper _output;

        public Opl2ChipTests(ITestOutputHelper output) => _output = output;

        /// <summary>
        /// Sets up channel 0 as a plain sine: additive connection, modulator muted, carrier at full
        /// level with an instant attack and no decay.
        /// </summary>
        private static Opl2Chip CreateSineChip(int frequencyNumber, int block, int waveform = 0)
        {
            var chip = new Opl2Chip();

            chip.WriteRegister(0x01, 0x20);                     // enable waveform select
            chip.WriteRegister(0x20 + Modulator, 0x01);         // multiplier 1
            chip.WriteRegister(0x20 + Carrier, 0x21);           // multiplier 1, sustaining
            chip.WriteRegister(0x40 + Modulator, 0x3F);         // modulator silent
            chip.WriteRegister(0x40 + Carrier, 0x00);           // carrier at full level
            chip.WriteRegister(0x60 + Modulator, 0xF0);
            chip.WriteRegister(0x60 + Carrier, 0xF0);           // fastest attack, no decay
            chip.WriteRegister(0x80 + Modulator, 0x0F);
            chip.WriteRegister(0x80 + Carrier, 0x0F);           // sustain at full, fast release
            chip.WriteRegister(0xE0 + Carrier, waveform);
            chip.WriteRegister(0xC0 + Channel, 0x01);           // additive, no feedback

            chip.WriteRegister(0xA0 + Channel, frequencyNumber & 0xFF);
            chip.WriteRegister(0xB0 + Channel, 0x20 | ((block & 7) << 2) | ((frequencyNumber >> 8) & 3));

            return chip;
        }

        private static float[] Render(Opl2Chip chip, int samples)
        {
            var buffer = new float[samples];
            chip.Render(buffer);
            return buffer;
        }

        /// <summary>
        /// Counts rising zero crossings, which for a clean tone equals the frequency in Hz when the
        /// buffer covers exactly one second.
        /// </summary>
        private static int CountRisingCrossings(ReadOnlySpan<float> samples)
        {
            int crossings = 0;
            for (int index = 1; index < samples.Length; index++)
            {
                if (samples[index - 1] <= 0f && samples[index] > 0f) crossings++;
            }

            return crossings;
        }

        [Fact]
        public void AmplitudeTableSpansTheFullOperatorRange()
        {
            Assert.Equal(4084, OplTables.Amplitude(0));
            Assert.True(OplTables.Amplitude(OplTables.MaxAttenuation) <= 1);

            // Attenuation is logarithmic: 256 units is one halving.
            Assert.InRange(OplTables.Amplitude(256), 2030, 2060);
            Assert.InRange(OplTables.Amplitude(512), 1010, 1030);
        }

        [Fact]
        public void KeyScaleLevelTableMatchesTheChipsOwnValues()
        {
            int[] expected = [0, 32, 40, 45, 48, 51, 53, 55, 56, 58, 59, 60, 61, 62, 63, 64];
            Assert.Equal(expected, OplTables.KeyScaleLevel);
        }

        [Fact]
        public void LogSineIsSilentAtZeroAndLoudestAtTheQuarterCycle()
        {
            int[] logSine = OplTables.LogSine;

            Assert.Equal(256, logSine.Length);
            Assert.Equal(0, logSine[^1]);
            Assert.True(logSine[0] > 1800, $"sin(0) should be far quieter, got {logSine[0]}.");

            for (int index = 1; index < logSine.Length; index++)
            {
                Assert.True(logSine[index] <= logSine[index - 1], $"Attenuation rises again at {index}.");
            }
        }

        [Fact]
        public void ChipIsSilentUntilAKeyIsPressed()
        {
            var chip = new Opl2Chip();
            chip.WriteRegister(0x40 + Carrier, 0x00);

            Assert.False(chip.IsActive);
            Assert.All(Render(chip, 512), sample => Assert.Equal(0f, sample));
        }

        [Theory]
        [InlineData(512, 4, 388)]
        [InlineData(512, 5, 777)]
        [InlineData(767, 5, 1163)]
        [InlineData(1023, 3, 388)]
        public void ChannelPlaysTheFrequencyTheRegistersAskFor(int frequencyNumber, int block, int expected)
        {
            Opl2Chip chip = CreateSineChip(frequencyNumber, block);

            // Let the attack settle before measuring.
            Render(chip, 4096);
            float[] samples = Render(chip, Opl2Chip.NativeSampleRate);

            int measured = CountRisingCrossings(samples);
            _output.WriteLine($"fnum={frequencyNumber} block={block}: expected {expected} Hz, measured {measured} Hz");

            Assert.InRange(measured, expected - 2, expected + 2);
        }

        [Fact]
        public void HighestTotalLevelAttenuatesByAboutFortySevenDecibels()
        {
            Opl2Chip loud = CreateSineChip(512, 5);
            Render(loud, 4096);
            float loudPeak = Render(loud, 4096).Max(Math.Abs);

            Opl2Chip quiet = CreateSineChip(512, 5);
            quiet.WriteRegister(0x40 + Carrier, 0x3F);
            Render(quiet, 4096);
            float quietPeak = Render(quiet, 4096).Max(Math.Abs);

            double decibels = 20d * Math.Log10(loudPeak / quietPeak);
            _output.WriteLine($"peak {loudPeak} at level 0, {quietPeak} at level 0x3F -> {decibels:F1} dB");

            // A total level of 0x3F is 63 steps of 0.75 dB.
            Assert.InRange(decibels, 45d, 49d);
            Assert.InRange(loudPeak, 0.98f, 1.01f);
        }

        [Fact]
        public void ReleaseFadesTheChannelOutAndDeactivatesIt()
        {
            Opl2Chip chip = CreateSineChip(512, 5);

            Render(chip, 4096);
            float sounding = Render(chip, 4096).Max(Math.Abs);
            Assert.True(sounding > 0.5f, $"The note is too quiet to test the release: {sounding}");

            // Key off, keeping the block and F-number.
            chip.WriteRegister(0xB0 + Channel, (5 << 2) | 0x02);

            float[] tail = Render(chip, Opl2Chip.NativeSampleRate / 4);
            float peak = tail.AsSpan(tail.Length / 2).ToArray().Max(Math.Abs);

            _output.WriteLine($"peak while sounding {sounding}, peak after release {peak}");
            Assert.True(peak < sounding / 100f, $"The release left {peak} behind.");
            Assert.False(chip.IsActive);
        }

        [Fact]
        public void SlowerRatesTakeLongerToFadeOut()
        {
            static int SamplesUntilSilent(int releaseRate)
            {
                Opl2Chip chip = CreateSineChip(512, 5);
                chip.WriteRegister(0x80 + Carrier, releaseRate);
                Render(chip, 4096);
                chip.WriteRegister(0xB0 + Channel, (5 << 2) | 0x02);

                var buffer = new float[256];
                for (int block = 0; block < 4000; block++)
                {
                    chip.Render(buffer);
                    if (!chip.IsActive) return block * buffer.Length;
                }

                return int.MaxValue;
            }

            int fast = SamplesUntilSilent(0x0F);
            int medium = SamplesUntilSilent(0x08);
            int slow = SamplesUntilSilent(0x04);

            _output.WriteLine($"release 15: {fast} samples, release 8: {medium}, release 4: {slow}");

            Assert.True(fast < medium, "A release rate of 15 should be faster than 8.");
            Assert.True(medium < slow, "A release rate of 8 should be faster than 4.");

            // Each step of four in the rate roughly halves the time.
            Assert.InRange(slow / (double)medium, 8d, 40d);
        }

        [Fact]
        public void HalfAndPulseWaveformsNeverGoNegative()
        {
            foreach (int waveform in new[] { 1, 2, 3 })
            {
                Opl2Chip chip = CreateSineChip(512, 5, waveform);
                Render(chip, 4096);

                float[] samples = Render(chip, 4096);
                float minimum = samples.Min();
                float maximum = samples.Max();

                _output.WriteLine($"waveform {waveform}: min {minimum}, max {maximum}");

                Assert.True(minimum >= 0f, $"Waveform {waveform} produced {minimum}.");
                Assert.True(maximum > 0.01f, $"Waveform {waveform} produced nothing.");
            }
        }

        [Fact]
        public void WaveformSelectIsIgnoredUntilItIsEnabled()
        {
            var chip = new Opl2Chip();
            chip.WriteRegister(0xE0 + Carrier, 3);
            Assert.Equal(3, chip.ReadRegister(0xE0 + Carrier));

            // Without register 0x01 bit 5 the chip stays on the plain sine, so the wave is
            // symmetric around zero.
            Opl2Chip plain = CreateSineChip(512, 5);
            plain.WriteRegister(0x01, 0x00);
            plain.WriteRegister(0xE0 + Carrier, 3);

            Render(plain, 4096);
            float[] samples = Render(plain, 4096);

            Assert.True(samples.Min() < -0.01f, "The waveform select was applied although it is disabled.");
        }

        [Fact]
        public void ModulationBendsTheCarrierAndAddsHarmonics()
        {
            static float Energy(Opl2Chip chip)
            {
                Render(chip, 4096);
                float[] samples = Render(chip, 8192);
                float sum = 0f;
                foreach (float sample in samples) sum += sample * sample;
                return sum;
            }

            Opl2Chip plain = CreateSineChip(512, 5);
            float plainCrossings = CountRisingCrossings(Render(plain, Opl2Chip.NativeSampleRate));

            Opl2Chip modulated = CreateSineChip(512, 5);
            modulated.WriteRegister(0xC0 + Channel, 0x00);      // frequency modulation
            modulated.WriteRegister(0x20 + Modulator, 0x21);    // let the modulator hold its level
            modulated.WriteRegister(0x40 + Modulator, 0x00);    // modulator at full level

            float[] samples = Render(modulated, Opl2Chip.NativeSampleRate);
            float modulatedCrossings = CountRisingCrossings(samples);

            _output.WriteLine($"plain {plainCrossings} crossings, modulated {modulatedCrossings}");

            Assert.True(Energy(plain) > 0f);
            Assert.True(modulatedCrossings > plainCrossings,
                "Frequency modulation should add zero crossings, not remove them.");
        }
    }
}
