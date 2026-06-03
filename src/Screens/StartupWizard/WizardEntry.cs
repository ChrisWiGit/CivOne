// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

namespace CivOne.Screens.StartupWizard
{
	/// <summary>
	/// Defines the action that a <see cref="WizardEntry"/> performs when activated.
	/// </summary>
	internal enum WizardEntryAction
	{
		/// <summary>
		/// No action; the entry is just informational and cannot be activated by the user.
		/// </summary>
		None,
		/// <summary>
		/// Selects a language. The chosen language postfix is stored in <see cref="WizardEntry.Value"/>.
		/// </summary>
		SelectLanguage,

		/// <summary>
		/// Opens a folder-browser dialog so the user can locate the DOS Civilization data directory.
		/// </summary>
		BrowseDataFolder,

		/// <summary>
		/// Opens a folder-browser dialog so the user can locate the Civilization for Windows sound files.
		/// </summary>
		BrowseSoundFolder,

		/// <summary>
		/// Selects the screen aspect ratio used by the game renderer.
		/// The chosen value is stored in <see cref="WizardEntry.Value"/>.
		/// </summary>
		SelectAspectRatio,

		/// <summary>
		/// Toggles whether the in-game debug menu is enabled.
		/// </summary>
		ToggleDebugMenu,

		/// <summary>
		/// Opens the full setup screen with additional settings.
		/// </summary>
		OpenSetupScreen,

		/// <summary>
		/// Opens the CivOne profile folder in the file manager.
		/// </summary>
		OpenProfileFolder,

		/// <summary>
		/// Selects whether fullscreen mode is enabled.
		/// The chosen value is stored in <see cref="WizardEntry.Value"/>.
		/// </summary>
		SelectFullScreen,

		/// <summary>
		/// Advances the wizard to the next page.
		/// </summary>
		Continue,

		/// <summary>
		/// Returns the wizard to the previous page.
		/// </summary>
		Back,

		/// <summary>
		/// Toggles the sound on or off and persists the choice to <see cref="Settings"/>.
		/// </summary>
		ToggleSound,

		/// <summary>
		/// Completes the wizard, saves all settings and starts the game.
		/// </summary>
		Finish
	}

	/// <summary>
	/// Represents a single selectable entry on a <see cref="WizardPage"/>.
	/// </summary>
	/// <remarks>
	/// Each entry is identified by a display number and an optional keyboard hotkey.
	/// Disabled entries are rendered in a muted colour and cannot be activated by either
	/// keyboard or mouse input.
	/// </remarks>
	internal sealed class WizardEntry
	{
		/// <summary>
		/// Gets the 1-based display number shown next to the entry in the wizard UI for the user to select it by pressing the corresponding number key.
		/// The numbering will start at 1 and end at number 9 and then continue with letters (A, B, C, etc.) for any additional entries without hotkeys.
		/// If a hotkey is defined for the entry, it will be shown instead of the number in the UI.
		/// There maybe collisions between entry numbers and hotkeys of other entries. 
		/// In such a case you should define all Hotkeys explicitly to ensure a consistent user experience.
		/// </summary>
		public required int Number { get; init; }

		/// <summary>
		/// Gets the optional single-character keyboard shortcut that activates this entry.
		/// <para>
		/// When <see langword="null"/>, the entry can only be activated by clicking it with the mouse.
		/// Hotkey matching is case-insensitive.
		/// </para>
		/// </summary>
		public char? Hotkey { get; init; }

		/// <summary>
		/// Gets the localised label displayed for this entry in the wizard UI.
		/// </summary>
		public required string Text { get; init; }

		/// <summary>
		/// Gets the action performed when this entry is activated.
		/// </summary>
		public required WizardEntryAction Action { get; init; }

		/// <summary>
		/// Gets a value indicating whether this entry can be activated by the user.
		/// </summary>
		/// <remarks>
		/// Disabled entries (<see langword="false"/>) are rendered in a muted colour and are
		/// ignored by both hotkey handling and mouse-click handling.
		/// <para>
		/// Example: the <em>Continue</em> entry on the data-folder page is disabled until
		/// <see cref="IO.FileSystem.DataFilesExist"/> returns <see langword="true"/>.
		/// </para>
		/// </remarks>
		public bool Enabled { get; init; } = true;

		/// <summary>
		/// Gets an optional string value associated with the entry.
		/// </summary>
		/// <remarks>
		/// Used by <see cref="WizardEntryAction.SelectLanguage"/> entries to carry the
		/// language postfix (e.g. <c>"german"</c>) that identifies the chosen translation.
		/// Also used by <see cref="WizardEntryAction.SelectAspectRatio"/> entries to carry
		/// the selected aspect ratio name.
		/// For all other actions this property is <see langword="null"/>.
		/// </remarks>
		public string? Value { get; init; }

		/// <summary>
		/// Gets a value indicating whether this entry should always be kept as the last visible entry in the list, 
		/// even when the number of entries exceeds the maximum visible count and scrolling is necessary.
		/// This will decrease <see cref="WizardPage.EntriesMaxCount"/> by one to ensure that there is always room to display this entry at the end of the list.
		/// There can be multiple entries with this property set to <see langword="true"/>; they will be kept at the end of the list in the order they are defined.
		/// </summary>
		public bool KeepAlwaysLastPosition { get; init; }
	}
}
