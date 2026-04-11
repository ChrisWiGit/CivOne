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
    public class PlayerDto
    {
		[Doc("The civilization of the player.")]
        public CivilizationDto Civilization { get; set; }

		[Doc("The unique id of the player. This may be the index of a player list, but it is not guaranteed to be stable across game sessions.")]
        public PlayerId Id { get; set; }
        
		[Doc("A list of explored advances", null, nameof(AllAdvancesInfo))]
		public List<AdvanceId> Advances { get; set; }
        
		[Doc("A list of player ids with which this player has an embassy.")]
		public List<PlayerId> Embassies { get; set; }

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

		[Doc("The player's initial X-position used as world map focus anchor.")]
		public short StartX { get; set; }

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
    }
}