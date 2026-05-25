// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using CivOne.Advances;
using CivOne.Events;
using CivOne.Graphics;

namespace CivOne.Screens
{
	// NO [ScreenResizeable] - it auto-resizes and redraws itself on each update, so no need to handle resize events separately
	internal class Discovery : BaseScreen
	{
		private const float FADE_STEP = 0.025f;

		private readonly IAdvance _advance;

        private float _fadeStep = 0.0f;
		
		private void FadeColours()
		{
			if (!GFX256) return;

            FadeStep = _fadeStep;

			Palette palette = Palette;
			for (int i = 86; i < 256; i++)
				palette[i] = FadeColour(OriginalColours[i], _advance.OriginalColours[i]);
			this.SetPalette(palette);
		}
		
		protected override bool HasUpdate(uint gameTick)
		{
			if (_fadeStep < 1.0F)
			{
				_fadeStep += FADE_STEP;
				if (_fadeStep >= 1.0F)
				{
					_fadeStep = 1.0F;
				}
				FadeColours();
			}
			return true;
		}
		
		public override bool KeyDown(KeyboardEventArgs args)
		{
			if (_fadeStep >= 1.0F)
				Destroy();
			return true;
		}
		
		public override bool MouseDown(ScreenEventArgs args)
		{
			if (_fadeStep >= 1.0F)
				Destroy();
			return true;
		}
		
		public Discovery(IAdvance advance)
		{
            _advance = advance;
			var modern = Human.HasAdvance<Electricity>() && advance.Not<Electricity>();
			string scientistName = Human.HasAdvance<Invention>() && (advance.Not<Invention>()) ? Translate("scientists") : Translate("wise men");

			Picture background = Resources[$"DISCOVR{(modern ? 2 : 1)}"];
			
			Palette = background.Palette;
			this.Clear(32).AddLayer(background);

			string[] text = TranslateFormattedArray("{0} {1}\ndiscover the secret\nof {2}!", Human.Civilization.Name, scientistName, advance.TranslatedName);

			
			for (int i = 0; i < text.Length; i++)
			{
				if (modern)
				{
					this.DrawText(text[i], 0, 3, 101, 30 + (8 * i));
				}
				else
				{
					this.DrawText(text[i], 5, 32, 101, 7 + (15 * i))
						.DrawText(text[i], 5, 15, 101, 6 + (15 * i));
				}
			}

			this.AddLayer(advance.Icon, 119, modern ? 53 : 61);

			PlaySound(Human.Civilization.Tune);
		}
	}
}