// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using CivOne.Advances;
using CivOne.Buildings;
using CivOne.Enums;
using CivOne.Tasks;
using CivOne.Tiles;
using CivOne.Wonders;

namespace CivOne.Units
{
	internal class Nuclear : BaseUnitAir
	{
		public override void Explore()
		{
			Explore(2);
		}

		public override void SkipTurn()
		{
			FuelLeft = 0;

			base.SkipTurn();
		}

		internal override bool HandleConfront(ITile moveTarget, int relX, int relY, MoveUnit? movement)
		{
			RegisterHostileAction();
			HandleNuclear(moveTarget, relX, relY);

			return PostConfront(moveTarget, relX, relY, movement);
		}

		private void HandleNuclear(ITile moveTarget, int relX, int relY)
		{	
			var nukeSuccess = RandomService.Hit(90);
			ITile? cityWithSdi = GetCityWithSDI(moveTarget);
			if (cityWithSdi != null && nukeSuccess)
			{
				Game.DisbandUnit(this);
				GameTask.Enqueue(Message.Advisor(Advisor.Defense, false, 
					TranslateFormattedArray("{0} defense system\ndestroyed the nuclear missile\nbefore it could reach its target.", cityWithSdi.City.Name)));
				
				// TODO: Should let all players (AI) know that the nuke was intercepted,
				// This will trigger a war declaration from the player that launched 
				// the nuke, and a war declaration from the player that was nuked.
				return;
			}
			Common.GamePlay?.CenterOnPoint(moveTarget.X, moveTarget.Y);
			Show nuke = CreateNukeAnimation(moveTarget);

			PlaySound(moveTarget.City != null ? "airnuke" : "s_nuke");
			nuke.Done += (s, a) => DestroyUnitsInNuclearBlast(relX, relY);

			GameTask.Enqueue(nuke);
		}

		private static ITile? GetCityWithSDI(ITile tile)
		{
			foreach (ITile t in Map.QueryMapPart(tile.X - 1, tile.Y - 1, 3, 3))
			{
				if (HasCitySDIBuilding(t))
				{
					return t;
				}
			}
			return null;
		}


		private static bool HasCitySDIBuilding(ITile moveTarget)
		{
			if (moveTarget.City == null) return false;
			foreach (var building in moveTarget.City.Buildings)
			{
				if (building is SdiDefense)
				{
					return true;
				}
			}
			return false;
		}

		private static Show CreateNukeAnimation(ITile moveTarget)
		{
			// because we center on moveTarget, we need to calculate the pixel position
			// of the nuke animation relative to the center of the screen to ensure it appears in the correct location.
			(int xx, int yy) = GetNukeAnimationPixelPosition(moveTarget);

			return Show.Nuke(xx, yy);
		}

		private static (int X, int Y) GetNukeAnimationPixelPosition(ITile moveTarget)
		{
			int viewX = Common.GamePlay!.X;
			int viewY = Common.GamePlay!.Y;

			int tileX = moveTarget.X - viewX;
			while (tileX < 0)
			{
				tileX += Map.WIDTH;
			}
			while (tileX >= Map.WIDTH)
			{
				tileX -= Map.WIDTH;
			}

			int tileY = moveTarget.Y - viewY;

			const int tileSize = 16;
			return (tileX * tileSize, tileY * tileSize);
		}

		private void DestroyUnitsInNuclearBlast(int relX, int relY)
		{
			foreach (ITile tile in Map.QueryMapPart(X + relX - 1, Y + relY - 1, 3, 3)) // NOSONAR: tile.Units must be re-evaluated after each Game.DisbandUnit() call; selecting tile.Units would capture a stale array snapshot and can cause repeated processing or an endless loop.
			{
				tile.Irrigation = false;
				tile.Road = false;
				tile.RailRoad = false;
				tile.Fortress = false;
				tile.Hut = false;
				tile.Mine = false;

				while (tile.Units.Length > 0)
				{
					Game.DisbandUnit(tile.Units[0]);
				}
				if (tile.City != null)
				{
					tile.City.Size = (byte)Math.Max(1, tile.City.Size / 2);
					continue;
				}
				// CW: 16% chance is not the same as the original game.
				if (RandomService.Hit(16))
				{
					tile.Pollution = true;
				}
			}
		}
		
		public Nuclear() : base(16, 99, 0, 16)
		{
			Type = UnitType.Nuclear;
			Name = "Nuclear";
			TranslatedName = Translate("Nuclear");
			RequiredTech = new Rocketry();
			RequiredWonder = new ManhattanProject();
			ObsoleteTech = null;
			SetIcon('D', 0, 0);
		}
	}
}