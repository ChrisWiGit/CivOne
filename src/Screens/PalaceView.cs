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

		private void DrawLeftPalaceSide(IBitmap picture, PalaceData palace)
		{
			// The left side of the palace consists of sections 0, 1 and 2. 
			const int maxLeftSectionIndex = 2;
			const int minLeftSectionIndex = 0;

			// buildings start from the right and draw leftwards
			// so startSection is the rightmost section that can be drawn, 
			int startSection = Math.Max(palace.PalaceLeft, minLeftSectionIndex);
			// and endSection is the leftmost section that can be drawn
			int endSection = Math.Min(palace.PalaceRight, maxLeftSectionIndex);

			const int spriteBaseY = 37;

			for (int sectionIndex = endSection; sectionIndex >= startSection; sectionIndex--)
			{
				if (sectionIndex < minLeftSectionIndex || sectionIndex > maxLeftSectionIndex)
					continue;

				byte sectionLevel = palace.GetPalaceLevel(sectionIndex);

				bool isEmptySection = sectionLevel == 0;
				bool isInnerSection = sectionIndex < maxLeftSectionIndex;

				if (isEmptySection && isInnerSection)
					continue;

				PalacePart part;
				int spriteX;

				if (sectionIndex == minLeftSectionIndex)
				{
					spriteX = 9;
					part = PalacePart.LeftTower;
				}
				else
				{
					const int sectionWidth = 48;
					const int leftmostSpriteX = 17;
					const int towerWallOverlap = 24;
					const int leftTowerWallOffset = 33;

					spriteX = leftmostSpriteX + (sectionWidth * sectionIndex);

					bool hasLeftNeighbor = palace.GetPalaceLevel(sectionIndex - 1) > 0;

					if (hasLeftNeighbor)
					{
						part = PalacePart.Wall;
						spriteX -= towerWallOverlap;
					}
					else
					{
						part = PalacePart.LeftTowerWall;
						spriteX -= leftTowerWallOffset;
					}
				}

				AddPalaceLayer(picture, spriteX, spriteBaseY, sectionIndex, sectionLevel, palace, part);
			}
		}

		void AddPalaceLayer(IBitmap picture, int left, int top,
				int sectionIndex,
				int sectionLevel,
				PalaceData palace,
				PalacePart part)
		{
			IBitmap sprite = _sprites.GetPalacePart(
					palace.GetPalaceStyle(sectionIndex),
					part,
					sectionLevel);

			picture.AddLayer(sprite, left, top);
		}

		private void DrawRightPalaceSide(IBitmap picture, PalaceData palace)
		{
			// The right side of the palace consists of sections 4, 5 and 6.
			const int minRightSectionIndex = 4;
			const int maxRightSectionIndex = 6;

			int startSection = Math.Max(palace.PalaceLeft, minRightSectionIndex);
			int endSection = Math.Min(palace.PalaceRight, maxRightSectionIndex);

			const int spriteBaseY = 37;

			for (int sectionIndex = startSection; sectionIndex <= endSection; sectionIndex++)
			{
				byte sectionLevel = palace.GetPalaceLevel(sectionIndex);

				bool isEmptySection = sectionLevel == 0;
				bool isInnerSection = sectionIndex > minRightSectionIndex;

				if (isEmptySection && isInnerSection)
					continue;

				PalacePart part;

				if (sectionIndex == minRightSectionIndex)
				{
					const int leftmostSpriteX = 185;					
					part = GetPalacePartWithRightNeighbour(palace, PalacePart.WallShadow, PalacePart.RightTowerWallShadow, sectionIndex);

					AddPalaceLayer(picture, leftmostSpriteX, spriteBaseY, sectionIndex, sectionLevel, palace, part);
					continue;
				}

				if (sectionIndex == 5)
				{
					const int rightmostSpriteX = 233;					
					part = GetPalacePartWithRightNeighbour(palace, PalacePart.Wall, PalacePart.RightTowerWall, sectionIndex);

					AddPalaceLayer(picture, rightmostSpriteX, spriteBaseY, sectionIndex, sectionLevel, palace, part);
					continue;
				}
				
				const int rightTowerSpriteX = 278;
				int spriteX = rightTowerSpriteX;
				if (sectionLevel == 4)
				{
					// For the highest level of the rightmost tower, the sprite is 1 pixel wider and overlaps with the center part by 1 pixel, so adjust left position to compensate.
					spriteX -= 1;
				}
				part = PalacePart.RightTower;

				AddPalaceLayer(picture, spriteX, spriteBaseY, sectionIndex, sectionLevel, palace, part);
			}
		}

		/// <summary>
		/// Returns the palace part for a section based on whether the section to the right exists.
		/// </summary>
		/// <param name="palace">The palace data.</param>
		/// <param name="wall">Part to return when the section has a right neighbor.</param>
		/// <param name="rightTowerWall">Part to return when the section has no right neighbor.</param>
		/// <param name="sectionIndex">Current section index.</param>
		/// <returns>The selected palace part for the current section.</returns>
		static PalacePart GetPalacePartWithRightNeighbour(PalaceData palace,  PalacePart wall, PalacePart rightTowerWall, int sectionIndex)
		{
			bool hasRightNeighbor = palace.GetPalaceLevel(sectionIndex + 1) > 0;
			return hasRightNeighbor ? wall : rightTowerWall;
		}

		private Picture DrawPalace()
		{
			PalaceData palace = _palace;
			Picture picture = new(320, 200, _background.Palette);
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

		/// <summary>
		/// Creates a static palace picture for use on other screens.
		/// </summary>
		/// <param name="sprites">Optional palace sprite provider.</param>
		/// <param name="palaceData">Optional palace data.</param>
		/// <returns>The rendered palace image.</returns>
		public static Picture CreatePicture(IPalaceSpriteProvider sprites = null, PalaceData palaceData = null)
		{
			PalaceView palaceView = new(false, sprites ?? PalaceSpriteProviderFactory.GetInstance(), false, palaceData);
			return palaceView.DrawPalace();
		}

		private void DrawDebugHelp(int ox, int oy)
		{
			if (!_debug)
			{
				return;
			}

			this.DrawText("F1 Disable Noise", 0, 5, ox + 4, oy + 190)
				.DrawText("F1 Disable Noise", 0, 14, ox + 4, oy + 189);
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
			PalaceData palace = _palace;

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

		public PalaceView(bool buildMode = false, IPalaceSpriteProvider sprites = null, bool debug = false, PalaceData palaceData = null)
		{
			_build = buildMode;
			_debug = debug;
			_sprites = sprites ?? PalaceSpriteProviderFactory.GetInstance();
			_palace = palaceData ?? Human.Palace;

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
			if (buildMode) _currentStage = Stage.Message;
		}
	}
}