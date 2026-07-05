// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using CivOne.Enums;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace CivOne.Services.SpaceShip
{
	/// <summary>
	/// Defines the 12x12 slot layout for spaceship part placement.
	/// </summary>
	public class SpaceShipSlotBlueprint : ISpaceShipSlotBlueprint
	{
		public string[] SlotMap { get; }
		public string[] StructuralOrderMap { get; }
		public string[] PropulsionOrderMap { get; }
		public string[] FuelOrderMap { get; }
		public string[] SolarPanelOrderMap { get; }
		public string[] HabitationOrderMap { get; }
		public string[] LifeSupportOrderMap { get; }
		public IReadOnlyDictionary<char, (int w, int h)> Footprint { get; }
		public (int x, int y)[] StructuralOrder { get; }
		public int MaxStructuralSlots { get; }
		public (int x, int y)[] PropulsionOrder { get; }
		public (int x, int y)[] FuelOrder { get; }
		public (int x, int y)[] SolarPanelOrder { get; }
		public (int x, int y)[] LifeSupportOrder { get; }
		public (int x, int y)[] HabitationOrder { get; }
		public SpaceShipOverlaySprite[] OverlaySprites { get; }
		public (int x, int y)[] ComponentOrder { get; }
		public (int x, int y)[] ModuleOrder { get; }

		public SpaceShipSlotBlueprint()
		{
			// This is the canonical source for all spaceship slot layout and part placement rules.
			// For every slot symbol, there must be a corresponding entry in the Footprint dictionary. 
			// The order maps determine the order in which parts are placed when the player clicks "Add Part". 
			SlotMap =
			[
				".........FP.",
				".S...S..#==.",
				"........|FP.",
				"========#FP.",
				"H.L.H.L.#==.",
				"........|FP.",
				"L.H.L.H.|FP.",
				"........#==.",
				"========#FP.",
				".S...S..|FP.",
				"........#==.",
				".........FP.",
			];

			// The order maps use the same coordinates as SlotMap, but instead of slot symbols, 
			// they have markers that indicate the order of placement for each part type.
			// 1-9, then A-Z, then a-z indicate placement order for that part type. '.' indicates no placement.
			StructuralOrderMap =
			[
				"............",
				"........UXY.",
				"........T...",
				"SRKJIHBA9...",
				"........256.",
				"........1...",
				"........3...",
				"........478.",
				"WVPNGFEDC...",
				"........L...",
				"........MOQ.",
				"............",
			];

			// Fuel order map.
			// 1-9,A-Z,a-z indicate placement order.
			FuelOrderMap =
			[
				".........8..",
				"............",
				".........6..",
				".........4..",
				"............",
				".........1..",
				".........2..",
				"............",
				".........3..",
				".........5..",
				"............",
				".........7..",
			];

			// Propulsion order map.
			// 1-9,A-Z,a-z indicate placement order.
			PropulsionOrderMap =
			[
				"..........8.",
				"............",
				"..........6.",
				"..........4.",
				"............",
				"..........1.",
				"..........2.",
				"............",
				"..........3.",
				"..........5.",
				"............",
				"..........7.",
			];

			// Solar panel order map.
			// 1-9,A-Z,a-z indicate placement order.
			SolarPanelOrderMap =
			[
				"............",
				".4...1......",
				"............",
				"............",
				"............",
				"............",
				"............",
				"............",
				"............",
				".3...2......",
				"............",
				"............",
			];

			// Habitation module order map.
			// 1-9,A-Z,a-z indicate placement order.
			HabitationOrderMap =
			[
				"............",
				"............",
				"............",
				"............",
				"4...2.......",
				"............",
				"..3...1.....",
				"............",
				"............",
				"............",
				"............",
				"............",
			];

			// Life support module order map.
			// 1-9,A-Z,a-z indicate placement order.
			LifeSupportOrderMap =
			[
				"............",
				"............",
				"............",
				"............",
				"..3...1.....",
				"............",
				"4...2.......",
				"............",
				"............",
				"............",
				"............",
				"............",
			];

			// The footprint dictionary defines the width and height of each slot symbol in the SlotMap. 
			Footprint = new Dictionary<char, (int w, int h)>
			{
				['='] = (1, 1), // horizontal structure
				['|'] = (1, 1), // vertical structure
				['#'] = (1, 1), // structure node
				['F'] = (1, 1), // fuel component
				['P'] = (1, 1), // propulsion component
				['C'] = (2, 2), // command module
				['L'] = (2, 2), // life support module
				['H'] = (2, 2), // habitation module
				['S'] = (2, 2), // solar panel module
			};
			// Additional sprite on different layers at these coordinates with pixel offsets:
			OverlaySprites = [
				new SpaceShipOverlaySprite(7, 5, SpaceShipOverlaySpriteIds.CommandModule, SpaceShipComponentType.CommandModule, 1).WithPixelOffset(0, 0),
			];

			const string OrderMarkers = "123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

			StructuralOrder = BuildOrderFromMap(StructuralOrderMap, OrderMarkers);
			MaxStructuralSlots = CountSlots(SlotMap, '=', '|', '#');
			PropulsionOrder = BuildOrderFromMap(PropulsionOrderMap, OrderMarkers);
			FuelOrder = BuildOrderFromMap(FuelOrderMap, OrderMarkers);
			SolarPanelOrder = BuildOrderFromMap(SolarPanelOrderMap, OrderMarkers);
			LifeSupportOrder = BuildOrderFromMap(LifeSupportOrderMap, OrderMarkers);
			HabitationOrder = BuildOrderFromMap(HabitationOrderMap, OrderMarkers);
			ComponentOrder = Interleave(PropulsionOrder, FuelOrder);
			ModuleOrder = Concat(Interleave(LifeSupportOrder, HabitationOrder), SolarPanelOrder);
		}

		// creates a list of coordinates in the order parts should be placed based on the provided order map and the canonical slot map.
		private static (int x, int y)[] BuildOrderFromMap(string[] map, string orderedMarkers)
		{
			var byMarker = new Dictionary<char, (int x, int y)>();
			for (int y = 0; y < map.Length; y++)
			{
				for (int x = 0; x < map[y].Length; x++)
				{
					char marker = map[y][x];
					if (marker == '.') continue;
					byMarker[marker] = (x, y);
				}
			}

			var result = new List<(int x, int y)>();
			foreach (char marker in orderedMarkers)
			{
				if (byMarker.TryGetValue(marker, out (int x, int y) pos))
				{
					result.Add(pos);
				}
			}

			return [.. result];
		}

		// Interleaves two coordinate lists, placing one from each list in order until both lists are exhausted.
		private static (int x, int y)[] Interleave((int x, int y)[] first, (int x, int y)[] second)
		{
			var result = new List<(int x, int y)>(first.Length + second.Length);
			int max = first.Length > second.Length ? first.Length : second.Length;
			for (int i = 0; i < max; i++)
			{
				if (i < first.Length) result.Add(first[i]);
				if (i < second.Length) result.Add(second[i]);
			}
			return [.. result];
		}

		// Concatenates multiple coordinate lists into a single list.
		private static (int x, int y)[] Concat(params (int x, int y)[][] lists) =>
			[.. lists.SelectMany(x => x)];

		// Counts the number of slots in the map that match the specified symbols.
		private static int CountSlots(string[] map, params char[] symbols)
		{
			int result = 0;
			for (int y = 0; y < map.Length; y++)
				for (int x = 0; x < map[y].Length; x++)
					if (symbols.Contains(map[y][x]))
						result++;
			return result;
		}
	}

	/// <summary>
	/// Default factory creating canonical <see cref="SpaceShipSlotBlueprint"/> instances.
	/// </summary>
	public class SpaceShipSlotBlueprintFactory : ISpaceShipSlotBlueprintFactory
	{
		public ISpaceShipSlotBlueprint Create() => new SpaceShipSlotBlueprint();
	}

	/// <summary>
	/// Singleton provider for the shared <see cref="ISpaceShipSlotBlueprintFactory"/> used across spaceship services.
	/// </summary>
	public static class SpaceShipSlotBlueprintFactoryProvider
	{
		/// <summary>
		/// Canonical spaceship grid width used by blueprint-driven layout and grid allocations.
		/// </summary>
		public const int CanonicalGridWidth = 12;

		/// <summary>
		/// Canonical spaceship grid height used by blueprint-driven layout and grid allocations.
		/// </summary>
		public const int CanonicalGridHeight = 12;

		private static ISpaceShipSlotBlueprintFactory? _instance;

		[SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "This method may perform initialization and is not a simple property getter.")]
		public static ISpaceShipSlotBlueprintFactory GetInstance()
		{
			_instance ??= new SpaceShipSlotBlueprintFactory();
			return _instance;
		}
	}
}
