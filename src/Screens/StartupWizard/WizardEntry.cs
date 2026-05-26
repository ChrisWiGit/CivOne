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
	internal enum WizardEntryAction
	{
		SelectLanguage,
		BrowseDataFolder,
		Continue,
		Back,
		ToggleSound,
		Finish
	}

	internal sealed class WizardEntry
	{
		public int Number { get; init; }
		public char? Hotkey { get; init; }
		public string Text { get; init; }
		public WizardEntryAction Action { get; init; }
		public bool Enabled { get; init; } = true;
		public string Value { get; init; }
	}
}
