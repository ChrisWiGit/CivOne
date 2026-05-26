// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using CivOne.Enums;

namespace CivOne.Screens.StartupWizard
{
	internal sealed class WizardEngine
	{
		private const int LastPageIndex = 4;

		public int PageIndex { get; private set; }
		public string StatusMessage { get; set; }
		public string SelectedLanguagePostfix { get; set; }
		public string DataFolder { get; set; }
		public bool SoundEnabled { get; set; }

		public WizardEngine(string selectedLanguagePostfix)
		{
			PageIndex = 0;
			StatusMessage = string.Empty;
			SelectedLanguagePostfix = selectedLanguagePostfix ?? string.Empty;
			SoundEnabled = Settings.Instance.Sound != GameOption.Off;
		}

		public void MoveNext()
		{
			if (PageIndex < LastPageIndex)
			{
				PageIndex++;
			}
		}

		public void MoveBack()
		{
			if (PageIndex > 0)
			{
				PageIndex--;
			}
		}
	}
}
