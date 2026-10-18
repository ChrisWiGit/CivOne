using CivOne.Sound;
using CivOne.Sound.Engine;
using CivOne.UnitTests.Sound.Engine;
using Xunit;

namespace CivOne.UnitTests.Sound
{
    /// <summary>
    /// Covers playing a short sound over running music the way the original driver did.
    /// </summary>
    public sealed class MusicStingDelegateTests
    {
        /// <summary>
        /// The music is frozen while the sting plays and comes back once it has ended.
        /// </summary>
        /// <remarks>
        /// Freezing rather than silencing is the whole point: a theme that kept running silently
        /// would come back shifted by the length of the sting.
        /// </remarks>
        [Fact]
        public void FreezesTheMusicForTheLengthOfTheStingAndBringsItBack()
        {
            var music = new FakeSoundHandle();
            var sting = new FakeSoundHandle();
            var delegateUnderTest = new MusicStingDelegate(_ => sting);

            ISoundHandle? started = delegateUnderTest.Play(music, "sting.wav");

            Assert.Same(sting, started);
            Assert.Equal(1, music.PauseCount);
            Assert.True(music.IsPaused);
            Assert.Equal(0, music.ResumeCount);

            sting.RaiseEnded();

            Assert.Equal(1, music.ResumeCount);
            Assert.False(music.IsPaused);
        }

        /// <summary>
        /// A sting that is cut short brings the music back just the same.
        /// </summary>
        [Fact]
        public void BringsTheMusicBackWhenTheStingIsStoppedEarly()
        {
            var music = new FakeSoundHandle();
            var sting = new FakeSoundHandle();

            new MusicStingDelegate(_ => sting).Play(music, "sting.wav");
            sting.Stop();

            Assert.Equal(1, music.ResumeCount);
        }

        /// <summary>
        /// A sting that could not be started must leave the music running untouched.
        /// </summary>
        [Fact]
        public void LeavesTheMusicAloneWhenTheStingCannotBeStarted()
        {
            var music = new FakeSoundHandle();

            ISoundHandle? started = new MusicStingDelegate(_ => null).Play(music, "missing.wav");

            Assert.Null(started);
            Assert.Equal(0, music.PauseCount);
            Assert.False(music.IsPaused);
        }

        /// <summary>
        /// Without running music the sting simply plays on its own.
        /// </summary>
        [Fact]
        public void PlaysTheStingOnItsOwnWhenNoMusicIsRunning()
        {
            var sting = new FakeSoundHandle();

            ISoundHandle? started = new MusicStingDelegate(_ => sting).Play(null, "sting.wav");

            Assert.Same(sting, started);
        }
    }
}
