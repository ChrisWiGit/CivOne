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
using CivOne.IO;
using CivOne.Tasks;
using CivOne.Tiles;
using CivOne.UserInterface;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CivOne.Units
{
	internal class Settlers : BaseUnitLand
	{
		internal void SetStatus(bool[] bits)
		{
			// TODO initialize MovesSkip value
			// TODO need to set MovesSkip from savefile format [see TODO in GetStatus]
			// CW: using FuelOrProgress/WorkProgress for this now instead of MovesSkip
			if (bits[1] && !bits[6] && !bits[7])
			{
				order = Order.Road;
			}
			if (!bits[1] && bits[6] && !bits[7])
			{
				order = Order.Irrigate;
			}
			if (!bits[1] && !bits[6] && bits[7])
			{
				order = Order.Mines;
			}
			if (!bits[1] && bits[6] && bits[7])
			{
				order = Order.Fortress;
			}
			if (bits[1] && !bits[6] && bits[7])
			{
				order = Order.ClearPollution;
			}
		}

		/// <summary>
		/// This method mimics the original CIV1 settlers behavior of building roads and railroads.
		/// 1. Cannot build road/railroad on railroad and in city.
		/// 2. Can build roads/railroads on ocean (always) if on ship.
		///    Would be nice in a future to implement a bridge on ocean that can be used by land units. 
		/// 3. Multiple settlers won't build road/railroad twice as fast (because each settler has its own progress).
		///    !! We should start using Dependency Injection for using Services (e.g. RoadService) to handle this and
		///    switch its implementation if another behavior is required. !!
		///    In this way we can implement a different behavior for settlers in the future (e.g. handling this "bug")
		/// 4. Cancel building a road and start again will not reset progress (fast road building bug).
		///    A settler will even take its progress away to another tile and use it there.
		/// 5. WorkProgress is stored in FuelOrProgress property to be saved in a save file.
		/// </summary>
		/// <returns></returns>
		public bool BuildRoad()
		{
			if (Tile.RailRoad)
			{
				order = Order.None;
				return false;
			}

			if (Tile.City != null)
			{
				return false;
			}

			if (Game.CurrentPlayer.HasAdvance<RailRoad>() && Tile.Road)
			{
				order = Order.Road;
				WorkOnRoads();
				SkipTurn(Tile.RailRoadCost);
				return true;
			}

			if (Tile.Road)
			{
				// end any other settler to stop building road if another settler has already built a road here
				order = Order.None;
				// we don't reset FuelOrProgress here, because it is used to track progress
				return false;
			}

			if ((Tile is River) && !Game.CurrentPlayer.HasAdvance<BridgeBuilding>())
			{
				return false;
			}

			order = Order.Road;
			WorkOnRoads();
			SkipTurn(Tile.RoadCost);

			return true;
		}


		void WorkOnRoads()
		{
			if (!Location.Road && (WorkProgress < Location.RoadCost) ||
				Location.Road && (WorkProgress < Location.RailRoadCost))
			{
				WorkProgress++;
				return;
			}

			if (Location.Road)
			{
				Location.RailRoad = true;
			}
			Location.Road = true;
			order = Order.None;
			WorkProgress = 0;
		}

		public bool BuildIrrigation()
		{
			ITile tile = Map[X, Y];
			if (tile.Irrigation || tile.IsOcean) // already irrigated or illogical: ignore
			{
				return false;
			}

			// Changing terrain type
			if (tile.IrrigationChangesTerrain())
			{
				order = Order.Irrigate;
				SkipTurn(tile.IrrigationCost);
				return true;
			}

			if (!tile.TerrainAllowsIrrigation())
			{
				if (Human == Owner)
					GameTask.Enqueue(Message.Error("-- Civilization Note --", TextFile.Instance.GetGameText("ERROR/NOIRR")));
				return false;
			}

			if (tile.AllowIrrigation() || tile.Type == Terrain.River)
			{
				order = Order.Irrigate;
				SkipTurn(tile.IrrigationCost);
				return true;
			}

			if (Human == Owner)
				GameTask.Enqueue(Message.Error("-- Civilization Note --", TextFile.Instance.GetGameText("ERROR/NOIRR")));
			return false;
		}

		public bool BuildMines()
		{
			ITile tile = Map[X, Y];
			if (!(tile.Mine) && ((tile is Desert) || (tile is Hills) || (tile is Mountains) || (tile is Jungle) || (tile is Grassland) || (tile is Plains) || (tile is Swamp)))
			{
				order = Order.Mines;
				SkipTurn(tile.MiningCost);
				return true;
			}
			return false;
		}

		public bool BuildFortress()
		{
			if (!Game.CurrentPlayer.HasAdvance<Construction>())
				return false;

			ITile tile = Map[X, Y];
			if (!tile.IsOcean && !(tile.Fortress) && tile.City == null)
			{
				order = Order.Fortress;
				SkipTurn(5); // TODO IUnit.FortressCost?
				return true;
			}
			return false;
		}

		public void CancelOrder()
		{
			order = Order.None;
			// do not reset FuelOrProgress (bug in classic CIV1)
		}


		public override void NewTurn()
		{
			base.NewTurn();

			if (order == Order.Road)
			{
				BuildRoad();
			}
			else if (order == Order.Irrigate)
			{
				Map[X, Y].Irrigation = false;
				Map[X, Y].Mine = false;
				if (Map[X, Y] is Forest)
				{
					Map.ChangeTileType(X, Y, Terrain.Plains);
				}
				else if ((Map[X, Y] is Jungle) || (Map[X, Y] is Swamp))
				{
					Map.ChangeTileType(X, Y, Terrain.Grassland1);
				}
				else
				{
					Map[X, Y].Irrigation = true;
				}
				order = Order.None;
			}
			else if (order == Order.Mines)
			{
				if ((Map[X, Y] is Jungle) || (Map[X, Y] is Grassland) || (Map[X, Y] is Plains) || (Map[X, Y] is Swamp))
				{
					Map[X, Y].Irrigation = false;
					Map[X, Y].Mine = false;
					Map.ChangeTileType(X, Y, Terrain.Forest);
				}
				else
				{
					Map[X, Y].Irrigation = false;
					Map[X, Y].Mine = true;
				}
				order = Order.None;
			}
			else if (order == Order.Fortress)
			{
				Map[X, Y].Fortress = true;
				order = Order.None;
			}
		}

		private MenuItem<int> MenuFoundCity() => MenuItem<int>
			.Create((Map[X, Y].City == null) ? "Found New City" : "Add to City")
			.SetShortcut("b")
			.OnSelect((s, a) => GameTask.Enqueue(Orders.FoundCity(this)));

		private MenuItem<int> MenuBuildRoad() => MenuItem<int>
			.Create((Map[X, Y].Road) ? "Build RailRoad" : "Build Road")
			.SetShortcut("r")
			.OnSelect((s, a) => BuildRoad());

		private MenuItem<int> MenuBuildIrrigation() => MenuItem<int>
			.Create((Map[X, Y] is Forest) ? "Change to Plains" :
					((Map[X, Y] is Jungle) || (Map[X, Y] is Swamp)) ? "Change to Grassland" :
					"Build Irrigation")
			.SetShortcut("i")
			.SetEnabled(Map[X, Y].AllowIrrigation() || Map[X, Y].IrrigationChangesTerrain())
			.OnSelect((s, a) => GameTask.Enqueue(Orders.BuildIrrigation(this)));

		private MenuItem<int> MenuBuildMines() => MenuItem<int>
			.Create(((Map[X, Y] is Jungle) || (Map[X, Y] is Grassland) || (Map[X, Y] is Plains) || (Map[X, Y] is Swamp)) ?
					"Change to Forest" : "Build Mines")
			.SetShortcut("m")
			.OnSelect((s, a) => GameTask.Enqueue(Orders.BuildMines(this)));

		private MenuItem<int> MenuBuildFortress() => MenuItem<int>
			.Create("Build fortress")
			.SetShortcut("f")
			.SetEnabled(Game.CurrentPlayer.HasAdvance<Construction>())
			.OnSelect((s, a) => GameTask.Enqueue(Orders.BuildFortress(this)));

		public override IEnumerable<MenuItem<int>> MenuItems
		{
			get
			{
				ITile tile = Map[X, Y];

				yield return MenuNoOrders();
				if (!tile.IsOcean)
				{
					yield return MenuFoundCity();
				}
				if (!tile.IsOcean && (!tile.Road || (Human.HasAdvance<RailRoad>() && !tile.RailRoad)))
				{
					// TODO classic CIV allowed building road/railroad on ocean
					yield return MenuBuildRoad();
				}
				if (!tile.Irrigation && (tile.TerrainAllowsIrrigation() || tile.IrrigationChangesTerrain())) // ((tile is Desert) || (tile is Grassland) || (tile is Hills) || (tile is Plains) || (tile is River) || (tile is Forest) || (tile is Jungle) || (tile is Swamp)))
				{
					yield return MenuBuildIrrigation();
				}
				if (!tile.Mine && ((tile is Desert) || (tile is Hills) || (tile is Mountains) || (tile is Jungle) || (tile is Grassland) || (tile is Plains) || (tile is Swamp)))
				{
					yield return MenuBuildMines();
				}
				if (!tile.IsOcean && !tile.Fortress)
				{
					yield return MenuBuildFortress();
				}
				//
				yield return MenuWait();
				yield return MenuSentry();
				yield return MenuGoTo();
				if (tile.Irrigation || tile.Mine || tile.Road || tile.RailRoad)
				{
					yield return MenuPillage();
				}
				if (tile.City != null)
				{
					yield return MenuHomeCity();
				}
				yield return null;
				yield return MenuDisbandUnit();
			}
		}

		/// <summary>
		/// A settlers-specific version of SkipTurn to manage MovesSkip
		/// </summary>
		/// <param name="turns">The number of turns to set MovesSkip</param>
		public void SkipTurn(int turns = 0)
		{
			base.SkipTurn();
			MovesSkip = turns;
			bool cheatEnabled = Human == Owner && Settings.Instance.AutoSettlers; // cheat for human
			if (turns > 1 && cheatEnabled) MovesSkip = 1;
		}

		public Settlers() : base(4, 0, 1, 1)
		{
			Type = UnitType.Settlers;
			Name = "Settlers";
			RequiredTech = null;
			ObsoleteTech = null;
			SetIcon('D', 1, 1);
			Role = UnitRole.Settler;
			FuelOrProgress = 0;
		}
	}
}