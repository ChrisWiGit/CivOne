using System;
using System.Collections.Generic;
using System.Linq;
using CivOne.Sound.Engine;
using Xunit;

namespace CivOne.UnitTests.Sound.Engine
{
    /// <summary>
    /// Covers the runtime mixer sample by sample, without a sound card.
    /// </summary>
    public sealed class SoundMixerTests
    {
        private const int SampleRate = 1000;

        /// <summary>
        /// Pausing must freeze the read position, not merely silence the voice.
        /// </summary>
        /// <remarks>
        /// This is the behaviour the original driver had when it ducked a theme for a sting: the
        /// theme continued on the very note it stopped on. A voice that kept advancing silently
        /// would come back shifted by the whole length of whatever played over it.
        /// </remarks>
        [Fact]
        public void PauseFreezesThePositionInsteadOfSilencingTheVoice()
        {
            float[] samples = Ramp(100);
            (SoundMixer mixer, SoundHandle handle) = Start(samples);

            float[] before = Pull(mixer, 10);
            Assert.Equal(samples.Take(10), before);

            handle.Pause();
            float[] whilePaused = Pull(mixer, 10);
            Assert.All(whilePaused, sample => Assert.Equal(0f, sample));

            handle.Resume();
            float[] after = Pull(mixer, 10);

            Assert.Equal(samples.Skip(10).Take(10), after);
            Assert.False(handle.IsPaused);
        }

        /// <summary>
        /// A loop must turn around at its loop point, not at the end of the file.
        /// </summary>
        /// <remarks>
        /// For a converted tune the end of the file is the wrong place: the render runs until every
        /// voice of the tune has wrapped once, so the tail already repeats the beginning.
        /// </remarks>
        [Fact]
        public void LoopTurnsAroundAtTheLoopPointAndNotAtTheEndOfTheFile()
        {
            float[] samples = Ramp(100);
            (SoundMixer mixer, SoundHandle _) = Start(samples, voice =>
            {
                voice.Loop = true;
                voice.LoopStart = 0;
                voice.LoopEnd = 10;
            });

            float[] output = Pull(mixer, 25);

            for (int index = 0; index < output.Length; index++)
            {
                Assert.Equal(samples[index % 10], output[index]);
            }
        }

        /// <summary>
        /// The overlap at a loop turnaround has to keep the level up instead of dipping.
        /// </summary>
        [Fact]
        public void LoopCrossFadeKeepsTheLevelAcrossTheTurnaround()
        {
            float[] samples = Enumerable.Repeat(0.5f, 100).ToArray();
            (SoundMixer mixer, SoundHandle _) = Start(samples, voice =>
            {
                voice.Loop = true;
                voice.LoopStart = 0;
                voice.LoopEnd = 50;
                voice.LoopCrossFade = 10;
            });

            float[] output = Pull(mixer, 70);

            Assert.All(output, sample => Assert.InRange(sample, 0.4f, 0.6f));
        }

        /// <summary>
        /// Commands are applied on the mixer's next pass, and none of them is dropped when several
        /// arrive between two passes.
        /// </summary>
        [Fact]
        public void CommandsQueuedBetweenTwoPassesAreAllApplied()
        {
            float[] samples = Enumerable.Repeat(1f, 100).ToArray();
            (SoundMixer mixer, SoundHandle handle) = Start(samples);

            handle.Pause();
            handle.Resume();

            float[] output = Pull(mixer, 5);

            Assert.All(output, sample => Assert.True(sample > 0f));
            Assert.False(handle.IsPaused);
        }

        /// <summary>
        /// A voice that runs out reports itself as ended, so the game thread can raise its event.
        /// </summary>
        [Fact]
        public void AVoiceThatRunsOutIsReportedAsEnded()
        {
            float[] samples = Ramp(8);
            (SoundMixer mixer, SoundHandle handle) = Start(samples);

            Pull(mixer, 16);

            Assert.True(mixer.TryTakeEnded(out SoundHandle? ended));
            Assert.Same(handle, ended);
            Assert.False(handle.IsPlaying);
        }

        /// <summary>
        /// The bus volume scales everything on that bus and leaves the other bus alone.
        /// </summary>
        [Fact]
        public void BusVolumeScalesOnlyItsOwnBus()
        {
            float[] samples = Enumerable.Repeat(0.4f, 50).ToArray();

            var mixer = new SoundMixer();
            AddVoice(mixer, samples, SoundBus.Music);
            AddVoice(mixer, samples, SoundBus.Effect);

            mixer.Post(MixerCommand.SetBusVolume(SoundBus.Music, 0f));

            float[] output = Pull(mixer, 5);

            Assert.All(output, sample => Assert.Equal(0.4f, sample, 3));
        }

        /// <summary>
        /// A handler added after the sound has already ended is called at once.
        /// </summary>
        /// <remarks>
        /// Otherwise a caller that starts a sound and subscribes afterwards could miss the event -
        /// and the one that waits for it to bring ducked music back would leave it frozen for good.
        /// </remarks>
        [Fact]
        public void EndedReachesAHandlerThatSubscribesAfterTheSoundHasEnded()
        {
            (SoundMixer mixer, SoundHandle handle) = Start(Ramp(4));

            Pull(mixer, 8);
            while (mixer.TryTakeEnded(out SoundHandle? ended))
            {
                ended?.RaiseEnded();
            }

            bool raised = false;
            handle.Ended += () => raised = true;

            Assert.True(raised);
        }

        /// <summary>
        /// A voice that is fading out to be removed must not be revived by a late resume.
        /// </summary>
        /// <remarks>
        /// This is the case a sting that is cut short produces: its end resumes the music it ducked,
        /// which by then may already have been told to stop.
        /// </remarks>
        [Fact]
        public void ResumeDoesNotReviveAVoiceThatIsAlreadyStopping()
        {
            float[] samples = Enumerable.Repeat(0.5f, 200).ToArray();
            (SoundMixer mixer, SoundHandle handle) = Start(samples);

            handle.Pause();
            Pull(mixer, 1);

            handle.Stop(TimeSpan.FromSeconds(1));
            handle.Resume();

            float[] output = Pull(mixer, 20);

            Assert.All(output, sample => Assert.Equal(0f, sample));
        }

        /// <summary>
        /// A cross fade longer than the loop itself must not eat the loop away.
        /// </summary>
        /// <remarks>
        /// The head would otherwise still be fading in when it reached the next turnaround: it
        /// would never reach full volume, and every wrap would leave another tail behind until
        /// there were none left to leave.
        /// </remarks>
        [Fact]
        public void ACrossFadeLongerThanTheLoopIsCutDownToIt()
        {
            float[] samples = [.. Enumerable.Repeat(0.5f, 40)];

            (SoundMixer mixer, SoundHandle handle) = Start(samples, voice =>
            {
                voice.Loop = true;
                voice.LoopStart = 0;
                voice.LoopEnd = 10;
                voice.LoopCrossFade = 1000;
            });

            // Far more turnarounds than there are tails to hand out.
            float[] output = Pull(mixer, 400);

            Assert.True(handle.IsPlaying);
            Assert.Contains(output, sample => sample > 0.45f);
            Assert.All(output, sample => Assert.InRange(sample, 0f, 1f));
        }

        /// <summary>
        /// The mixer takes only as many voices as it was built for.
        /// </summary>
        /// <remarks>
        /// Its list is never allowed to grow, because growing it would allocate on the audio
        /// thread. A voice that finds no room ends straight away rather than being left pending,
        /// so nothing on the game thread waits for a sound that was never started.
        /// </remarks>
        [Fact]
        public void AVoiceThatFindsNoRoomEndsInsteadOfBeingLeftPending()
        {
            var mixer = new SoundMixer();
            var handles = new List<SoundHandle>();

            for (int index = 0; index < 64; index++)
            {
                handles.Add(AddVoice(mixer, Ramp(1000), SoundBus.Effect));
            }

            Pull(mixer, 10);

            Assert.Contains(handles, handle => !handle.IsPlaying);
            Assert.All(handles.Take(32), handle => Assert.True(handle.IsPlaying));
        }

        private static float[] Ramp(int length)
            => [.. Enumerable.Range(0, length).Select(value => value / 1000f)];

        private static (SoundMixer mixer, SoundHandle handle) Start(float[] samples,
            Action<MixerVoice>? configure = null)
        {
            var mixer = new SoundMixer();
            SoundHandle handle = AddVoice(mixer, samples, SoundBus.Music, configure);

            return (mixer, handle);
        }

        private static SoundHandle AddVoice(SoundMixer mixer, float[] samples, SoundBus bus,
            Action<MixerVoice>? configure = null)
        {
            var handle = new SoundHandle(mixer.NextHandleId(), mixer, SampleRate);
            var voice = new MixerVoice
            {
                Samples = samples,
                Handle = handle,
                Bus = bus,
                LoopEnd = samples.Length
            };

            configure?.Invoke(voice);
            mixer.Post(MixerCommand.Add(voice));

            return handle;
        }

        private static float[] Pull(SoundMixer mixer, int count)
        {
            var buffer = new float[count];
            mixer.Render(buffer);

            return buffer;
        }
    }
}
