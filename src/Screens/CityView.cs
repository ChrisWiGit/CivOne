// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Linq;
using CivOne.Advances;
using CivOne.Buildings;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Governments;
using CivOne.Graphics;
using CivOne.Graphics.Sprites;
using CivOne.Sound;
using CivOne.Sound.Playback;
using CivOne.Wonders;

using UniversityBuilding = CivOne.Buildings.University;

namespace CivOne.Screens
{
	[ScreenResizeable]
	[Modal]
	internal class CityView : BaseScreen
	{
		private const float FADE_STEP = 0.1f;
		private const int NOISE_COUNT = 40;

		private const int MIN_HOUSES = 2;

		/// <summary>Gap in pixels between the feet of the animated figures and the bottom of the city view.</summary>
		private const int ANIMATION_BOTTOM_MARGIN = 5;

		private readonly TextSettings _dialogText;

		private readonly City _city;
		private readonly IProduction? _production;
		private readonly Picture _background;
		private readonly bool _showFoundedScreen;
		private readonly bool _firstView;
		private readonly bool _captured;
		private readonly bool _disorder;
		private readonly bool _weLovePresidentDay;

		/// <summary>Whether this screen asked for a sound, and therefore has to end it again.</summary>
		private bool _startedSound;
		private readonly byte[,]? _noiseMap;
		
		private int _noiseCounter = NOISE_COUNT + 15;

		private int _houseType;

		private readonly Picture _overlay;
		private readonly Picture[]? _invadersOrRevolters;

		/// <summary>Horizontal start offsets of the four civil disorder groups, as in the original game.</summary>
		private static readonly int[] CrowdOffsetsX = [ -96, -36, 0, -56 ];

		/// <summary>Vertical offsets of the four groups, which staggers them in depth.</summary>
		private static readonly int[] CrowdOffsetsY = [ -6, -4, -2, 0 ];

		/// <summary>Baseline the vertical group offsets are measured from.</summary>
		private const int CROWD_BASE_Y = 132;

		/// <summary>Pixels the crowd moves per animation frame.</summary>
		private const int CROWD_STEP = 3;

		/// <summary>Number of animation frames a crowd walk lasts before the screen closes itself.</summary>
		private const int CROWD_FRAMES = 156;

		/// <summary>Leftmost position of the walk, one figure width off screen.</summary>
		private const int CROWD_START_X = -48;

		/// <summary>Animation frame counter of the walking crowd.</summary>
		private int _crowdFrame;

		/// <summary>Per-figure animation and position offsets, so the citizens do not march in lockstep.</summary>
		/// <remarks>
		/// Deliberate deviation from the original Civilization: there every figure of the crowd shares
		/// one walk frame and an exactly even spacing, which makes the rioting and celebrating citizens
		/// look like a drilled formation.
		/// Here each civilian figure gets a random phase and a small random horizontal offset, so the
		/// crowd moves out of step.
		/// Only the civilian crowds (civil disorder, celebration) are staggered.
		/// The invaders of a captured city are soldiers and keep the original's lockstep on purpose.
		/// </remarks>
		private int[]? _walkPhase;
		private int[]? _walkOffsetX;

		private bool _update = true;
		private int OffsetX => Math.Max(0, (Width - 320) / 2);
		private int OffsetY => Math.Max(0, (Height - 200) / 2);
		
		private int _x = 80;
        private int _y = 138;

		private float _fadeStep = 1.0f;
		private bool _skip;

		private string? _buildingFile;

		public event EventHandler? Skipped;

		private void RenderBase()
		{
			this.Clear()
				.AddLayer(_background, OffsetX, OffsetY);
		}
		
		private void FadeColours()
		{
			if (!GFX256) return;

            FadeStep = _fadeStep;

			Palette palette = _background.Palette;
			for (int i = 1; i < 256; i++)
				palette[i] = FadeColour(new Colour(0, 0, 0), _background.OriginalColours[i]);
			this.SetPalette(palette);
		}
		
		/// <summary>
		/// Creates the per-figure offsets that break up the lockstep of a walking crowd.
		/// </summary>
		/// <remarks>
		/// Intentional difference from the original game, see <see cref="_walkPhase"/>.
		/// The original gives group <c>k</c> the fixed phase <c>k * 3</c>, which still looks drilled.
		/// </remarks>
		/// <param name="figureCount">Number of figures in the crowd.</param>
		private void CreateWalkOffsets(int figureCount)
		{
			// Do not use the global gameplay RNG here: opening a disorder or celebration
			// screen would advance the sequence used by later game logic.
			Random localRandom = new(GetRandomSeedFromName(_city.Name));

			_walkPhase = new int[figureCount];
			_walkOffsetX = new int[figureCount];
			for (int i = 0; i < figureCount; i++)
			{
				_walkPhase[i] = localRandom.Next(10);
				_walkOffsetX[i] = localRandom.Next(-6, 7);
			}
		}

		/// <summary>
		/// Returns the animation frame for one figure, shifted by its own phase offset.
		/// </summary>
		/// <param name="index">Index of the figure in the crowd.</param>
		/// <returns>Frame index in the range 0-9.</returns>
		private int WalkFrame(int index)
		{
			int phase = (_walkPhase != null && index < _walkPhase.Length) ? _walkPhase[index] : index * 3;
			return (((_crowdFrame + phase) % 10) + 10) % 10;
		}

		/// <summary>
		/// Returns the horizontal jitter of one figure, so the crowd is not evenly spaced.
		/// </summary>
		/// <param name="index">Index of the figure in the crowd.</param>
		/// <returns>Offset in pixels.</returns>
		private int WalkOffsetX(int index) => (_walkOffsetX != null && index < _walkOffsetX.Length) ? _walkOffsetX[index] : 0;

		/// <summary>
		/// Draws the four walking groups of a civil disorder or a celebration and advances the animation.
		/// </summary>
		/// <remarks>
		/// The original walks the crowd from x = -48 to x = 420 in steps of 3 pixels, which takes 156
		/// frames, and then ends the screen by itself.
		/// A celebration walks the same path in the opposite direction and spreads the groups by 1.5.
		/// </remarks>
		/// <param name="movingLeft">Whether the crowd walks to the left, as a celebration does.</param>
		/// <param name="spreadHalves">Group spacing in halves, 2 for a disorder and 3 for a celebration.</param>
		/// <returns>Always <c>true</c>, the screen has been redrawn.</returns>
		private bool DrawCrowd(bool movingLeft, int spreadHalves)
		{
			if (_crowdFrame >= CROWD_FRAMES)
			{
				SkipAction();
				return true;
			}

			RenderBase();

			int walk = CROWD_START_X + (CROWD_STEP * _crowdFrame);
			if (movingLeft)
			{
				walk = CROWD_START_X + (CROWD_STEP * (CROWD_FRAMES - 1 - _crowdFrame));
			}

			for (int i = 0; i < CrowdOffsetsX.Length; i++)
			{
				if (_invadersOrRevolters == null) continue;

				int x = ((spreadHalves * CrowdOffsetsX[i]) / 2) + walk + WalkOffsetX(i);
				int y = CROWD_BASE_Y + CrowdOffsetsY[i];
				// Citizens walk out of step, an intentional difference from the original game.
				AddClippedLayer(_invadersOrRevolters[WalkFrame(i)], x, y);
			}

			_crowdFrame++;
			return true;
		}

		/// <summary>
		/// Draws an animated figure on top of the city view, clipped to the 320x200 city view area.
		/// </summary>
		/// <remarks>
		/// The marching figures of a civil disorder or a celebration walk in from outside the city
		/// view. On a window larger than 320x200 the area around the view is visible, so a figure
		/// drawn without clipping would keep marching across the black border instead of vanishing
		/// at the edge of the view.
		/// </remarks>
		/// <param name="sprite">The animation frame to draw.</param>
		/// <param name="x">Horizontal position inside the city view.</param>
		/// <param name="y">Vertical position inside the city view.</param>
		private void AddClippedLayer(Picture sprite, int x, int y)
		{
			int left = Math.Max(0, -x);
			int top = Math.Max(0, -y);
			int width = Math.Min(sprite.Width - left, 320 - Math.Max(0, x));
			int height = Math.Min(sprite.Height - top, 200 - Math.Max(0, y));
			if (width <= 0 || height <= 0) return;

			if (left == 0 && top == 0 && width == sprite.Width && height == sprite.Height)
			{
				this.AddLayer(sprite, x + OffsetX, y + OffsetY);
				return;
			}

			using Picture part = sprite[left, top, width, height];
			this.AddLayer(part, x + left + OffsetX, y + top + OffsetY);
		}

		protected override bool HasUpdate(uint gameTick)
		{
			if (gameTick % 4 == 0)
			{
				this.Cycle(64, 79);
				_update = true;
			}

			if (_disorder)
			{
				return DrawCrowd(movingLeft: false, spreadHalves: 2);
			}

			if (_captured)
			{
				RenderBase();
				int frame = _x % 30 / 3;
				if (frame < 0)
				{
					Log($"Warning: Invaders frame is negative: {frame} for x={_x} and player={_city.CityOwnerPlayerIndex}");
					frame = 0;
				}
				for (int i = 7; i >= 0; i--)
				{
					int xx = _x - 65 - (48 * i);
					if (xx + 78 <= 0) continue;
					if (_invadersOrRevolters != null)
					{
						// The invaders of a captured city are soldiers and keep the original's lockstep.
						AddClippedLayer(_invadersOrRevolters[frame], xx, _y);
					}
				}
				_x++;
				return true;
			}
			
			if (_weLovePresidentDay)
			{
				return DrawCrowd(movingLeft: true, spreadHalves: 3);
			}

			if (_noiseMap != null)
			{
				if (_noiseCounter > 0)
				{
					_overlay.ApplyNoise(_noiseMap, _noiseCounter--);
					RenderBase();
					this.AddLayer(_overlay, OffsetX, OffsetY);
					return true;
				}
				return false;
			}

			if (_showFoundedScreen && (_skip || _x > 120))
			{
				_fadeStep -= FADE_STEP;
				if (_fadeStep <= 0.0f)
				{
					Destroy();
					return true;
				}
				FadeColours();
			}
			if (_showFoundedScreen)
			{
				RenderBase();
				this.DrawText(TranslateFormatted("{0} founded: {1}.", _city.Name, Game.GameYear), 5, 5, 161 + OffsetX, 3 + OffsetY, TextAlign.Center);

				int frame = _x % 4;
				this.AddLayer(Resources["SETTLERS"][1, 1 + (16 * frame), 48, 15], _x + OffsetX, 120 + OffsetY)
					.AddLayer(Resources["SETTLERS"][1, 1 + (16 * ((frame + 2) % 4)), 48, 15], _x + 27 + OffsetX, 125 + OffsetY)
					.AddLayer(Resources["SETTLERS"][1, 1 + (16 * ((frame + 3) % 4)), 48, 15], _x + 14 + OffsetX, 131 + OffsetY)
					.AddLayer(Resources["SETTLERS"][1, 1 + (16 * ((frame + 1) % 4)), 48, 15], _x + 40 + OffsetX, 135 + OffsetY);

				if (gameTick % 3 == 0)
					_x++;
				return true;
			}

			if (_firstView && _fadeStep < 1.0f)
			{
				_fadeStep += FADE_STEP;
				if (_fadeStep > 1.0f) _fadeStep = 1.0f;
				FadeColours();
			}

			if (_update)
			{
				RenderBase();
				_update = false;
			}
			return true;
		}

		/// <summary>
		/// Starts a sound and remembers that this screen owns it.
		/// </summary>
		/// <param name="soundName">Name of the sound to play.</param>
		private void PlayScreenSound(string soundName)
		{
			if (!Game.Started || !Game.Sound) return;
			if (Settings.Sound == GameOption.Off) return;

			_startedSound = SoundPlaybackStrategyProvider.Current.PlaySound(soundName);
		}

		/// <summary>
		/// Ends the screen, and with it the sound it started.
		/// </summary>
		/// <remarks>
		/// The win music of a celebration or a founding runs far longer than the screen does, and
		/// would otherwise keep playing underneath whatever comes next.
		/// </remarks>
		protected override void Destroy()
		{
			if (_startedSound)
			{
				_startedSound = false;
				SoundPlaybackStrategyProvider.Abort();
			}

			base.Destroy();
		}

		protected override void Resize(int width, int height)
		{
			base.Resize(width, height);
			_update = true;
		}

		private bool SkipAction()
		{
			if (_fadeStep != 0.0F && _fadeStep != 1.0F) return false;
			if (_noiseCounter > 0 && _noiseCounter < NOISE_COUNT) return false;

			Destroy();
			
			if (Skipped != null)
				Skipped(this, EventArgs.Empty);
			else
				HandleClose();
			return true;
		}
		
		public override bool KeyDown(KeyboardEventArgs args)
		{
			return SkipAction();
		}
		
		public override bool MouseDown(ScreenEventArgs args)
		{
			return SkipAction();
		}

		private void DrawWonder<T>(Picture? picture = null, int x = -1, int y = -1) where T : IWonder
		{
			picture ??= _background;

			if (typeof(T) == typeof(Pyramids))
			{
				picture.AddLayer(Resources["WONDERS2"][131, 54, 188, 29], 133, 0);
			}
			if (typeof(T) == typeof(Colossus))
			{
				picture.AddLayer(Resources["WONDERS2"][88, 97, 124, 39], 170, 0);
			}
			if (typeof(T) == typeof(GreatWall))
			{
				picture.AddLayer(Resources["WONDERS2"][1, 38, 66, 81], 0, 0);
			}
			if (typeof(T) == typeof(HooverDam))
			{
				picture.AddLayer(Resources["WONDERS2"][1, 14, 147, 20], 0, 8);
			}
			if (typeof(T) == typeof(Lighthouse))
			{
				picture.AddLayer(Resources["WONDERS"][229, 116, 40, 83], x, y);
			}
			if (typeof(T) == typeof(HangingGardens))
			{
				picture.AddLayer(Resources["WONDERS"][159, 149, 69, 50], x, y);
			}
			if (typeof(T) == typeof(Oracle))
			{
				picture.AddLayer(Resources["WONDERS"][164, 97, 64, 51], x, y);
			}
			if (typeof(T) == typeof(DarwinsVoyage))
			{
				picture.AddLayer(Resources["WONDERS"][40, 69, 62, 47], x, y);
			}
			if (typeof(T) == typeof(GreatLibrary))
			{
				picture.AddLayer(Resources["WONDERS"][61, 117, 41, 56], x, y);
			}
			if (typeof(T) == typeof(MagellansExpedition))
			{
				picture.AddLayer(Resources["WONDERS"][268, 53, 51, 62], x, y);
			}
			if (typeof(T) == typeof(MichelangelosChapel))
			{
				picture.AddLayer(Resources["WONDERS"][9, 117, 51, 56], x, y);
			}
			if (typeof(T) == typeof(CopernicusObservatory))
			{
				picture.AddLayer(Resources["WONDERS"][126, 1, 57, 53], x, y);
			}
			if (typeof(T) == typeof(ShakespearesTheatre))
			{
				picture.AddLayer(Resources["WONDERS"][276, 1, 43, 51], x, y);
			}
			if (typeof(T) == typeof(IsaacNewtonsCollege))
			{
				picture.AddLayer(Resources["WONDERS"][103, 139, 55, 60], x, y);
			}
			if (typeof(T) == typeof(JSBachsCathedral))
			{
				picture.AddLayer(Resources["WONDERS"][184, 1, 41, 69], x, y);
			}
			if (typeof(T) == typeof(WomensSuffrage))
			{
				picture.AddLayer(Resources["WONDERS"][253, 1, 22, 47], x, y);
			}
			if (typeof(T) == typeof(ManhattanProject))
			{
				picture.AddLayer(Resources["WONDERS"][226, 1, 26, 47], x, y);
			}
			if (typeof(T) == typeof(UnitedNations))
			{
				picture.AddLayer(Resources["WONDERS"][103, 79, 60, 59], x, y);
			}
			if (typeof(T) == typeof(ApolloProgram))
			{
				picture.AddLayer(Resources["WONDERS"][270, 116, 49, 83], x, y);
			}
			if (typeof(T) == typeof(SETIProgram))
			{
				picture.AddLayer(Resources["WONDERS"][63, 1, 62, 54], x, y);
			}
			if (typeof(T) == typeof(CureForCancer))
			{
				picture.AddLayer(Resources["WONDERS"][240, 60, 27, 55], x, y);
			}
		}

		private void DrawWonderOverlay<T>(int x, int y, int offset) where T : IWonder
		{
			DrawWonder<T>(x: x, y: y + offset);
			if (_production is not T)
				DrawWonder<T>(_overlay, x, y + offset);
		}

		private void DrawBuilding<T>(Picture? picture = null, int x = -1, int y = -1) where T : IBuilding
		{
			if (_buildingFile == null)
			{
				_buildingFile = Game.GetPlayer(_city.CityOwnerPlayerIndex)!.HasAdvance<Invention>() ? "CITYPIX3" : "CITYPIX2";
			}

			picture ??= _background;
			if (typeof(T) == typeof(Aqueduct))
			{
				picture.AddLayer(Resources[_buildingFile][51, 151, 49, 49], 0, 72);
			}

			if (typeof(T) == typeof(CityWalls))
			{
				Picture wall = Resources[_buildingFile][251, 101, 43, 49];
				Picture door = Resources[_buildingFile][51, 101, 49, 49];

				for (int xx = 0; xx < 142; xx += 43)
					picture.AddLayer(wall, xx, 108);
				picture.AddLayer(door, 142, 108);
				for (int xx = 191; xx < 320; xx += 43)
					picture.AddLayer(wall, xx, 108);
			}

			if (typeof(T) == typeof(Barracks))
				picture.AddLayer(Resources[_buildingFile][1, 1, 49, 49], x, y);
			if (typeof(T) == typeof(Granary))
				picture.AddLayer(Resources[_buildingFile][1, 51, 49, 49], x, y);
			if (typeof(T) == typeof(Temple))
				picture.AddLayer(Resources[_buildingFile][1, 101, 49, 49], x, y);
			if (typeof(T) == typeof(MarketPlace))
				picture.AddLayer(Resources[_buildingFile][1, 151, 49, 49], x, y);
			if (typeof(T) == typeof(Library))
				picture.AddLayer(Resources[_buildingFile][51, 1, 49, 49], x, y);
			if (typeof(T) == typeof(Courthouse))
				picture.AddLayer(Resources[_buildingFile][51, 51, 49, 49], x, y);
			if (typeof(T) == typeof(Bank))
				picture.AddLayer(Resources[_buildingFile][101, 1, 49, 49], x, y);
			if (typeof(T) == typeof(Cathedral))
				picture.AddLayer(Resources[_buildingFile][101, 51, 49, 49], x, y);
			if (typeof(T) == typeof(UniversityBuilding))
				picture.AddLayer(Resources[_buildingFile][101, 101, 49, 49], x, y);
			if (typeof(T) == typeof(Colosseum))
				picture.AddLayer(Resources[_buildingFile][151, 1, 49, 49], x, y);
			if (typeof(T) == typeof(Factory))
				picture.AddLayer(Resources[_buildingFile][151, 51, 49, 49], x, y);
			if (typeof(T) == typeof(MfgPlant))
				picture.AddLayer(Resources[_buildingFile][151, 101, 49, 49], x, y);
			if (typeof(T) == typeof(SdiDefense))
				picture.AddLayer(Resources[_buildingFile][151, 151, 49, 49], x, y);
			if (typeof(T) == typeof(RecyclingCenter))
				picture.AddLayer(Resources[_buildingFile][201, 1, 49, 49], x, y);
			if (typeof(T) == typeof(NuclearPlant))
				picture.AddLayer(Resources[_buildingFile][201, 151, 49, 49], x, y);
		}

		private void DrawBuildingOverlay<T>(int x, int y, int offset = -18) where T : IBuilding
		{
			DrawBuilding<T>(x: x, y: y + offset);
			if (_production is not T)
				DrawBuilding<T>(_overlay, x, y + offset);
		}

        internal static short GetRandomSeedFromName(string name)
        {
            short number = 0;
            foreach (char c in name)
            {
                byte charByte = (byte)c;
                number += charByte;
            }

            return number;
        }

        private CityViewMap[,] GetCityMap
		{
			get
			{
				// fire-eggs 20190711 do NOT set the global RNG! Using a consistant RNG for
				// the city view is great (we get a consistant city), but modifying the 
				// global RNG means a less random game!
				//Common.SetRandomSeedFromName(_city.Name);
				Random localRandom = new(GetRandomSeedFromName(_city.Name));
				_houseType = localRandom.Next(2);

				CityViewMap[,] cityMap = new CityViewMap[18,11];
				for (int yy = 0; yy < 11; yy++)
				for (int xx = 0; xx < 18; xx++)
				{
					if (xx == 6 || xx == 11 || yy == 2 || yy == 6)
						cityMap[xx, yy] = CityViewMap.Road;
					if ((xx < 2 && yy < 3) || (xx > 16 && yy > 8))
						cityMap[xx, yy] = CityViewMap.Occupied;
				}
				
				int houseCount = 0;

				// This is experimental code, not the same as the original game
				int ww = 4 + _city.Size;
				int hh = 4 + (_city.Size - 1);
				if (ww > 18) ww = 18;
				if (hh > 11) hh = 11;

				int bx = (ww / 2) + ((18 - ww) / 2);
				int by = hh / 2;
				for (int ii = 0; ii < _city.Size; ii++)
				{
					houseCount += PlaceHouses(localRandom, cityMap, ww, hh, ref bx, ref by);
					for (int i = 0; i < 1000; i++)
					{
						bx = localRandom.Next(ww) + ((18 - ww) / 2);
						by = localRandom.Next(hh);
						if (cityMap[bx, by] != CityViewMap.Empty) continue;
						for (int ix = -1; ix < 2; ix++)
							for (int iy = -1; iy < 2; iy++)
							{
								if (Math.Abs(ix) == Math.Abs(iy)) continue;
								if (bx + ix < ((18 - ww) / 2)) continue;
								if (bx + ix >= ww + ((18 - ww) / 2)) continue;
								if (by + iy < 0) continue;
								if (by + iy >= hh) continue;
								if (cityMap[bx + ix, by + iy] != CityViewMap.Empty) { i = 1000; break; }
							}
					}
				}

				for (int yy = 0; yy < 11; yy++)
				for (int xx = 0; xx < 18; xx++)
				{
					if ((int)cityMap[xx, yy] > 1)
					{
						if ((xx == 0 || (cityMap[xx - 1, yy] != CityViewMap.House && cityMap[xx - 1, yy] != CityViewMap.Tree)) &&
							(xx == 17 || (cityMap[xx + 1, yy] != CityViewMap.House && cityMap[xx + 1, yy] != CityViewMap.Tree)) &&
							(yy == 0 || (cityMap[xx, yy - 1] != CityViewMap.House && cityMap[xx, yy - 1] != CityViewMap.Tree)) &&
							(yy == 10 || (cityMap[xx, yy + 1] != CityViewMap.House && cityMap[xx, yy + 1] != CityViewMap.Tree)))
						{
							// Keep at least MIN_HOUSES actual houses in the city view:
							// trees may always be cleared, but isolated houses are only cleared
							// while we still have more than the minimum house count.
							bool isHouse = cityMap[xx, yy] == CityViewMap.House;
							bool canClear = !isHouse || houseCount > MIN_HOUSES;
							if (canClear)
							{
								if (isHouse)
								{
									houseCount--;
								}

								// FIX: some houses may be placed on xx:0,10;yy:0,17 (above) by PlaceHouses by sheer luck.
								// and may be erased until no houses are left, so we test for it.
								cityMap[xx, yy] = CityViewMap.Empty;
							}
						}
					}
					if (cityMap[xx, yy] != CityViewMap.Road) continue;

					bool blocked =
							xx == 0 || (int)cityMap[xx - 1, yy] > 1 ||
							xx == 17 || (int)cityMap[xx + 1, yy] > 1 ||
							yy == 0 || (int)cityMap[xx, yy - 1] > 1 ||
							yy == 10 || (int)cityMap[xx, yy + 1] > 1;
					if (blocked) continue;

					cityMap[xx, yy] = CityViewMap.Empty;
				}
				
				for (int yy = 0; yy < 11; yy++)
				for (int xx = 0; xx < 18; xx++)
				{
					if (cityMap[xx, yy] != CityViewMap.Empty) continue;
					if (!(xx == 6 || xx == 11 || yy == 2 || yy == 6)) continue;

					bool blocked =
						xx == 0  || (int)cityMap[xx - 1, yy] != 1 ||
						xx == 17 || (int)cityMap[xx + 1, yy] != 1 ||
						yy == 0  || (int)cityMap[xx, yy - 1] != 1 ||
						yy == 10 || (int)cityMap[xx, yy + 1] != 1;

					if (blocked) continue;
					
					cityMap[xx, yy] = CityViewMap.Road;
				}

					// After cleanup, only close real 1-tile gaps
					// so roads stay connected without making the panorama artificially dense.
					CloseSingleRoadGaps(cityMap);

				
				foreach (Type type in new Type[] { typeof(Barracks), typeof(Granary), typeof(Temple), typeof(MarketPlace), typeof(Library), typeof(Courthouse), typeof(Bank), typeof(Cathedral), typeof(UniversityBuilding), typeof(Colosseum), typeof(Factory), typeof(MfgPlant), typeof(SdiDefense), typeof(RecyclingCenter), typeof(NuclearPlant), typeof(Lighthouse), typeof(HangingGardens), typeof(Oracle), typeof(DarwinsVoyage), typeof(GreatLibrary), typeof(MagellansExpedition), typeof(MichelangelosChapel), typeof(CopernicusObservatory), typeof(ShakespearesTheatre), typeof(IsaacNewtonsCollege), typeof(JSBachsCathedral), typeof(WomensSuffrage), typeof(ManhattanProject), typeof(UnitedNations), typeof(ApolloProgram), typeof(SETIProgram), typeof(CureForCancer) })
				{
					if (_city.HasBuilding(type) || _city.HasWonder(type))
					{
						int sizeX = 2, sizeY = 2;

						CityViewMap id;
						if (type == typeof(Barracks)) id = CityViewMap.Barracks;
						else if (type == typeof(Granary)) id = CityViewMap.Granary;
						else if (type == typeof(Temple)) id = CityViewMap.Temple;
						else if (type == typeof(MarketPlace)) id = CityViewMap.MarketPlace;
						else if (type == typeof(Library)) id = CityViewMap.Library;
						else if (type == typeof(Courthouse)) id = CityViewMap.Courthouse;
						else if (type == typeof(Bank)) id = CityViewMap.Bank;
						else if (type == typeof(Cathedral)) id = CityViewMap.Cathedral;
						else if (type == typeof(UniversityBuilding)) id = CityViewMap.University;
						else if (type == typeof(Colosseum)) id = CityViewMap.Colosseum;
						else if (type == typeof(Factory)) id = CityViewMap.Factory;
						else if (type == typeof(MfgPlant)) id = CityViewMap.MfgPlant;
						else if (type == typeof(SdiDefense)) id = CityViewMap.SdiDefense;
						else if (type == typeof(RecyclingCenter)) id = CityViewMap.RecyclingCenter;
						else if (type == typeof(NuclearPlant)) id = CityViewMap.NuclearPlant;
						else if (type == typeof(Lighthouse)) id = CityViewMap.Lighthouse;
						else if (type == typeof(HangingGardens)) { id = CityViewMap.HangingGardens; }
						else if (type == typeof(Oracle)) { id = CityViewMap.Oracle; }
						else if (type == typeof(DarwinsVoyage)) { id = CityViewMap.DarwinsVoyage; }
						else if (type == typeof(GreatLibrary)) { id = CityViewMap.GreatLibrary; }
						else if (type == typeof(MagellansExpedition)) { id = CityViewMap.MagellansExpedition; }
						else if (type == typeof(MichelangelosChapel)) { id = CityViewMap.MichelangelosChapel; }
						else if (type == typeof(CopernicusObservatory)) { id = CityViewMap.CopernicusObservatory; }
						else if (type == typeof(ShakespearesTheatre)) { id = CityViewMap.ShakespearesTheatre; }
						else if (type == typeof(IsaacNewtonsCollege)) { id = CityViewMap.IsaacNewtonsCollege; }
						else if (type == typeof(JSBachsCathedral)) { id = CityViewMap.JSBachsCathedral; }
						else if (type == typeof(WomensSuffrage)) { id = CityViewMap.WomensSuffrage; }
						else if (type == typeof(ManhattanProject)) { id = CityViewMap.ManhattanProject; }
						else if (type == typeof(UnitedNations)) { id = CityViewMap.UnitedNations; }
						else if (type == typeof(ApolloProgram)) { id = CityViewMap.ApolloProgram; }
						else if (type == typeof(SETIProgram)) { id = CityViewMap.SETIProgram; }
						else if (type == typeof(CureForCancer)) { id = CityViewMap.CureForCancer; }
						else continue;

						// The original reserves the same footprint for every wonder, regardless of
						// sprite size: the cells (gx - 1 .. gx + 3, gy - 1 .. gy + 1) around the anchor.
						// Buildings keep their 2x2 block.
						bool isWonder = typeof(IWonder).IsAssignableFrom(type);

						// A city holding many wonders runs out of 5x3 blocks on this 18x11 grid, and the
						// wonders late in the list (Apollo Program, SETI Program, Cure for Cancer) would
						// never be placed and therefore never drawn. Shrink the reserved area instead of
						// dropping the wonder.
						(int X0, int X1, int Y0, int Y1)[] footprints = isWonder
							? [ (-1, 3, -1, 1), (0, 2, 0, 1), (0, 1, 0, 0) ]
							: [ (0, sizeX - 1, 0, sizeY - 1) ];

						foreach ((int offX0, int offX1, int offY0, int offY1) in footprints)
						{
							bool placed = false;
							for (int i = 0; i < 1000; i++)
							{
								int xx = localRandom.Next(15) + 1;
								int yy = localRandom.Next(10);
								if (xx == 6 || xx == 11 || yy == 2 || yy == 6) continue;
								if (xx == 5 || xx == 10 || yy == 1 || yy == 5) continue;
								if (xx + offX0 < 0 || xx + offX1 >= cityMap.GetLength(0)) continue;
								if (yy + offY0 < 0 || yy + offY1 >= cityMap.GetLength(1)) continue;
								if ((int)cityMap[xx, yy] > 3) continue;
								bool invalid = false;
								for (int oy = offY0; oy <= offY1; oy++)
								for (int ox = offX0; ox <= offX1; ox++)
								{
									if ((int)cityMap[xx + ox, yy + oy] <= 3) continue;
									invalid = true;
									break; 
								}
								if (invalid) continue;

								cityMap[xx, yy] = id;
								for (int oy = offY0; oy <= offY1; oy++)
								for (int ox = offX0; ox <= offX1; ox++)
								{
									if (ox == 0 && oy == 0) continue;
									cityMap[xx + ox, yy + oy] = CityViewMap.Occupied;
								}
								placed = true;
								break;
							}
							if (placed) break;
						}
					}
				}

				return cityMap;
			}
		}

		private static int PlaceHouses(Random localRandom, CityViewMap[,] cityMap, int ww, int hh, ref int bx, ref int by)
		{
			const int MaxRepeats = 4;
			const int MaxTries = 16;
			const int HouseChance = 6;
			const int MinRel = -1;
			const int MaxRel = 2;
			const int HouseTypes = 8;
			const int PlacementWidth = 18;

			int placed = 0;

			// Fix: no houses shown, so
			// repeat up to MaxRepeats times or until MIN_HOUSES houses are placed
			for (int repeat = 1; repeat <= MaxRepeats && placed < MIN_HOUSES; repeat++)
			{
				for (int t = 0; t < MaxTries; t++)
				{
					int relX = localRandom.Next(MinRel, MaxRel);
					int relY = localRandom.Next(MinRel, MaxRel);

					if (relX == 0 && relY == 0)
					{
						continue;
					}

					bx += relX;
					by += relY;

					while (bx < ((PlacementWidth - ww) / 2))
					{
						bx++;
					}
					while (bx >= ww + ((PlacementWidth - ww) / 2))
					{
						bx--;
					}
					while (by < 0)
					{
						by++;
					}
					while (by >= hh)
					{
						by--;
					}

					int type = localRandom.Next(HouseTypes);

					if (cityMap[bx, by] != CityViewMap.Empty)
					{
						continue;
					}

					if (type < HouseChance)
					{
						cityMap[bx, by] = CityViewMap.House;
						placed++;
					}
					else
					{
						cityMap[bx, by] = CityViewMap.Tree;
					}
				}
			}
			return placed;
		}

		/// <summary>
		/// Closes only single-tile gaps on the designated road axes (x=6/11, y=2/6)
		/// when roads already connect from left/right or top/bottom.
		/// For crossing tiles, it also fills the gap when at least two adjacent
		/// roads are present. This keeps connectivity stable without globally
		/// refilling entire road axes.
		/// </summary>
		/// <param name="cityMap">The computed city map to patch locally.</param>
		private static void CloseSingleRoadGaps(CityViewMap[,] cityMap)
		{
			for (int yy = 0; yy < cityMap.GetLength(1); yy++)
			for (int xx = 0; xx < cityMap.GetLength(0); xx++)
			{
				if (cityMap[xx, yy] != CityViewMap.Empty) continue;
				if (!(xx == 6 || xx == 11 || yy == 2 || yy == 6)) continue;

				bool horizontalGap =
					(yy == 2 || yy == 6)
					&& xx > 0 && xx < cityMap.GetUpperBound(0)
					&& cityMap[xx - 1, yy] == CityViewMap.Road
					&& cityMap[xx + 1, yy] == CityViewMap.Road;

				bool verticalGap =
					(xx == 6 || xx == 11)
					&& yy > 0 && yy < cityMap.GetUpperBound(1)
					&& cityMap[xx, yy - 1] == CityViewMap.Road
					&& cityMap[xx, yy + 1] == CityViewMap.Road;

				bool crossingGap = false;
				if ((xx == 6 || xx == 11) && (yy == 2 || yy == 6))
				{
					int adjacentRoads = 0;
					if (xx > 0 && cityMap[xx - 1, yy] == CityViewMap.Road) adjacentRoads++;
					if (xx < cityMap.GetUpperBound(0) && cityMap[xx + 1, yy] == CityViewMap.Road) adjacentRoads++;
					if (yy > 0 && cityMap[xx, yy - 1] == CityViewMap.Road) adjacentRoads++;
					if (yy < cityMap.GetUpperBound(1) && cityMap[xx, yy + 1] == CityViewMap.Road) adjacentRoads++;
					crossingGap = adjacentRoads >= 2;
				}

				if (horizontalGap || verticalGap || crossingGap)
					cityMap[xx, yy] = CityViewMap.Road;
			}
		}

		private void DrawBuildings()
		{
			CityViewMap[,] cityMap = GetCityMap;

			if (_city.Wonders.Any(b => b is Pyramids))
			{
				DrawWonder<Pyramids>();
				if (_production is not Pyramids)
					DrawWonder<Pyramids>(_overlay);
			}
			if (_city.Wonders.Any(b => b is Colossus))
			{
				DrawWonder<Colossus>();
				if (_production is not Colossus)
					DrawWonder<Colossus>(_overlay);
			}
			if (_city.Wonders.Any(b => b is HooverDam))
			{
				DrawWonder<HooverDam>();
				if (_production is not HooverDam)
					DrawWonder<HooverDam>(_overlay);
			}
			if (_city.Wonders.Any(b => b is GreatWall))
			{
				DrawWonder<GreatWall>();
				if (_production is not GreatWall)
					DrawWonder<GreatWall>(_overlay);
			}

			if (_city.Buildings.Any(b => b is Aqueduct))
			{
				DrawBuilding<Aqueduct>();
				if (_production is not Aqueduct)
					DrawBuilding<Aqueduct>(_overlay);
			}

			int stage = (int)Math.Floor((double)(Game.GetPlayer(_city.CityOwnerPlayerIndex)!.Advances.Length - 9) / 2);
			// Painter's algorithm: draw row by row from the back (high yy) to the front (low yy).
			// Iterating column-major instead would let a tile from a further-back row overdraw
			// an already drawn tile that stands in front of it, which made roads cover buildings.
			for (int yy = 10; yy >= 0; yy--)
			for (int xx = 0; xx < 18; xx++)
			{
				int dx = 0 + (16 * xx) + (yy * 8);
				int dy = 106 - (yy * 8);
				Picture building;
				switch (cityMap[xx, yy])
				{
					case CityViewMap.House:
						int centerDistance = Math.Max(Math.Abs(9 - xx), yy);
						if (stage >= 20)
						{
							if (_city.Size > 8 && RandomService.NextInt((_city.Size - 7) * 2) > centerDistance)
							{
								if (RandomService.NextInt(10) > 5)
								{
									building = Resources["CITYPIX1"][1 + (32 * 8), (RandomService.NextInt(10) > 5) ? 1 : 33, 31, 31];
								}
								else
								{
									building = Resources["CITYPIX1"][1 + (32 * 9), (RandomService.NextInt(10) > 5) ? 1 : 33, 31, 31];
								}
							}
							else
							{
								if (RandomService.NextInt(10) > 5)
								{
									building = Resources["CITYPIX1"][1 + (32 * 6), 33, 31, 31];
								}
								else
								{
									building = Resources["CITYPIX1"][1 + (32 * 7), 33, 31, 31];
								}
							}
						}
						else if (stage >= 16)
						{
							if (RandomService.NextInt(stage - 16) > centerDistance)
							{
								if (RandomService.NextInt(10) > 5)
								{
									building = Resources["CITYPIX1"][1 + (32 * 6), 1, 31, 31];
								}
								else
								{
									building = Resources["CITYPIX1"][1 + (32 * 7), 1, 31, 31];
								}
							}
							else
							{
								if (RandomService.NextInt(10) > 5)
								{
									building = Resources["CITYPIX1"][1 + (32 * 4), 33, 31, 31];
								}
								else
								{
									building = Resources["CITYPIX1"][1 + (32 * 5), 33, 31, 31];
								}
							}
						}
						else if (stage >= 7)
						{
							if (RandomService.NextInt(stage - 7) > centerDistance)
							{
								if (RandomService.NextInt(10) > 5)
								{
									building = Resources["CITYPIX1"][1 + (32 * 4), 1, 31, 31];
								}
								else
								{
									building = Resources["CITYPIX1"][1 + (32 * 5), 1, 31, 31];
								}
							}
							else
							{
								if (RandomService.NextInt(10) > 5)
								{
									building = Resources["CITYPIX1"][1 + (32 * 2), 33, 31, 31];
								}
								else
								{
									building = Resources["CITYPIX1"][1 + (32 * 3), 33, 31, 31];
								}
							}
						}
						else if (stage >= 1)
						{
							if (RandomService.NextInt(stage) > centerDistance)
							{
								if (RandomService.NextInt(10) > 5)
								{
									if (RandomService.NextInt((stage - 5) * 4) > centerDistance)
									{
										building = Resources["CITYPIX1"][1 + (32 * 2), 33, 31, 31];
									}
									else
									{
										building = Resources["CITYPIX1"][1 + (32 * 2), 1, 31, 31];
									}
								}
								else
								{
									if (RandomService.NextInt((stage - 5) * 4) > centerDistance)
									{
										building = Resources["CITYPIX1"][1 + (32 * 3), 33, 31, 31];
									}
									else
									{
										building = Resources["CITYPIX1"][1 + (32 * 3), 1, 31, 31];
									}
								}
							}
							else
							{
								building = Resources["CITYPIX1"][1 + (32 * _houseType), 33, 31, 31];
							}
						}
						else
						{
							if (RandomService.NextInt(-3 - stage) > centerDistance)
							{
								building = Resources["CITYPIX1"][1 + (32 * _houseType), 33, 31, 31];
							}
							else
							{
								building = Resources["CITYPIX1"][1 + (32 * _houseType), 1, 31, 31];
							}
						}
						break;
					case CityViewMap.Tree:
						building = Resources["CITYPIX1"][0, 65, 24, 8];
						dx -= 5;
						dy += 24;
						break;
					case CityViewMap.Road:
						Direction road = 0;
						if (yy < cityMap.GetUpperBound(1) && cityMap[xx, yy + 1] == CityViewMap.Road) road |= Direction.North;
						if (xx < cityMap.GetUpperBound(0) && cityMap[xx + 1, yy] == CityViewMap.Road) road |= Direction.East;
						if (yy > 0 && cityMap[xx, yy - 1] == CityViewMap.Road) road |= Direction.South;
						if (xx > 0 && cityMap[xx - 1, yy] == CityViewMap.Road) road |= Direction.West;

						int sx = (int)road;
						int sy = 65;
						if (sx == 0) continue;
						if (sx > 7) sy += 8;
						sx = sx % 8 * 24;
						if (Game.GetPlayer(_city.CityOwnerPlayerIndex)!.HasAdvance<Automobile>()) sy += 16;
						building = Resources["CITYPIX1"][sx, sy, 24, 8];
						dx -= 5;
						dy += 24;
						break;
					case CityViewMap.Barracks:
						DrawBuildingOverlay<Barracks>(dx, dy);
						continue;
					case CityViewMap.Granary:
						DrawBuildingOverlay<Granary>(dx, dy);
						continue;
					case CityViewMap.Temple:
						DrawBuildingOverlay<Temple>(dx, dy);
						continue;
					case CityViewMap.MarketPlace:
						DrawBuildingOverlay<MarketPlace>(dx, dy);
						continue;
					case CityViewMap.Library:
						DrawBuildingOverlay<Library>(dx, dy);
						continue;
					case CityViewMap.Courthouse:
						DrawBuildingOverlay<Courthouse>(dx, dy);
						continue;
					case CityViewMap.Bank:
						DrawBuildingOverlay<Bank>(dx, dy);
						continue;
					case CityViewMap.Cathedral:
						DrawBuildingOverlay<Cathedral>(dx, dy);
						continue;
					case CityViewMap.University:
						DrawBuildingOverlay<UniversityBuilding>(dx, dy);
						continue;
					case CityViewMap.Colosseum:
						DrawBuildingOverlay<Colosseum>(dx, dy);
						continue;
					case CityViewMap.Factory:
						DrawBuildingOverlay<Factory>(dx, dy);
						continue;
					case CityViewMap.MfgPlant:
						DrawBuildingOverlay<MfgPlant>(dx, dy);
						continue;
					case CityViewMap.SdiDefense:
						DrawBuildingOverlay<SdiDefense>(dx, dy);
						continue;
					case CityViewMap.RecyclingCenter:
						DrawBuildingOverlay<RecyclingCenter>(dx, dy);
						continue;
					case CityViewMap.NuclearPlant:
						DrawBuildingOverlay<NuclearPlant>(dx, dy);
						continue;
					case CityViewMap.Lighthouse:
						DrawWonderOverlay<Lighthouse>(dx, dy, -52);
						continue;
					//
					case CityViewMap.HangingGardens:
						DrawWonderOverlay<HangingGardens>(dx, dy, -19);
						continue;
					case CityViewMap.Oracle:
						DrawWonderOverlay<Oracle>(dx, dy, -20);
						continue;
					case CityViewMap.DarwinsVoyage:
						DrawWonderOverlay<DarwinsVoyage>(dx, dy, -16);
						continue;
					case CityViewMap.GreatLibrary:
						DrawWonderOverlay<GreatLibrary>(dx, dy, -25);
						continue;
					case CityViewMap.MagellansExpedition:
						DrawWonderOverlay<MagellansExpedition>(dx, dy, -31);
						continue;
					case CityViewMap.MichelangelosChapel:
						DrawWonderOverlay<MichelangelosChapel>(dx, dy, -25);
						continue;
					case CityViewMap.CopernicusObservatory:
						DrawWonderOverlay<CopernicusObservatory>(dx, dy, -22);
						continue;
					case CityViewMap.ShakespearesTheatre:
						DrawWonderOverlay<ShakespearesTheatre>(dx, dy, -20);
						continue;
					case CityViewMap.IsaacNewtonsCollege:
						DrawWonderOverlay<IsaacNewtonsCollege>(dx, dy, -29);
						continue;
					case CityViewMap.JSBachsCathedral:
						DrawWonderOverlay<JSBachsCathedral>(dx, dy, -38);
						continue;
					case CityViewMap.WomensSuffrage:
						DrawWonderOverlay<WomensSuffrage>(dx, dy, -16);
						continue;
					case CityViewMap.ManhattanProject:
						DrawWonderOverlay<ManhattanProject>(dx, dy, -16);
						continue;
					case CityViewMap.UnitedNations:
						DrawWonderOverlay<UnitedNations>(dx, dy, -28);
						continue;
					case CityViewMap.ApolloProgram:
						DrawWonderOverlay<ApolloProgram>(dx, dy, -52);
						continue;
					case CityViewMap.SETIProgram:
						DrawWonderOverlay<SETIProgram>(dx, dy, -23);
						continue;
					case CityViewMap.CureForCancer:
						DrawWonderOverlay<CureForCancer>(dx, dy, -24);
						continue;
					default: continue;
				}
				_background.AddLayer(building, dx, dy);
				_overlay.AddLayer(building, dx, dy);
			}

			if (_city.Buildings.Any(b => b is CityWalls))
			{
				DrawBuilding<CityWalls>();
				if (_production is not CityWalls)
					DrawBuilding<CityWalls>( _overlay);
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (!disposing)
			{
				return;
			}

			_background.Dispose();
			_overlay.Dispose();
			base.Dispose(disposing);
		}

		public static CityView Capture(City city, string []? message)
		{
			return new CityView(city, message, captured: true);
		}
		
		public static CityView Disorder(City city)
		{
			return new CityView(city, disorder: true);
		}

		public static CityView WeLovePresidentDay(City city)
		{
			return new CityView(city, weLovePresidentDay: true);
		}
		
		/// <summary>
		/// Creates the animated screen shown when a city is founded.
		/// </summary>
		/// <param name="city">The city that was founded.</param>
		/// <param name="playFoundingMusic">
		/// Whether this founding is the one that establishes the civilization, which the original
		/// marks with its win music.
		/// </param>
		/// <returns>The screen.</returns>
		public static CityView FoundCityWithAnimation(City city, bool playFoundingMusic = false)
		{
			return new CityView(city, showFoundedScreen: true, playFoundingMusic: playFoundingMusic);
		}

		public static CityView FoundCity(City city)
		{
			return new CityView(city);
		}

		public CityView(City city, string[]? message = null, bool showFoundedScreen = false, bool firstView = false, IProduction? production = null, bool captured = false, bool disorder = false, bool weLovePresidentDay = false, bool playFoundingMusic = false)
		{
			_dialogText = TextSettings.ShadowText(15, 5);
			_dialogText.FontId = 5;
			_skip = false;

			_city = city;
			_production = production;
			_background = new Picture(Resources["HILL"]);
			_showFoundedScreen = showFoundedScreen;
			_firstView = firstView;

			Palette = _background.Palette;
			_overlay = new Picture(_background);

			if (showFoundedScreen)
			{
				if (playFoundingMusic) PlayScreenSound(SoundNames.MusicWin);
				return;
			}

			DrawBuildings();
			RenderBase();

			// ReSharper disable once AssignmentInConditionalExpression
			if (_captured = captured)
			{
				Picture invaders;
				int xx = 0, yy = 2, ww = 78, hh = 60;
				if (Game.CurrentPlayer.HasAdvance<Conscription>())
				{
					invaders = Resources["INVADERS"];
				}
				else if (Game.CurrentPlayer.HasAdvance<Gunpowder>())
				{
					invaders = Resources["INVADER2"];
				}
				else
				{
					invaders = Resources["INVADER3"];
					xx = 1;
					yy = 1;
					ww = 78;
					hh = 65;
				}
				_y = 200 - ANIMATION_BOTTOM_MARGIN - hh;

				_invadersOrRevolters = new Picture[10];
				for (int ii = 0; ii < 10; ii++)
				{
					int frameX = ii % 4;
					int frameY = (ii - frameX) / 4;
					_invadersOrRevolters[ii] = invaders[xx + (frameX * (ww + 1)), yy + (frameY * (hh + 1)), ww, hh];
				}
				_x = 0;

				if (message != null)
					drawMessage(message);
			}

			// ReSharper disable once AssignmentInConditionalExpression
			if (_disorder = disorder)
			{
				Picture revolters;
				int xx = 1, yy = 1, ww, hh;
				if (Game.CurrentPlayer.HasAdvance<Advances.University>())
				{
					ww = 78;
					hh = 63;
					revolters = Resources["RIOT"];
				}
				else
				{
					ww = 74;
					hh = 65;
					revolters = Resources["RIOT2"];
				}
				_invadersOrRevolters = new Picture[10];
				for (int ii = 0; ii < 10; ii++)
				{
					int frameX = ii % 4;
					int frameY = (ii - frameX) / 4;
					_invadersOrRevolters[ii] = revolters[xx + (frameX * (ww + 1)), yy + (frameY * (hh + 1)), ww, hh];
				}
				CreateWalkOffsets(CrowdOffsetsX.Length);
				string[] lines = TranslateFormattedArray("Civil disorder in\n{0}! Mayor\nflees in panic.", city.Name);
				drawMessage(lines);
			}

			// ReSharper disable once AssignmentInConditionalExpression
			if (_weLovePresidentDay = weLovePresidentDay)
			{
				int xx = 1, yy = 1, ww = 78, hh = 65;

				var resourceName = Game.CurrentPlayer.HasAdvance<Industrialization>() ? "LOVE2" : "LOVE1";
				Picture marchers = Resources[resourceName];
				_invadersOrRevolters = new Picture[10];
				for (int ii = 0; ii < 10; ii++)
				{
					int frameX = ii % 4;
					int frameY = (ii - frameX) / 4;
					_invadersOrRevolters[ii] = marchers[xx + (frameX * (ww + 1)), yy + (frameY * (hh + 1)), ww, hh];
				}
				CreateWalkOffsets(CrowdOffsetsX.Length);

				string leaderTitle = Translate("President");
				if (Game.CurrentPlayer.Government is Governments.Monarchy)
					leaderTitle = Translate("King");
				if (Game.CurrentPlayer.Government is Governments.Communism)
					leaderTitle = Translate("Comrade");
				if (Game.CurrentPlayer.Government is Despotism)
					leaderTitle = Translate("Emperor");

				string[] lines = TranslateFormattedArray("'We Love the {0}'\nday celebrated in\n{1}!", leaderTitle, city.Name);
				drawMessage(lines);
			}

			if (production is ICivilopedia civilopedia)
			{
				_noiseMap = new byte[320, 200];
				for (int x = 0; x < 320; x++)
				{
					for (int y = 0; y < 200; y++)
					{
						_noiseMap[x, y] = (byte)RandomService.NextInt(1, NOISE_COUNT);
					}
				}

				string[] lines = TranslateFormattedArray("{0} builds\n{1}.", _city.Name, civilopedia.TranslatedName);
				int width = lines.Max(l => Resources.GetTextSize(5, l).Width) + 12;
				using Picture dialog = new(width, 39);
				dialog
					.Tile(Pattern.PanelGrey, 1, 1)
					.DrawRectangle()
					.DrawRectangle3D(1, 1, width - 2, 37)
					.DrawText(lines[0], 5, 6, _dialogText)
					.DrawText(lines[1], 5, 21, _dialogText);
					

				foreach (Picture picture in new[] { _background, _overlay })
				{
					picture.AddLayer(dialog, 80, 10);
				}
				return;
			}

			// While the disorder animation plays, RenderBase() redraws _background on every frame and
			// layers the (mostly transparent) revolter sprite on top - the static population row would
			// still show through the gaps around the animated figure, so skip drawing it here.
			if (captured || _disorder) return;

			_background.DrawText(_city.Name, 5, 5, 161, 3, TextAlign.Center)
				.DrawText(_city.Name, 5, 15, 160, 2, TextAlign.Center)
				.DrawText(Game.GameYear, 5, 5, 161, 16, TextAlign.Center)
				.DrawText(Game.GameYear, 5, 15, 160, 15, TextAlign.Center);

			if (firstView)
			{
				_fadeStep = 0.0f;
				FadeColours();
				return;
			}

			// The celebration gets the original's win music, a city view opened plainly its short
			// flourish. Disorder stays quiet here: the alarm for it is played from City.Update.
			if (weLovePresidentDay)
			{
				PlayScreenSound(SoundNames.MusicWin);
			}
			else if (!disorder)
			{
				PlayScreenSound(SoundNames.EventCityViewOpened);
			}

			int i = 0;
			int group = -1;
			int offsetX = 24;
			bool modern = Human.HasAdvance<Industrialization>();
			foreach (Citizen citizen in _city.GetCitizens())
			{
				// POP.PIC only has 9 slots; civil disorder citizens reuse the Unhappy slot, which is
				// already painted red in the original artwork. The original draws no difference between
				// ordinary and parked unhappiness here, so both share the sprite.
				Citizen spriteSource = citizen switch
				{
					Citizen.RedShirtMale => Citizen.UnhappyMale,
					Citizen.RedShirtFemale => Citizen.UnhappyFemale,
					_ => citizen,
				};

				int previousGroup = group;
				group = Common.CitizenGroup(spriteSource);
				if (previousGroup >= 0 && group != previousGroup)
				{
					// The original leaves no gap between the happy and the content block, 8 pixels before
					// the unhappy block and 12 before the specialists.
					if (group == 2) offsetX += 8;
					else if (group == 3) offsetX += 12;
				}

				int sx = ((int)spriteSource * 35) + 1, sy = modern ? 1 : 52;
				int sw = 34, sh = modern ? 50 : 52;
				int dx = offsetX + (11 * i++), dy = 140;
				_background.AddLayer(Resources["POP"][sx, sy, sw, sh], dx, dy);
			}

			void drawMessage(string[] lines)
			{
				int width = lines.Max(l => Resources.GetTextSize(5, l).Width) + 12;
				using Picture dialog = new(width, 54);
				dialog
					.Tile(Pattern.PanelGrey, 1, 1)
					.DrawRectangle()
					.DrawRectangle3D(1, 1, width - 2, 52)
					.DrawText(lines[0], 5, 6, _dialogText)
					.DrawText(lines[1], 5, 21, _dialogText)
					.DrawText(lines[2], 5, 36, _dialogText);

				_background.AddLayer(dialog, 80, 8);
			}
		}
	}
}