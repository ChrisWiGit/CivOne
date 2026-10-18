using System;
using System.Collections.Generic;
using System.Linq;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Screens.Debug;
using CivOne.Sound.Playback;

namespace CivOne.Screens;

/// <summary>
/// Popup for picking a sound pack while a game runs.
/// </summary>
/// <remarks>
/// Opened when sound is switched on while no sound pack is selected.
/// Picking an entry selects that pack and switches sound on.
/// Esc closes the popup and changes nothing.
/// </remarks>
[ScreenResizeable]
internal sealed class SoundPackPickerScreen : BaseScreen
{
	private readonly SoundPackPickerEntry[] _entries;
	private readonly SoundPackSwitchDelegate _switch;

	private GridMenuDelegate? _grid;

	private void OnItemSelected(int index)
	{
		if (index < 0 || index >= _entries.Length)
		{
			return;
		}

		SoundPackPickerEntry entry = _entries[index];
		_switch.Switch(entry.PackId, () => entry.Label);
		Destroy();
	}

	private void OnCancelled(object? _, EventArgs __) => Destroy();

	private GridMenuDelegate CreateGrid()
	{
		string selected = SoundPlaybackStrategyProvider.SelectedPack;
		int defaultIndex = Math.Max(0, Array.FindIndex(_entries, entry => IsSelected(entry, selected)));

		return new GridMenuDelegate(
			[.. _entries.Select(entry => entry.Label)],
			GridMenuDelegate.SelectionMode.Select,
			isChecked: i => IsSelected(_entries[i], selected),
			fontId: 0,
			defaultSelectedIndex: defaultIndex);
	}

	private static bool IsSelected(SoundPackPickerEntry entry, string selected)
		=> string.Equals(entry.PackId, selected, StringComparison.OrdinalIgnoreCase);

	protected override bool HasUpdate(uint gameTick)
	{
		if (!RefreshNeeded())
		{
			return false;
		}

		if (_grid == null)
		{
			_grid = CreateGrid();
			_grid.ItemSelected += OnItemSelected;
			_grid.Cancelled += OnCancelled;
		}

		_grid.Draw(this, Translate("Sound pack:"), CanvasHeight);
		return true;
	}

	public override bool KeyDown(KeyboardEventArgs args)
	{
		if (_grid == null)
		{
			return false;
		}

		bool handled = _grid.KeyDown(args);
		if (handled)
		{
			Refresh();
		}
		return handled;
	}

	public override bool MouseDown(ScreenEventArgs args)
	{
		if (_grid == null)
		{
			return false;
		}

		bool handled = _grid.MouseDown(args.X, args.Y);
		if (handled)
		{
			Refresh();
		}
		return handled;
	}

	/// <summary>
	/// Creates the picker.
	/// </summary>
	/// <param name="entries">Packs to offer; see <see cref="SoundPackPickerDelegate.GetEntries"/>.</param>
	/// <param name="soundPackSwitch">Selects the picked pack; <c>null</c> uses the default switch.</param>
	public SoundPackPickerScreen(IReadOnlyList<SoundPackPickerEntry> entries, SoundPackSwitchDelegate? soundPackSwitch = null) : base(MouseCursor.Pointer)
	{
		ArgumentNullException.ThrowIfNull(entries);

		_entries = [.. entries];
		_switch = soundPackSwitch ?? new();

		using var defaultPalette = Common.DefaultPalette;
		Palette = defaultPalette;
	}
}
