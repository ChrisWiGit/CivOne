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
using System.Drawing;
using System.Linq;
using CivOne.Civilizations;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.Leaders;

namespace CivOne.Screens
{
	#pragma warning disable CA1822 // Mark members as static
	[ScreenResizeable]
	internal class Conquest : BaseScreen
	{
		private struct Enemy
		{
			public string DestroyYear;
			public ILeader Leader;
			public string CivilizationName;
		}

		private const int NOISE_COUNT = 64;

		/// <summary>
		/// Number of small leader portraits the board can show: two rows of seven.
		/// Once they are all taken, the board is wiped and filled again from the first slot.
		/// </summary>
		private const int PORTRAIT_SLOTS = 14;

		private static readonly Point[] PortraitPoints =
		[
			// lower row of leader portraits
			new(8, 49), new(284, 49), new(54, 49), new(238, 49), new(100, 49), new(192, 49), new(146, 49),
			// upper row of leader portraits
			new(8, 8), new(284, 8), new(54, 8), new(238, 8), new(100, 8), new(192, 8), new(146, 8)
		];

		private int _noiseCounter;
		private readonly byte[,] _noiseMap;
		private bool _update = true;

		private readonly Enemy[] _enemies;

		private int _enemy;
		private int _step;

		private int _timer;
		private Picture _background;
		private Picture? _overlay;

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
						return (byte)i;
				}
				return 5;
			}
		}

		private string HumanName => Game.CurrentPlayer.LeaderName;

		private void DrawMessageLines()
		{
			string line1 = TranslateFormatted("{0}: {1} destroy", _enemies[_enemy].DestroyYear, Human.Civilization.NamePlural);
			string line2 = TranslateFormatted("{0} civilization!", _enemies[_enemy].CivilizationName);
			this.DrawText(line1, 5, 20, 159 + OffsetX, 152 + OffsetY, TextAlign.Center)
				.DrawText(line1, 5, 23, 159 + OffsetX, 151 + OffsetY, TextAlign.Center)
				.DrawText(line2, 5, 20, 159 + OffsetX, 168 + OffsetY, TextAlign.Center)
				.DrawText(line2, 5, 23, 159 + OffsetX, 167 + OffsetY, TextAlign.Center);
		}


		private void SetPalette()
		{
			Palette palette = _enemies[_enemy].Leader.GetPortrait().Palette;
			for (int i = 64; i < 144; i++)
			{
				Palette[i] = palette[i];
			}
		}

		/// <summary>
		/// Returns the board position for the given enemy, wrapping around once the board is full.
		/// </summary>
		/// <param name="number">The index of the enemy in <see cref="_enemies"/>.</param>
		/// <returns>The top left corner of the portrait, in unscaled 320x200 coordinates.</returns>
		private static Point GetPoint(int number) => PortraitPoints[number % PORTRAIT_SLOTS];

		/// <summary>
		/// Replaces the board with an empty one, so the portraits start again at the first slot.
		/// </summary>
		private void ClearBoard()
		{
			Picture previous = _background;
			_background = Resources["SLAM1"];
			previous.Dispose();
		}

		protected override bool HasUpdate(uint gameTick)
		{
			if (_enemy >= 0 && ++_timer > NOISE_COUNT)
			{
				_timer = 0;
				_step++;

				Console.WriteLine($"Conquest step {_step} ");
				if (_step == 2)
				{
					_overlay?.Dispose();
					_overlay = new Picture(_background);
					_overlay.AddLayer(_enemies[_enemy].Leader.GetPortrait(FaceState.Angry), 90, 0);
					_noiseCounter = NOISE_COUNT + 2;
					if (_enemy > 0 && _enemy % PORTRAIT_SLOTS == 0)
					{
						// The board is full: wipe it and start over instead of drawing on top of the
						// portraits that are already there. The overlay still holds the old board, so the
						// noise transition dissolves the previous portraits away.
						ClearBoard();
					}
					_background.AddLayer(_enemies[_enemy].Leader.PortraitSmall, GetPoint(_enemy));
				}
				if (_step == 3)
				{
					_step = 0;
					_enemy++;
					if (_enemy > _enemies.GetUpperBound(0))
					{
						_step = 4;
						_enemy = -1;
						_timer = 0;
						Console.WriteLine($"Exit step 3");

						return true;
					}
				}

				if (_enemy >= 0)
				{
					SetPalette();
				}
			} else if (++_timer > NOISE_COUNT &&_step == 5)
			{
				Destroy();
				return true;
			}

			switch (_step)
			{
				case 0:
					this.Clear(OpaqueBlackColour)
						.AddLayer(_background, OffsetX, OffsetY)
						.AddLayer(_enemies[_enemy].Leader.GetPortrait(FaceState.Smiling), 90 + OffsetX, 0 + OffsetY);
					break;
				case 1:
					this.Clear(OpaqueBlackColour)
						.AddLayer(_background, OffsetX, OffsetY)
						.AddLayer(_enemies[_enemy].Leader.GetPortrait(FaceState.Angry), 90 + OffsetX, 0 + OffsetY);
					DrawMessageLines();
					break;
				case 2:
					if (_overlay != null)
					{
						_overlay.ApplyNoise(_noiseMap, --_noiseCounter);
						if (_noiseCounter < -2) _timer = 90;
						this.Clear(OpaqueBlackColour)
							.AddLayer(_background, OffsetX, OffsetY)
							.AddLayer(_overlay, OffsetX, OffsetY);
					}
					DrawMessageLines();
					break;
				case 4:
					this.Clear(OpaqueBlackColour)
						.AddLayer(_background, OffsetX, OffsetY)
						.DrawText(Translate("The entire world hails"), 5, 22, 159 + OffsetX, 153 + OffsetY, TextAlign.Center)
						.DrawText(TranslateFormatted("{0} the CONQUEROR!", HumanName), 5, 22, 159 + OffsetX, 168 + OffsetY, TextAlign.Center);

					break;
			}

			if (_update) return false;
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
			if (_step < 1)
			{
				_timer = NOISE_COUNT;
				_step = 1;
			}
			if (_step == 4)
			{
				_timer = 0;
				_step = 5;
			}
			return true;
		}

		public Conquest()
		{
			_background = Resources["SLAM1"];

			Palette = _background.Palette;

			this.Clear(OpaqueBlackColour).AddLayer(_background, OffsetX, OffsetY);

			_noiseMap = new byte[320, 200];
			for (int x = 0; x < 320; x++)
				for (int y = 0; y < 200; y++)
				{
					_noiseMap[x, y] = RandomService.NextByte(1, NOISE_COUNT);
				}

			// Already in replay order, so destructions that share a turn keep the order they happened in.
			IReadOnlyList<DestroyedCivilizationEntry> destroyedCivilizations =
				new DestroyedCivilizationResolverDelegate().Resolve(
					Game.GetReplayData<ReplayData>(), Common.Random!.InitialSeed, Game.Competition,
					Game.HumanPlayerId, Game.HumanPlayer.Civilization);

			CivilizationNameDelegate civilizationNames = new();
			_enemies = [.. destroyedCivilizations
				.Where(entry => entry.Destroyed.DestroyedById == Game.HumanPlayerId)
				.Select(entry => new Enemy
				{
					DestroyYear = Common.YearString((ushort)entry.Destroyed.Turn),
					Leader = entry.Civilization.Leader,
					// Several players can share one civilization once there are more players than civilizations,
					// so use the same numbered name the destroyed player had ("Roman II").
					CivilizationName = civilizationNames.Build(entry.Civilization, entry.Occurrence).TribeName
						?? entry.Civilization.Name,
				}
			)];

			SetPalette();
		}

		protected override void Dispose(bool disposing)
		{
			if (!disposing)
			{
				return;
			}

			_overlay?.Dispose();
			_overlay = null;
			_background.Dispose();
			base.Dispose(disposing);
		}
	}
}