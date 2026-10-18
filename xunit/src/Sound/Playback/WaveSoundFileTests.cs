using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CivOne.Sound;
using CivOne.Sound.Playback;
using Xunit;

namespace CivOne.UnitTests.Sound.Playback
{
    /// <summary>
    /// Covers which wave file a sound name ends up on, and which sounds a folder of wave files can
    /// play at all.
    /// </summary>
    public sealed class WaveSoundFileTests : IDisposable
    {
        private readonly string _folder;

        /// <summary>Creates an empty sounds folder for the test to fill.</summary>
        public WaveSoundFileTests()
        {
            _folder = Path.Combine(Path.GetTempPath(), $"wave-sounds-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_folder);
        }

        /// <summary>Removes the folder again.</summary>
        public void Dispose()
        {
            if (Directory.Exists(_folder)) Directory.Delete(_folder, true);
        }

        private void Place(string fileName) => File.WriteAllBytes(Path.Combine(_folder, fileName), [0]);

        private WaveSoundFileDelegate Delegate() => new(_folder);

        /// <summary>
        /// A file named after the sound is what the game asks for first.
        /// </summary>
        [Fact]
        public void AFileNamedAfterTheSoundIsFound()
        {
            Place($"{SoundNames.MusicTitle}.wav");

            Assert.True(Delegate().TryResolve(SoundNames.MusicTitle, out string? path));
            Assert.Equal($"{SoundNames.MusicTitle}.wav", Path.GetFileName(path));
        }

        /// <summary>
        /// The file names of the original game are upper case, and on a file system that tells
        /// upper and lower case apart they would otherwise never be found.
        /// </summary>
        [Fact]
        public void AnUpperCaseFileIsFound()
        {
            Place("OPENING.WAV");

            Assert.True(Delegate().TryResolve(SoundNames.MusicTitle, out string? path));
            Assert.Equal("OPENING.WAV", Path.GetFileName(path));
        }

        /// <summary>
        /// A file the player put in under the sound's own name wins over the older one, so replacing
        /// a sound needs nothing but the right file name.
        /// </summary>
        [Fact]
        public void TheSoundsOwnNameWinsOverTheOldOne()
        {
            Place("OPENING.WAV");
            Place($"{SoundNames.MusicTitle}.wav");

            Assert.True(Delegate().TryResolve(SoundNames.MusicTitle, out string? path));
            Assert.Equal($"{SoundNames.MusicTitle}.wav", Path.GetFileName(path));
        }

        /// <summary>
        /// A sound with no file is silent rather than an error - a collection of wave files rarely
        /// covers everything.
        /// </summary>
        [Fact]
        public void ASoundWithoutAFileIsNotFound()
        {
            Place("OPENING.WAV");

            Assert.False(Delegate().TryResolve(SoundNames.CombatWinStrong, out string? path));
            Assert.Null(path);
        }

        /// <summary>
        /// A folder that does not exist is the state of a fresh profile, and has to stay quiet.
        /// </summary>
        [Fact]
        public void AMissingFolderYieldsNothing()
        {
            var delegateUnderTest = new WaveSoundFileDelegate(Path.Combine(_folder, "does-not-exist"));

            Assert.False(delegateUnderTest.TryResolve(SoundNames.MusicTitle, out _));
            Assert.Empty(delegateUnderTest.Available());
        }

        /// <summary>
        /// The sound test offers exactly what the folder can play, which is what makes a partial
        /// collection of wave files usable at all.
        /// </summary>
        [Fact]
        public void OnlySoundsWithAFileAreOffered()
        {
            Place("OPENING.WAV");
            Place("LINC.WAV");
            Place("WINTUNE.WAV");

            IReadOnlyList<string> names = [.. Delegate().Available().Select(entry => entry.Name)];

            Assert.Equal([SoundNames.MusicTitle, SoundNames.LeaderLincoln, SoundNames.MusicWin], names);
        }

        /// <summary>
        /// One file may serve two sounds, because the older combat scheme really did share files.
        /// Both are offered, and both point at that file.
        /// </summary>
        [Fact]
        public void OneFileCanServeTwoSounds()
        {
            Place("S_LAND.WAV");

            var offered = Delegate().Available().ToDictionary(entry => entry.Name, entry => entry.Path);

            Assert.Equal(
                [SoundNames.CombatLossWeak, SoundNames.CombatWinWeak],
                offered.Keys.OrderBy(name => name, StringComparer.Ordinal));

            Assert.Equal(offered[SoundNames.CombatWinWeak], offered[SoundNames.CombatLossWeak]);
        }

        /// <summary>
        /// A file nothing maps to is not playable, so it must not turn up in the sound test.
        /// </summary>
        [Fact]
        public void AFileWithoutAMappingIsNotOffered()
        {
            Place("SOMETHING_ELSE.WAV");

            Assert.Empty(Delegate().Available());
        }
    }
}
