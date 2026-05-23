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
	[ScreenResizeable]
	internal class Conquest : BaseScreen
	{
		private struct Enemy
		{
			public string DestroyYear;
			public ILeader Leader;
			public ICivilization Civilization;
		}

		private const int NOISE_COUNT = 64;
		private int _noiseCounter;
		private readonly byte[,] _noiseMap;
		private bool _update = true;

		private readonly Enemy[] _enemies;

		private int _enemy = 0;
		private int _step = 0;

		private int _timer = 0;

		private Picture _background, _overlay;
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
			string line2 = TranslateFormatted("{0} civilization!", _enemies[_enemy].Civilization.Name);
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

		private Point GetPoint(int number)
		{
			return number switch
			{
				0 => new Point(8, 49),
				1 => new Point(284, 49),
				2 => new Point(54, 49),
				3 => new Point(238, 49),
				4 => new Point(100, 49),
				5 => new Point(192, 49),
				6 => new Point(146, 49),
				// high up pictures of leaders
				7 => new Point(8, 8),
				8 => new Point(284, 8),
				9 => new Point(54, 8),
				10 => new Point(238, 8),
				11 => new Point(100, 8),
				12 => new Point(192, 8),
				13 => new Point(146, 8),
				_ => new Point(8, 49),
			};
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
					_overlay = new Picture(_background);
					_overlay.AddLayer(_enemies[_enemy].Leader.GetPortrait(FaceState.Angry), 90, 0);
					_noiseCounter = NOISE_COUNT + 2;
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
					_overlay.ApplyNoise(_noiseMap, --_noiseCounter);
					if (_noiseCounter < -2) _timer = 90;
					this.Clear(OpaqueBlackColour)
						.AddLayer(_background, OffsetX, OffsetY)
						.AddLayer(_overlay, OffsetX, OffsetY);
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
					_noiseMap[x, y] = (byte)Common.Random.Next(1, NOISE_COUNT);
				}

			BaseCivilization.BuddyCivilization getBuddyCiv =
				BaseCivilization.GetBuddyCivilizationSupplier(
					Common.Random.InitialSeed, Game.Competition, Game.HumanPlayer.Civilization.PreferredPlayerNumber);

			_enemies = Game.GetReplayData<ReplayData.CivilizationDestroyed>()
				.Where(x => x.DestroyedById == Game.HumanPlayer.Civilization.Id)
				.Select(x =>
				{
					ICivilization civ = getBuddyCiv(x.DestroyedId);

					// Console.WriteLine($"Civilization {civ.Name} ({civ.Id}) destroyed by {Game.HumanPlayer.Civilization.Name} ({Game.HumanPlayer.Civilization.Id}/{civ.PreferredPlayerNumber}) in year {Common.YearString((ushort)x.Turn)}");

					return new Enemy
					{
						DestroyYear = Common.YearString((ushort)x.Turn),
						Leader = civ.Leader,
						Civilization = civ,
					};
				}
			).ToArray();

			SetPalette();
		}
	}
}