using System.Collections.Generic;
using System.Linq;
using CivOne.Screens;
using CivOne.Services;
using CivOne.Sound.Cvl;
using CivOne.Sound.Playback;
using Xunit;

namespace CivOne.UnitTests
{
	public class SoundPackPickerDelegateTests
	{
		private sealed class StubTranslationService : ITranslationService
		{
			public string Translate(string key) => key;

			public string TranslateFormatted(string key, params object[] args) => string.Format(System.Globalization.CultureInfo.InvariantCulture, key, args);

			public string[] TranslateArray(string key) => [key];

			public string[] TranslateFormattedArray(string key, params object[] args) => [string.Format(System.Globalization.CultureInfo.InvariantCulture, key, args)];
		}

		private IReadOnlyList<SoundPackPickerEntry>? _opened;

		private SoundPackPickerDelegate CreatePicker(bool hasWaveFiles, SoundPackSummary[] packs) => new(
			packs: () => packs,
			hasWaveFiles: () => hasWaveFiles,
			translation: new StubTranslationService(),
			open: entries => _opened = entries);

		[Fact]
		public void EntriesListWaveFilesFirstThenPacks()
		{
			IReadOnlyList<SoundPackPickerEntry> entries = CreatePicker(true, [new("adlib", "AdLib"), new("pc-speaker", "PC speaker")]).GetEntries();

			Assert.Equal([SoundPlaybackStrategyConstants.WaveSoundPack, "adlib", "pc-speaker"], entries.Select(entry => entry.PackId));
			Assert.Equal(["Wave files", "AdLib", "PC speaker"], entries.Select(entry => entry.Label));
		}

		[Fact]
		public void EntriesNeverListNone()
		{
			IReadOnlyList<SoundPackPickerEntry> entries = CreatePicker(true, [new("adlib", "AdLib")]).GetEntries();

			Assert.DoesNotContain(entries, entry => entry.PackId == SoundPlaybackStrategyConstants.NoSoundPack);
		}

		[Fact]
		public void EntriesSkipWaveFilesWhenThereAreNone()
		{
			IReadOnlyList<SoundPackPickerEntry> entries = CreatePicker(false, [new("adlib", "AdLib")]).GetEntries();

			Assert.Equal(["adlib"], entries.Select(entry => entry.PackId));
		}

		[Fact]
		public void TryOpenOpensPickerWithEntries()
		{
			Assert.True(CreatePicker(true, []).TryOpen());

			Assert.NotNull(_opened);
			Assert.Single(_opened);
		}

		[Fact]
		public void TryOpenDoesNothingWithoutPacksOrWaveFiles()
		{
			Assert.False(CreatePicker(false, []).TryOpen());

			Assert.Null(_opened);
		}

		[Fact]
		public void CanOpenOverNoScreenIsFalse()
		{
			Assert.False(CreatePicker(true, []).CanOpenOver(null));
		}
	}
}
