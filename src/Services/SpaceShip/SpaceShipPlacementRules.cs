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

namespace CivOne.Services.SpaceShip
{
	/// <summary>
	/// Canonical placement engine for spaceship parts based on <see cref="ISpaceShipSlotBlueprint"/> order maps and footprints.
	/// </summary>
	public class SpaceShipPlacementRules : ISpaceShipPlacementRules
	{
		private readonly ISpaceShipSlotBlueprint _slotBlueprint;

		public SpaceShipPlacementRules(ISpaceShipSlotBlueprint slotBlueprint)
		{
			_slotBlueprint = slotBlueprint ?? throw new System.ArgumentNullException(nameof(slotBlueprint));
		}

		private static bool IsRealPart(SpaceShipComponentType partType) => partType != SpaceShipComponentType.Empty;

		private (int x, int y)[] GetFamilyOrderList(SpaceShipComponentType partType) => partType switch
		{
			SpaceShipComponentType.Structural
			or SpaceShipComponentType.StructureHorizontal
			or SpaceShipComponentType.StructureVertical
			or SpaceShipComponentType.StructureNode => _slotBlueprint.StructuralOrder,

			SpaceShipComponentType.Component => _slotBlueprint.ComponentOrder,
			SpaceShipComponentType.FuelComponent => _slotBlueprint.FuelOrder,
			SpaceShipComponentType.PropulsionComponent => _slotBlueprint.PropulsionOrder,

			SpaceShipComponentType.Module => _slotBlueprint.ModuleOrder,
			SpaceShipComponentType.CommandModule => FindSlots(_slotBlueprint.SlotMap, 'C'),
			SpaceShipComponentType.LifeSupportModule => _slotBlueprint.LifeSupportOrder,
			SpaceShipComponentType.HabitationModule => _slotBlueprint.HabitationOrder,
			SpaceShipComponentType.SolarPanelModule => _slotBlueprint.SolarPanelOrder,

			_ => []
		};

		private static bool IsGenericFamilyType(SpaceShipComponentType partType) =>
		partType is SpaceShipComponentType.Structural
		or SpaceShipComponentType.Component
		or SpaceShipComponentType.Module;

		private static bool IsFootprintFree(SpaceShipComponentType[,] grid, int ox, int oy, int w, int h)
		{
			if (ox + w > grid.GetLength(0) || oy + h > grid.GetLength(1))
				return false;

			for (int dy = 0; dy < h; dy++)
				for (int dx = 0; dx < w; dx++)
					if (grid[ox + dx, oy + dy] != SpaceShipComponentType.Empty)
						return false;

			return true;
		}

		private static void StampFootprint(SpaceShipComponentType[,] grid, int ox, int oy, int w, int h, SpaceShipComponentType type)
		{
			for (int dy = 0; dy < h; dy++)
				for (int dx = 0; dx < w; dx++)
					grid[ox + dx, oy + dy] = type;
		}

		private static (int x, int y)[] FindSlots(string[] map, char symbol)
		{
			var result = new List<(int x, int y)>();
			for (int y = 0; y < map.Length; y++)
			{
				for (int x = 0; x < map[y].Length; x++)
				{
					if (map[y][x] == symbol)
					{
						result.Add((x, y));
					}
				}
			}

			return [.. result];
		}

		private bool HasAvailableFamilySlot(SpaceShipComponentType[,] grid, SpaceShipComponentType partType)
		{
			(int x, int y)[] orderList = GetFamilyOrderList(partType);
			bool isGeneric = IsGenericFamilyType(partType);

			foreach ((int x, int y) in orderList)
			{
				char symbol = _slotBlueprint.SlotMap[y][x];
				if (!_slotBlueprint.Footprint.TryGetValue(symbol, out (int w, int h) fp))
					continue;

				SpaceShipComponentType concrete = SpaceShipComponentTypeMapper.FromSlotSymbol(symbol);
				if (!isGeneric && concrete != partType)
					continue;

				if (IsFootprintFree(grid, x, y, fp.w, fp.h))
					return true;
			}

			return false;
		}

		private bool TryFindAndPlace(SpaceShipComponentType[,] grid, SpaceShipComponentType partType)
		{
			(int x, int y)[] orderList = GetFamilyOrderList(partType);
			bool isGeneric = IsGenericFamilyType(partType);

			foreach ((int x, int y) in orderList)
			{
				char symbol = _slotBlueprint.SlotMap[y][x];
				if (!_slotBlueprint.Footprint.TryGetValue(symbol, out (int w, int h) fp))
					continue;

				SpaceShipComponentType concrete = SpaceShipComponentTypeMapper.FromSlotSymbol(symbol);
				if (concrete == SpaceShipComponentType.Empty)
					continue;

				if (!isGeneric && concrete != partType)
					continue;

				if (!IsFootprintFree(grid, x, y, fp.w, fp.h))
					continue;

				StampFootprint(grid, x, y, fp.w, fp.h, concrete);
				return true;
			}

			return false;
		}

		public virtual bool CanAddPart(IPlayerSpaceRace player, SpaceShipComponentType partType)
		{
			if (player == null || !IsRealPart(partType))
				return false;

			return HasAvailableFamilySlot(player.SpaceShipGrid, partType);
		}

		public bool TryAddPart(IPlayerSpaceRace player, SpaceShipComponentType partType)
		{
			if (!CanAddPart(player, partType))
				return false;

			return TryFindAndPlace(player.SpaceShipGrid, partType);
		}
	}
}
