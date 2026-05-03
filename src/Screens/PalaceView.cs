// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.Collections.Generic;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.Graphics.Sprites;
using CivOne.IO;
using CivOne.Screens.PalaceAssets;

namespace CivOne.Screens
{
	[ScreenResizeable]
	internal class PalaceView : BaseScreen
	{
		private enum Stage
		{
			View,
			Message,
			SelectPart,
			SelectStyle,
			Morph
		}

		private const int NOISE_COUNT = 40;

		// Y-level offsets for text positioning (for manual fine-tuning)
		private const int PALACE_NUMBERS_Y_OFFSET = 0;   // Base: 144 (font 14), 145 (font 5)
		private const int GARDEN_LETTERS_Y_OFFSET = 0;   // Base: 160 (font 14), 161 (font 5)

		private readonly Picture _background;
		private readonly IPalaceSpriteProvider _sprites;
		private readonly byte[,] _noiseMap;
		private readonly bool _build;
		private readonly bool _debug;
		private bool _disableNoise = false;
		private int _pendingPartIndex = -1;
		private int OffsetX => System.Math.Max(0, (Width - 320) / 2);
		private int OffsetY => System.Math.Max(0, (Height - 200) / 2);

		private byte OpaqueBlackColour
		{
			get
			{
				for (int i = 1; i < Palette.Length; i++)
				{
					Colour c = Palette[i];
					if (c.A > 0 && c.R == 0 && c.G == 0 && c.B == 0)
						return (byte)i;
				}
				return 5;
			}
		}

		private Picture _palaceMorph = null;
		private int _noiseCounter = NOISE_COUNT + 5;

		private Stage _currentStage = Stage.View;

		private bool _update = true;

		private static PalacePart GetPalacePartPreview(PalaceData palace, int index)
		{
			return index switch
			{
				0 => PalacePart.LeftTower,
				1 or 2 => palace.GetPalaceLevel(index - 1) > 0 ? PalacePart.Wall : PalacePart.LeftTowerWall,
				3 => PalacePart.Center,
				4 => palace.GetPalaceLevel(index + 1) > 0 ? PalacePart.WallShadow : PalacePart.RightTowerWallShadow,
				5 => palace.GetPalaceLevel(index + 1) > 0 ? PalacePart.Wall : PalacePart.RightTowerWall,
				6 => PalacePart.RightTower,
				_ => PalacePart.None
			};
		}

		private void StartMorph(Picture palaceMorph)
		{
			if (_disableNoise)
			{
				_currentStage = GetPostMorphStage();
				_update = true;
				return;
			}
			_palaceMorph = palaceMorph;
			_noiseCounter = NOISE_COUNT + 5;
			_currentStage = Stage.Morph;
			_update = true;
		}

		private Stage GetPostMorphStage() => _build && _debug ? Stage.SelectPart : Stage.View;

		private void DrawLeftPalaceSide(Picture picture, PalaceData palace)
		{
			Picture deferredLeftTower = null;
			int leftStart = System.Math.Max(palace.PalaceLeft, 0);
			int leftEnd = System.Math.Min(palace.PalaceRight, 2);

			for (int i = leftStart; i <= leftEnd; i++)
			{
				byte level = palace.GetPalaceLevel(i);
				if (level == 0 && i < 2) continue;

				PalacePart part;
				int xx;
				switch (i)
				{
					case 0:
						xx = 9;
						part = PalacePart.LeftTower;
						break;
					case 1:
					case 2:
						xx = 17 + (48 * i);
						if (palace.GetPalaceLevel(i - 1) > 0)
						{
							part = PalacePart.Wall;
							xx -= 24;
						}
						else
						{
							part = PalacePart.LeftTowerWall;
							xx -= 33;
						}
						break;
					default:
						continue;
				}

				Picture palacePart = _sprites.GetPalacePart(palace.GetPalaceStyle(i), part, palace.GetPalaceLevel(i));
				if (i == 0)
				{
					// Draw left tower after side walls so it stays visually in front.
					deferredLeftTower = palacePart;
					continue;
				}

				picture.AddLayer(palacePart, xx, 37);
			}

			if (deferredLeftTower != null)
			{
				picture.AddLayer(deferredLeftTower, 9, 37);
			}
		}

		private void DrawRightPalaceSide(Picture picture, PalaceData palace)
		{
			int rightStart = System.Math.Max(palace.PalaceLeft, 4);
			int rightEnd = System.Math.Min(palace.PalaceRight, 6);

			for (int i = rightStart; i <= rightEnd; i++)
			{
				byte level = palace.GetPalaceLevel(i);
				if (level == 0 && i > 4) continue;

				PalacePart part;
				int xx;
				switch (i)
				{
					case 4:
						xx = 185;
						if (palace.GetPalaceLevel(i + 1) > 0)
						{
							part = PalacePart.WallShadow;
						}
						else
						{
							part = PalacePart.RightTowerWallShadow;
						}
						break;
					case 5:
						xx = 233;
						if (palace.GetPalaceLevel(i + 1) > 0)
						{
							part = PalacePart.Wall;
						}
						else
						{
							part = PalacePart.RightTowerWall;
						}
						break;
					case 6:
						xx = 278;
						if (level == 4)
						{
							xx -= 1;
						}
						part = PalacePart.RightTower;
						break;
					default:
						continue;
				}

				picture.AddLayer(_sprites.GetPalacePart(palace.GetPalaceStyle(i), part, palace.GetPalaceLevel(i)), xx, 37);
			}
		}

		private Picture DrawPalace()
		{
			PalaceData palace = Human.Palace;
			Picture picture = new(320, 200);
			picture.AddLayer(_background);

			Picture backdrop = _sprites.GetGardenBackdrop(palace.GetGardenLevel(1));
			if (backdrop != null)
			{
				picture.AddLayer(backdrop, 0, 135);
			}

			DrawLeftPalaceSide(picture, palace);
			DrawRightPalaceSide(picture, palace);

			// Draw palace middle
			picture.AddLayer(_sprites.GetPalacePart(palace.GetPalaceStyle(3), PalacePart.Center, palace.GetPalaceLevel(3)), 135, palace.GetPalaceLevel(3) == 0 ? 37 : 38);

			Picture leftGarden = _sprites.GetGardenBrush(0, palace.GetGardenLevel(0));
			if (leftGarden != null)
			{
				picture.AddLayer(leftGarden, 0, palace.GetGardenLevel(0) == 1 ? 105 : 94);
			}

			Picture rightGarden = _sprites.GetGardenBrush(2, palace.GetGardenLevel(2));
			if (rightGarden != null)
			{
				picture.AddLayer(rightGarden, 184, palace.GetGardenLevel(2) == 1 ? 105 : 94);
			}
			return picture;
		}

		protected override bool HasUpdate(uint gameTick)
		{
			if (!_update)
			{
				_update = false;
				return true;
			}

			int ox = OffsetX;
			int oy = OffsetY;
			PalaceData palace = Human.Palace;

			this.Clear(OpaqueBlackColour)
				.AddLayer(DrawPalace(), ox, oy);

			switch (_currentStage)
			{
				case Stage.Message:
					{
						Picture message = new Picture(269, 39)
							.Tile(Pattern.PanelGrey)
							.DrawRectangle3D()
							.As<Picture>();
						int yy = 4;
						foreach (string line in TextFile.Instance.GetGameText("KING/PALACE"))
						{
							message.DrawText(line.Trim('^'), 0, 15, 4, yy);
							yy += 8;
						}
						this.FillRectangle(20 + ox, 16 + oy, 271, 41, 5)
							.AddLayer(message, 21 + ox, 17 + oy);
					}
					break;
				case Stage.SelectPart:
					{
						Picture message = new Picture(180, 15)
							.Tile(Pattern.PanelGrey)
							.DrawRectangle3D()
							.DrawText("Which section shall we improve?", 0, 15, 4, 4)
							.As<Picture>();
						this.FillRectangle(40 + ox, 16 + oy, 182, 17, 5)
							.AddLayer(message, 41 + ox, 17 + oy);

						for (int i = 0; i < 7; i++)
						{
							if (!palace.IsSlotUnlocked(i) || palace.GetPalaceLevel(i) >= 4) continue;

							int xx = 12 + (48 * i);
							this.DrawText($"{i + 1}", 0, 5, xx + ox, 145 + oy + PALACE_NUMBERS_Y_OFFSET)
								.DrawText($"{i + 1}", 0, 14, xx + ox, 144 + oy + PALACE_NUMBERS_Y_OFFSET);
						}
						for (int i = 0; i < 3; i++)
						{
							if (palace.GetGardenLevel(i) >= 3) continue;

							int xx = 40 + (120 * i);
							this.DrawText($"{(char)('A' + i)}", 0, 5, xx + ox, 161 + oy + GARDEN_LETTERS_Y_OFFSET)
								.DrawText($"{(char)('A' + i)}", 0, 14, xx + ox, 160 + oy + GARDEN_LETTERS_Y_OFFSET);
						}
					}
					break;
				case Stage.SelectStyle:
					{
						Picture message = new Picture(280, 118)
							.Tile(Pattern.PanelGrey)
							.DrawRectangle3D()
							.DrawText("Which style shall we use?", 0, 15, 4, 4)
							.As<Picture>();

						if (_pendingPartIndex >= 0)
						{
							PalacePart previewPart = GetPalacePartPreview(palace, _pendingPartIndex);
							byte previewLevel = (byte)(palace.GetPalaceLevel(_pendingPartIndex) + 1);
							for (int i = 1; i <= 3; i++)
							{
								int panelX = 12 + ((i - 1) * 88);
								Picture preview = _sprites.GetPalacePart((PalaceStyle)i, previewPart, previewLevel);
								message.DrawRectangle(panelX, 18, 76, 92, 5)
									.DrawText($"{i}", 0, 14, panelX + 33, 21);
								if (preview != null)
								{
									int previewX = panelX + ((76 - preview.Width) / 2);
									int previewY = 108 - preview.Height;
									message.AddLayer(preview, previewX, previewY);
								}
							}
						}

						this.FillRectangle(20 + ox, 16 + oy, 282, 120, 5)
							.AddLayer(message, 21 + ox, 17 + oy);
					}
					break;
				case Stage.Morph:
					if (_noiseCounter > 0)
					{
						_palaceMorph.ApplyNoise(_noiseMap, _noiseCounter--);
						this.Clear(OpaqueBlackColour)
							.AddLayer(DrawPalace(), ox, oy)
							.AddLayer(_palaceMorph, ox, oy);
						return true;
					}
					_currentStage = GetPostMorphStage();
					_update = true;
					return true;
			}

			_update = false;
			return true;
		}

		protected override void Resize(int width, int height)
		{
			base.Resize(width, height);
			_update = true;
		}

		public override bool KeyDown(KeyboardEventArgs args)
		{
			PalaceData palace = Human.Palace;

			if (_debug && args[Key.Escape])
			{
				Destroy();
				return true;
			}

			if (_debug && args[Key.F1])
			{
				_disableNoise = !_disableNoise;
				_update = true;
				return true;
			}

			switch (_currentStage)
			{
				case Stage.Message:
					_currentStage = Stage.SelectPart;
					_update = true;
					break;
				case Stage.SelectPart:
					if (args.KeyChar >= 'A' && args.KeyChar <= 'C')
					{
						int index = args.KeyChar - 'A';
						if (palace.GetGardenLevel(index) < 3)
						{
							Picture palaceMorph = DrawPalace();
							palace.SetGarden(index, (byte)(palace.GetGardenLevel(index) + 1));
							StartMorph(palaceMorph);
						}
						break;
					}

					if (args.KeyChar >= '1' && args.KeyChar <= '7')
					{
						int index = args.KeyChar - '1';
						if (palace.IsSlotUnlocked(index) && palace.GetPalaceLevel(index) < 4)
						{
							_pendingPartIndex = index;
							_currentStage = Stage.SelectStyle;
							_update = true;
						}
					}
					break;
				case Stage.SelectStyle:
					if (args.KeyChar >= '1' && args.KeyChar <= '3' && _pendingPartIndex >= 0)
					{
						byte newLevel = (byte)(palace.GetPalaceLevel(_pendingPartIndex) + 1);
						if (newLevel <= 4)
						{
							PalaceStyle style = (PalaceStyle)(args.KeyChar - '0');
							Picture palaceMorph = DrawPalace();
							palace.SetPalace(_pendingPartIndex, (byte)style, newLevel);
							_pendingPartIndex = -1;
							StartMorph(palaceMorph);
						}
					}
					break;
				case Stage.View:
					Destroy();
					break;
			}
			return true;
		}

		public override bool MouseDown(ScreenEventArgs args)
		{
			switch (_currentStage)
			{
				case Stage.Message:
					_currentStage = Stage.SelectPart;
					_update = true;
					break;
				case Stage.SelectPart:
					if (!_debug)
					{
						_currentStage = Stage.View;
						_update = true;
					}
					break;
				case Stage.View:
					Destroy();
					break;
			}
			return true;
		}

		public PalaceView(bool build = false, IPalaceSpriteProvider sprites = null, bool debug = false)
		{
			_build = build;
			_debug = debug;
			_sprites = sprites ?? PalaceSpriteProviderFactory.GetInstance();

			_noiseMap = new byte[320, 200];
			for (int x = 0; x < 320; x++)
			{
				for (int y = 0; y < 200; y++)
				{
					_noiseMap[x, y] = (byte)Common.Random.Next(1, NOISE_COUNT);
				}
			}

			_background = _sprites.GetBackground();
			Palette = _background.Palette;
			if (build) _currentStage = Stage.Message;
		}
	}
}