// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.Linq;
using CivOne.Graphics;

namespace CivOne.Screens.Dialogs
{
    internal class SpyMessage : BaseDialog
	{
        private static Picture[] TextBitmaps(string[] message)
		{
			Picture[] output = new Picture[message.Length];
			for (int i = 0; i < message.Length; i++)
				output[i] = Resources.GetText(message[i], 0, 15);
			return output;
		}

		private static int DialogWidth(string[] message)
		{
			return TextBitmaps(message).Max(b => b.Width) + 50;
		}

		public SpyMessage(string[] message) : base(38, 72, DialogWidth(message), 57)
		{
            IBitmap spyPortrait = Icons.Spy;

			Palette palette = Common.DefaultPalette;
			// In 16-colour mode both palettes have only 16 entries, so skip the merge.
			if (palette.Length >= 256 && spyPortrait.Palette.Length >= 256)
			{
				for (int i = 144; i < 256; i++)
				{
					palette[i] = spyPortrait.Palette[i];
				}
			}
			this.SetPalette(palette);
			
			var textLines = TextBitmaps(message);
			DialogBox.AddLayer(spyPortrait, 2, 2);
			for (int i = 0; i < textLines.Length; i++)
				DialogBox.AddLayer(textLines[i], 47, (textLines[i].Height * (i -1)) + 13);
		}
	}
}