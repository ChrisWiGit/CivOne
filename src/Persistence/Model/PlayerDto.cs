using System;
using System.Collections.Generic;
using CivOne.Advances;
using CivOne.Civilizations;
using CivOne.Governments;
using CivOne.Persistence.Model.Attributes;

namespace CivOne.Persistence.Model
{
    using PlayerId = System.UInt16;
    using AdvanceId = System.UInt32;
    using GovernmentId = System.Byte;
	using CityId = System.UInt16;

	#pragma warning disable CA2227, CA1002 // Since this is a DTO we need setters and mutable collections.
    public class PlayerDto
    {
		[Doc("The civilization of the player.")]
        public CivilizationDto Civilization { get; set; }

		[Doc("The unique id of the player. This may be the index of a player list, but it is not guaranteed to be stable across game sessions.")]
        public PlayerId Id { get; set; }

		[Doc("Stable player identity independent from list/index positions.")]
		public Guid PlayerGuid { get; set; }
        
		[Doc("A list of explored advances. Use -1 to indicate all advances.", null, nameof(AllAdvancesInfo))]
		// We use long to allow YAML to read/write the values without overflow issues, but the mapper will clamp to the valid range of advances.
		public List<long> Advances { get; set; }
        
		[Doc("A list of player ids with which this player has an embassy.")]
		public List<PlayerId> Embassies { get; set; }

		[Doc("Per-target diplomacy state entries. RawFlags are persisted 1:1 from legacy diplomacy bitmasks.")]
		public List<DiplomacyEntryDto> Diplomacy { get; set; }

		public static Dictionary<AdvanceId, string> AllAdvancesInfo = [];

        [Doc("The number of turns the player is in anarchy.")]
        public short Anarchy { get; set; }

		[Doc("The amount of gold the player has. Must be non-negative and not exceed 30000. If the value is out of range, it will be set to the closest valid value.", 0, 30000)]
        public long Gold { get; set; }

		[Doc("The id of the advance currently being researched, or null if no research is in progress.", nameof(AllAdvances))]
        public AdvanceId CurrentResearch { get; set; }

		// Must be initialized outside of this class, so it is not coupled to the actual advances in the game. 
		public static string[] AllAdvances = [];

        [Doc("The current index of the city name list for this player, and to be shown next time a city name is needed for a new city.")]
        public int CityNamesSkipped { get; set; }

		[Doc("The number of future technologies this player has completed.", 0, ushort.MaxValue)]
		public ushort FutureTechCount { get; set; }

		[Doc("Turns since last major contact state with the human player in legacy save semantics.", 0, ushort.MaxValue)]
		public ushort HumanContactTurn { get; set; }

		[Doc("The player's initial X-position used as world map focus anchor.")]
		public short StartX { get; set; }

		[Doc("Saved map camera slots (1-9 in UI, 0-8 in storage). Use -1/-1 for empty slots. Can be null.")]
		public List<MapPositionDto>? MapPositions { get; set; }

		[Doc("Last opened map camera position for the human player. Null means no remembered position for backward compatibility.")]
		public MapPositionDto? LastMapPosition { get; set; }

		[Doc("Units lost per unit type (28 entries). YAML allows long values; mapper clamps to ushort range.")]
		public List<long> UnitsLost { get; set; }

		[Doc("Units destroyed by this player indexed by current player list order. YAML allows long values; mapper clamps to ushort range.")]
		public List<long> UnitsDestroyedBy { get; set; }

		[Doc("Units destroyed by this player keyed by target PlayerGuid. Preferred over index-based UnitsDestroyedBy for durable cross-references.")]
		public Dictionary<Guid, long> UnitsDestroyedByByPlayerGuid { get; set; }

		[Doc("Legacy epic ranking value for this player. YAML allows long values; mapper clamps to ushort range.")]
		public long EpicRanking { get; set; }

		[Doc("Legacy military power value for this player. YAML allows long values; mapper clamps to ushort range.")]
		public long MilitaryPower { get; set; }

		[Doc("Legacy civilization score value for this player. YAML allows long values; mapper clamps to ushort range.")]
		public long CivilizationScore { get; set; }

		[Doc("A list of the player's cities")]
        public List<CityDto> Cities { get; set; }

		[Doc("A list of the player's units")]
        public List<UnitDto> Units { get; set; }

		[Doc("A 2D array indicating which tiles have been explored by the player (1) or not (0). This only accounts for the city tiles to be shown (4x4). Center tile is always 1.", 0,4)]
        public Bool2dMap Explored { get; set; }

        [Doc("A 2D array indicating which tiles are visible to the player (1) or not (0). This only accounts for the city tiles to be shown (4x4). Center tile is always 1.", 0,4)]
        public Bool2dMap Visible { get; set; }

		[Doc("The name of the player's tribe, e.g. 'Romans'.")]
        public string TribeName { get; set; }
        
		[Doc("The plural form of the player's tribe name, e.g. 'Romans'.")]
        public string TribeNamePlural { get; set; }

		[Doc("The id of the government type of the player.", nameof(AllGovernments))]
        public GovernmentId Government { get; set; }

		public static string[] AllGovernments = [];

		[Doc("The percentage of luxuries rate, from 0 to 10, where 0 means 0% and 10 means 100%." +
			"Must be in increments of 10%. If the value is out of range, it will be set to the closest valid value."+
			"The sum of all rates (luxuries, taxes, science) must be 10. If setting this property causes the sum to exceed 10, the result is undefined"
			, 0,10)]
        public int LuxuriesRate { get; set; }
        [Doc("The percentage of taxes rate, from 0 to 10, where 0 means 0% and 10 means 100%." +
			"Must be in increments of 10%. If the value is out of range, it will be set to the closest valid value." +
			"The sum of all rates (luxuries, taxes, science) must be 10. If setting this property causes the sum to exceed 10, the result is undefined"
			, 0,10)]
        public int TaxesRate { get; set; }
        [Doc("The percentage of science rate, from 0 to 10, where 0 means 0% and 10 means 100%." +
			"Must be in increments of 10%. If the value is out of range, it will be set to the closest valid value." +
			"The sum of all rates (luxuries, taxes, science) must be 10. If setting this property causes the sum to exceed 10, the result is undefined"
			, 0,10)]
        public int ScienceRate { get; set; }

		[Doc("The total amount of science points the player has.", 0,30000)]
        public int Science { get; set; }

		[Doc("The player's palace. This may be null if the player has no palace.")]
        public PalaceDto Palace { get; set; }

		[Doc("The player's spaceship state. Null if no Apollo Program or not started.")]
		public SpaceShipDto SpaceShip { get; set; }
    }
}