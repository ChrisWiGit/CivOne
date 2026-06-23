// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using CivOne.Advances;
using CivOne.Enums;
using CivOne.Screens;
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
			Common.GamePlay?.CenterOnPoint(moveTarget.X, moveTarget.Y);
			Show nuke = CreateNukeAnimation(moveTarget);

			PlaySound(moveTarget.City != null ? "airnuke" : "s_nuke");
			nuke.Done += (s, a) => DestroyUnitsInNuclearBlast(relX, relY);

			GameTask.Enqueue(nuke);
		}

		private Show CreateNukeAnimation(ITile moveTarget)
		{
			int viewX = Common.GamePlay!.X;
			int viewY = Common.GamePlay!.Y;

			int xx = moveTarget.X - viewX;
			while (xx < 0)
			{
				xx += Map.WIDTH;
			}
			while (xx >= Map.WIDTH)
			{
				xx -= Map.WIDTH;
			}

			int yy = moveTarget.Y - viewY;

			xx *= 16;
			yy *= 16;
			return Show.Nuke(xx, yy);
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
					tile.City.Size /= 2;
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