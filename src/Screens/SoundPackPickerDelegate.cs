using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CivOne.Services;
using CivOne.Services.Screen;
using CivOne.Sound.Cvl;
using CivOne.Sound.Playback;

namespace CivOne.Screens;

/// <summary>
/// Lists the sound packs the player can pick and opens the picker.
/// </summary>
/// <remarks>
/// Opened by Shift+Alt+V, and when sound is switched on while "None" is selected
/// (Alt+V or the "Sound" entry of the game options).
/// "None" is never listed: picking it would leave sound off, which is not what the player asked for.
/// Wave files are listed only when the sounds folder holds at least one playable wave file.
/// </remarks>
/// <param name="packs">Lists the converted packs; <c>null</c> reads the profile's sounds folder.</param>
/// <param name="hasWaveFiles">Tells whether wave files exist; <c>null</c> checks the profile's sounds folder.</param>
/// <param name="translation">Translates the entry texts; <c>null</c> uses the current language.</param>
/// <param name="open">Shows the picker; <c>null</c> adds a <see cref="SoundPackPickerScreen"/>.</param>
internal sealed class SoundPackPickerDelegate(
	Func<IReadOnlyList<SoundPackSummary>>? packs = null,
	Func<bool>? hasWaveFiles = null,
	ITranslationService? translation = null,
	Action<IReadOnlyList<SoundPackPickerEntry>>? open = null)
{
	private readonly Func<IReadOnlyList<SoundPackSummary>> _packs = packs ?? (() => SoundPackCatalog.GetAvailablePacks(Settings.Instance.SoundsDirectory));
	private readonly Func<bool> _hasWaveFiles = hasWaveFiles ?? (() => new WaveSoundFileDelegate().Available().Count > 0);
	private readonly ITranslationService? _translation = translation;
	private readonly Action<IReadOnlyList<SoundPackPickerEntry>> _open = open ?? OpenScreen;

	private ITranslationService Translation => _translation ?? TranslationServiceFactory.GetCurrent();

	/// <summary>
	/// Builds the entries of the picker.
	/// </summary>
	/// <returns>Wave files first, then the converted packs. Empty when there is nothing to play.</returns>
	public IReadOnlyList<SoundPackPickerEntry> GetEntries()
	{
		List<SoundPackPickerEntry> entries = [];
		if (_hasWaveFiles())
		{
			entries.Add(new(SoundPlaybackStrategyConstants.WaveSoundPack, Translation.Translate("Wave files")));
		}

		entries.AddRange(_packs().Select(pack => new SoundPackPickerEntry(pack.PackId, Translation.Translate(pack.DisplayName))));
		return entries;
	}

	/// <summary>
	/// Tells whether the picker may open over a screen.
	/// </summary>
	/// <remarks>
	/// Only the game map without a running dialog or animation, and the main menu of the title
	/// screen, are safe.
	/// Over any other screen the popup could end up behind it or take away its input.
	/// </remarks>
	/// <param name="topScreen">The screen that currently receives input.</param>
	/// <returns><c>true</c> when the picker may open.</returns>
	[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This class is a delegate, not a static utility.")]
	public bool CanOpenOver(IScreen? topScreen) => topScreen switch
	{
		GamePlay => !GameTask.Any(),
		Menu { Id: "MainMenu" } => true,
		_ => false
	};

	/// <summary>
	/// Opens the picker, if there is anything to pick.
	/// </summary>
	/// <returns><c>false</c> when neither converted packs nor wave files exist; nothing is opened then.</returns>
	public bool TryOpen()
	{
		IReadOnlyList<SoundPackPickerEntry> entries = GetEntries();
		if (entries.Count == 0)
		{
			return false;
		}

		_open(entries);
		return true;
	}

	private static void OpenScreen(IReadOnlyList<SoundPackPickerEntry> entries)
		=> ScreenServiceFactory.CreateCommandService().AddScreen(new SoundPackPickerScreen(entries));
}
