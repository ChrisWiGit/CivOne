using System.Collections.Generic;
using CivOne.Sound;
using CivOne.Sound.Playback;
using Xunit;

namespace CivOne.UnitTests.Sound.Playback
{
    /// <summary>
    /// Covers redirecting one sound name to another, which is how a plugin replaces a sound it does
    /// not own.
    /// </summary>
    public sealed class SoundAliasTests
    {
        private readonly SoundAliasRegistry _registryUnderTest = new();

        /// <summary>Records what it was asked to play, and plays nothing.</summary>
        private sealed class RecordingStrategy : ISoundPlaybackStrategy
        {
            public List<string> Played { get; } = [];

            public int Aborts { get; private set; }

            public bool PlaySound(string soundName)
            {
                Played.Add(soundName);
                return true;
            }

            public void Abort() => Aborts++;
        }

        /// <summary>
        /// A name nothing redirects comes back unchanged, so callers never have to check first.
        /// </summary>
        [Fact]
        public void AnUnknownNameResolvesToItself()
        {
            Assert.Equal(SoundNames.MusicTitle, _registryUnderTest.Resolve(SoundNames.MusicTitle));
        }

        /// <summary>
        /// The point of the registry: an existing sound plays something else instead.
        /// </summary>
        [Fact]
        public void AnAliasRedirectsTheName()
        {
            _registryUnderTest.SetAlias(SoundNames.CombatWinWeak, "my_victory");

            Assert.Equal("my_victory", _registryUnderTest.Resolve(SoundNames.CombatWinWeak));
        }

        /// <summary>
        /// Call sites spell names as constants, but a plugin may not, so the lookup ignores case.
        /// </summary>
        [Fact]
        public void AnAliasIsFoundRegardlessOfCase()
        {
            _registryUnderTest.SetAlias("MY_SOUND", "other");

            Assert.Equal("other", _registryUnderTest.Resolve("my_sound"));
        }

        /// <summary>
        /// One plugin may redirect a name another plugin has already redirected.
        /// </summary>
        [Fact]
        public void AliasesAreFollowedThroughAChain()
        {
            _registryUnderTest.SetAlias("first", "second");
            _registryUnderTest.SetAlias("second", "third");

            Assert.Equal("third", _registryUnderTest.Resolve("first"));
        }

        /// <summary>
        /// Two plugins redirecting at each other must not hang the game thread.
        /// </summary>
        [Fact]
        public void ACycleStopsInsteadOfLoopingForever()
        {
            _registryUnderTest.SetAlias("a", "b");
            _registryUnderTest.SetAlias("b", "a");

            Assert.Equal("b", _registryUnderTest.Resolve("a"));
        }

        /// <summary>
        /// Passing no target removes the redirect, so a plugin can undo what it set.
        /// </summary>
        [Fact]
        public void ClearingAnAliasRestoresTheOriginalName()
        {
            _registryUnderTest.SetAlias(SoundNames.MusicWin, "other");
            _registryUnderTest.SetAlias(SoundNames.MusicWin, null);

            Assert.Equal(SoundNames.MusicWin, _registryUnderTest.Resolve(SoundNames.MusicWin));
        }

        /// <summary>
        /// The redirect is applied before the strategy sees the name. That is what lets a redirect
        /// move a sound to a different source than the one it would normally come from.
        /// </summary>
        [Fact]
        public void TheStrategyReceivesTheResolvedName()
        {
            var inner = new RecordingStrategy();
            _registryUnderTest.SetAlias(SoundNames.EventAlarm, "my_alarm");

            var strategyUnderTest = new AliasSoundPlaybackStrategy(inner, _registryUnderTest);

            Assert.True(strategyUnderTest.PlaySound(SoundNames.EventAlarm));
            Assert.Equal(["my_alarm"], inner.Played);
        }

        /// <summary>
        /// Silencing everything has to reach the strategy that actually plays something.
        /// </summary>
        [Fact]
        public void AbortIsPassedOn()
        {
            var inner = new RecordingStrategy();

            new AliasSoundPlaybackStrategy(inner, _registryUnderTest).Abort();

            Assert.Equal(1, inner.Aborts);
        }
    }
}
