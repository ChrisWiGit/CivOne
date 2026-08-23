// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.Persistence.Game;

namespace CivOne.Screens
{
	[ScreenResizeable]
	internal sealed class SpaceVictory : BaseScreen
	{
		private readonly Picture _background;
		private readonly string _tribeName;
		private bool _update = true;

		private int OffsetX => Math.Max(0, (Width - 320) / 2);
		private int OffsetY => Math.Max(0, (Height - 200) / 2);

		private byte OpaqueBlackColour
		{
			get
			{
				for (int i = 1; i < Palette.Length; i++)
				{
					Colour c = Palette[i];
					if (c.A > 0 && c.R == 0 && c.G == 0 && c.B == 0)
					{
						return (byte)i;
					}
				}

				return 5;
			}
		}

		protected override bool HasUpdate(uint gameTick)
		{
			if (!_update)
			{
				return false;
			}

			_update = false;
			this.Clear(OpaqueBlackColour)
				.AddLayer(_background, OffsetX, OffsetY)
				.DrawText(TranslateFormatted("{0} reaches Alpha Centauri!", _tribeName), 5, 22, 160 + OffsetX, 174 + OffsetY, TextAlign.Center)
				.DrawText(Translate("Press any key to continue"), 5, 15, 160 + OffsetX, 188 + OffsetY, TextAlign.Center);
			return true;
		}

		protected override void Resize(int width, int height)
		{
			base.Resize(width, height);
			_update = true;
		}

		public override bool KeyDown(KeyboardEventArgs args)
		{
			Destroy();
			return true;
		}

		public override bool MouseDown(ScreenEventArgs args)
		{
			Destroy();
			return true;
		}

		public SpaceVictory(IPlayer winner)
		{
			_tribeName = winner?.TribeName ?? throw new ArgumentNullException(nameof(winner));
			_background = Resources["SPACEST"];
			Palette = _background.Palette;
		}
	}
}