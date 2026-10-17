// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Collections.Generic;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.Graphics.Sprites;
using CivOne.IO;
using CivOne.Screens.PalaceAssets;
using CivOne.Services.Random;

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
		private readonly PalaceData _palace;
		private readonly PalaceLayoutDelegate _layout = new();
		private readonly byte[,] _noiseMap;
		private readonly bool _build;
		private readonly bool _debug;
		private bool _disableNoise;
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

		private Picture? _palaceMorph;
		private int _noiseCounter = NOISE_COUNT + 5;

		private Stage _currentStage = Stage.View;

		private bool _update = true;

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

		private Picture DrawPalace()
		{
			PalaceData palace = _palace;
			Picture picture = new(320, 200, _background.Palette);
			picture.AddLayer(_background);

			Picture? backdrop = _sprites.GetGardenBackdrop(palace.GetGardenLevel(1));
			if (backdrop != null)
			{
				picture.AddLayer(backdrop, 0, 135);
			}

			foreach (PalacePiece piece in _layout.Layout(palace))
			{
				picture.AddLayer(_sprites.GetPalacePiece(piece.Level, piece.Style, piece.Kind), piece.X, piece.Y);
			}

			Picture? leftGarden = _sprites.GetGardenBrush(0, palace.GetGardenLevel(0));
			if (leftGarden != null)
			{
				picture.AddLayer(leftGarden, 0, palace.GetGardenLevel(0) == 1 ? 105 : 94);
			}

			Picture? rightGarden = _sprites.GetGardenBrush(2, palace.GetGardenLevel(2));
			if (rightGarden != null)
			{
				picture.AddLayer(rightGarden, 184, palace.GetGardenLevel(2) == 1 ? 105 : 94);
			}
			return picture;
		}

		/// <summary>
		/// Creates a static palace picture for use on other screens.
		/// </summary>
		/// <param name="sprites">Optional palace sprite provider.</param>
		/// <param name="palaceData">Optional palace data.</param>
		/// <returns>The rendered palace image.</returns>
		public static Picture CreatePicture(IPalaceSpriteProvider? sprites = null, PalaceData? palaceData = null)
		{
			using PalaceView palaceView = new(false, sprites ?? PalaceSpriteProviderFactory.GetInstance(), false, palaceData);
			return palaceView.DrawPalace();
		}

		private void DrawDebugHelp(int ox, int oy)
		{
			if (!_debug)
			{
				return;
			}

			this.DrawText(Translate("F1 Disable Noise"), 0, 5, ox + 4, oy + 190)
				.DrawText(Translate("F1 Disable Noise"), 0, 14, ox + 4, oy + 189);
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
			PalaceData palace = _palace;

			this.Clear(OpaqueBlackColour)
				.AddLayer(DrawPalace(), ox, oy);

			switch (_currentStage)
			{
				case Stage.Message:
					{
						Picture message = new(269, 39);

						message
							.Tile(Pattern.PanelGrey)
							.DrawRectangle3D();
						int yy = 4;
						foreach (string line in GetGameText("KING/PALACE"))
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
						Picture message = new(180, 15);

						message
							.Tile(Pattern.PanelGrey)
							.DrawRectangle3D()
							.DrawText(Translate("Which section shall we improve?"), 0, 15, 4, 4);

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
					DrawStyleSelection(palace, ox, oy);
					break;
				case Stage.Morph:
					if (_noiseCounter > 0 && _palaceMorph != null)
					{
						_palaceMorph.ApplyNoise(_noiseMap, _noiseCounter--);
						this.Clear(OpaqueBlackColour)
							.AddLayer(DrawPalace(), ox, oy)
							.AddLayer(_palaceMorph, ox, oy);
						DrawDebugHelp(ox, oy);
						return true;
					}
					_currentStage = GetPostMorphStage();
					_update = true;
					return true;
			}

			DrawDebugHelp(ox, oy);

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
					Choose(char.ToUpperInvariant(args.KeyChar));
					break;
				case Stage.SelectStyle:
					if (args.KeyChar >= '1' && args.KeyChar <= '3')
					{
						ChooseStyle(args.KeyChar - '0');
					}
					break;
				case Stage.View:
					Destroy();
					break;
			}
			return true;
		}

		/// <summary>
		/// Handles a section number '1'..'7' or a garden 'A'..'C', like the original upgrade dialog.
		/// </summary>
		private void Choose(char choice)
		{
			PalaceData palace = _palace;
			if (choice >= 'A' && choice <= 'C')
			{
				int garden = choice - 'A';
				if (palace.GetGardenLevel(garden) < 3)
				{
					Picture palaceMorph = DrawPalace();
					palace.SetGarden(garden, (byte)(palace.GetGardenLevel(garden) + 1));
					StartMorph(palaceMorph);
				}
				return;
			}

			if (choice >= '1' && choice <= '7' && _layout.ResolveSection(palace, choice - '0') is int section)
			{
				_pendingPartIndex = section;
				_currentStage = Stage.SelectStyle;
				_update = true;
			}
		}

		private void ChooseStyle(int style)
		{
			if (style < 1 || style > 3 || _pendingPartIndex < 0)
			{
				return;
			}

			PalaceData palace = _palace;
			byte newLevel = (byte)Math.Min(palace.GetPalaceLevel(_pendingPartIndex) + 1, 4);
			Picture palaceMorph = DrawPalace();
			palace.SetPalace(_pendingPartIndex, (byte)style, newLevel);
			_pendingPartIndex = -1;
			StartMorph(palaceMorph);
		}

		/// <summary>
		/// Draws the style choice like the original: three panels with the chosen section in each style.
		/// </summary>
		private void DrawStyleSelection(PalaceData palace, int ox, int oy)
		{
			this.FillRectangle(16 + ox, 2 + oy, 288, 102, 9)
				.DrawText(Translate("Select new section."), 0, 15, 160 + ox, 3 + oy, TextAlign.Center);

			if (_pendingPartIndex < 0)
			{
				return;
			}

			int level = Math.Min(palace.GetPalaceLevel(_pendingPartIndex) + 1, 4);
			for (int i = 1; i <= 3; i++)
			{
				int panelX = (100 * i) - 80;
				this.FillRectangle(panelX + ox, 10 + oy, 80, 92, 1);
				foreach (PalacePiece piece in _layout.Preview(palace, _pendingPartIndex, (PalaceStyle)i, level, panelX + 16))
				{
					this.AddLayer(_sprites.GetPalacePiece(piece.Level, piece.Style, piece.Kind), piece.X + ox, piece.Y + oy);
				}
				this.DrawText($"{i}", 0, 5, (100 * i) - 43 + ox, 13 + oy)
					.DrawText($"{i}", 0, 15, (100 * i) - 44 + ox, 12 + oy);
			}
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
					// Like the original, a click chooses the section or garden under the mouse.
					if (_layout.ChoiceAt(args.X - OffsetX, args.Y - OffsetY) is char choice)
					{
						Choose(choice);
					}
					break;
				case Stage.SelectStyle:
					{
						// Clicks in the resize margin around the 320×200 palace are ignored.
						int x = args.X - OffsetX;
						int y = args.Y - OffsetY;
						if (x >= 0 && x < 320 && y >= 0 && y < 200)
						{
							ChooseStyle((x / 107) + 1);
						}
					}
					break;
				case Stage.View:
					Destroy();
					break;
			}
			return true;
		}

		public PalaceView(bool buildMode = false, IPalaceSpriteProvider? sprites = null, bool debug = false, PalaceData? palaceData = null)
		{
			var random = RandomServiceFactory.Create();
			_build = buildMode;
			_debug = debug;
			_sprites = sprites ?? PalaceSpriteProviderFactory.GetInstance();
			_palace = palaceData ?? Human.Palace;

			_noiseMap = new byte[320, 200];
			for (int x = 0; x < 320; x++)
			{
				for (int y = 0; y < 200; y++)
				{
					_noiseMap[x, y] = random.NextByte(1, NOISE_COUNT);
				}
			}

			_background = _sprites.GetBackground();
			Palette = _background.Palette;
			if (buildMode) _currentStage = Stage.Message;
		}

		protected override void Dispose(bool disposing)
		{
			if (!disposing)
			{
				return;
			}

			_background.Dispose();
			base.Dispose(disposing);
		}
	}
}