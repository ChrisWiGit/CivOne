// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.Drawing;
using System.Linq;
using CivOne.Advances;
using CivOne.Enums;
using CivOne.Graphics;
using CivOne.Services;

namespace CivOne.Screens.Dialogs
{
	internal class AdvisorMessage : BaseDialog
	{
		private static Picture[] TextBitmaps(string[] message)
		{
			Picture[] output = new Picture[message.Length];
			for (int i = 0; i < message.Length; i++)
				output[i] = Resources.GetText(message[i], 0, 15);
			return output;
		}

		private static int PORTRAIT_SIZE = 52;
		private static int MINIMUM = 94;

		private static string[] LocalizedAdvisorNames()
		{
			ITranslationService translation = TranslationServiceFactory.GetCurrent();
			return
			[
				translation.Translate("Defense Minister"),
				translation.Translate("Domestic Advisor"),
				translation.Translate("Foreign Minister"),
				translation.Translate("Science Advisor")
			];
		}

		private static int DialogWidth(string[] message)
		{
			int advisorWidth = TextBitmaps(LocalizedAdvisorNames()).Max(b => b.Width) + PORTRAIT_SIZE;
            int maxWidth = TextBitmaps(message).Max(b => b.Width) + PORTRAIT_SIZE;
			maxWidth = System.Math.Max(System.Math.Max(maxWidth, advisorWidth), MINIMUM);
			return maxWidth;
		}

		public AdvisorMessage(Advisor advisor, string[] message, bool leftAlign) : base((leftAlign ? 38 : 58), 72, DialogWidth(message), 62)
		{
			bool modernGovernment = Human.HasAdvance<Invention>();
			IBitmap governmentPortrait = Icons.GovernmentPortrait(Human.Government, advisor, modernGovernment);
			
			const int portraitPaletteStart = 144;
			using Palette palette = Common.DefaultPalette
				.Merge(governmentPortrait.Palette, portraitPaletteStart, 256 - portraitPaletteStart);
			SetPalette(palette);
			
			Picture[] textLines = TextBitmaps(message);
			DialogBox.AddLayer(governmentPortrait, 2, 2);
			string advisorLabel = TranslateFormatted("{0}:", LocalizedAdvisorNames()[(int)advisor]);
			DialogBox.DrawText(advisorLabel, 0, 15, 47, 4);
			DialogBox.FillRectangle(47, 11, Resources.GetText(advisorLabel, 0, 15).Width + 1, 1, 11);
			for (int i = 0; i < textLines.Length; i++)
				DialogBox.AddLayer(textLines[i], 47, (textLines[i].Height * i) + 13);
		}
	}
}