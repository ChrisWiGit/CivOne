namespace CivOne.Persistence.Model
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using CivOne.Enums;
	using CivOne.Persistence.Model.Attributes;
	using CityId = System.UInt32;
    using PlayerId = System.Byte;

    public enum CityStatus
    {
        // RIOT = 1 << 0,
        // COASTAL = 1 << 1,
        // CELEBRATION_CANCELLED = 1 << 2,
        // HYDRO_AVAILABLE = 1 << 3,
        // AUTO_BUILD = 1 << 4,
        // TECH_STOLEN = 1 << 5,
        // CELEBRATION_RAPTURE = 1 << 6,
        // IMPROVEMENT_SOLD = 1 << 7
        Riot,
        Coastal,
        CelebrationCancelled,
        HydroAvailable,
        AutoBuild,
        TechStolen,
        CelebrationRapture, 
        ImprovementSold
    }

    [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "These collections need to be settable for deserialization and mapping.")]
    [SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists", Justification = "The collections need to be List<T> for deserialization and mapping.")]
    public class CityDto
    {
        [Doc("Unique identifier for the city. Used for YAML references.")]
        public Guid Id { get; set; }

        [Doc("City location on the map.")]
        public MapLocation Location { get; set; } = new();

        [Doc("Identifier of the player who owns the city starting from 0")]
        public PlayerId Owner { get; set; }

        [Doc("Name of the city.")]
        public string Name { get; set; } = string.Empty;

        [Doc("Size of the city.")]
        public uint Size { get; set; }

        [Doc("Food storage of the city. Determines how close the city is to growing.", 0, 65535)]
        public int Food { get; set; }

        [Doc("Shield storage of the city. Determines how close the current production is to completion.", 0, 65535)]
        public int Shields { get; set; }

        [Doc("Visible sizes of the city for other players. Index corresponds to player ID.")]
        public uint[] VisibleSizes { get; set; } =  [];

        [Doc("Current production of the city.")]
        public ProductionDto? CurrentProduction { get; set; }

        /// <summary>
        /// 5x5 bitmask of active resource tiles relative to the city center.
        /// Index [dx+2, dy+2] corresponds to offset (dx, dy) from city position.
        /// The center tile [2,2] is always implicitly used and not stored here.
        /// </summary>
        [Doc("5x5 bitmask of active resource tiles relative to the city center. Center will be ignored.")]
        public Bool2dMap ResourceTiles { get; set; } = new();

        [Doc("Specialists in the city.", nameof(AllSpecialists))]
        public List<Citizen> Specialists { get; set; } = new();
        public static readonly Citizen[] AllSpecialists = [Citizen.Entertainer, Citizen.Scientist, Citizen.Taxman];

        [Doc("Buildings present in the city.", nameof(AllBuildings))]
        public List<Building> Buildings { get; set; } = [];
        public static readonly Building[] AllBuildings = Enum.GetValues<Building>();

        [Doc("Wonders present in the city.", nameof(AllWonders))]
        public List<Wonder> Wonders { get; set; } = [];
        public static readonly Wonder[] AllWonders = Enum.GetValues<Wonder>();

        [Doc("Status flags of the city.", nameof(CityStatusEnumAll))]
        public List<CityStatus> Status { get; set; } = [];
        public static readonly CityStatus[] CityStatusEnumAll = Enum.GetValues<CityStatus>();

        [Doc("Indicates if the city was in disorder.")]
        public bool WasInDisorder { get; set; }

        [Doc("IDs of cities that this city is trading with. Current maximum is 3.")]
        public Guid[] TradingCities { get; set; } = [];

        [Doc("Continent ID the city is located on.")]
        public int ContinentId { get; set; }
    }
}