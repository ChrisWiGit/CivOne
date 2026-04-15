using System.Collections.Generic;
using CivOne.Persistence.Model.Attributes;

namespace CivOne.Persistence.Model
{
    /// <summary>
    /// DTO for a single replay event. Exactly one of the event-data properties must be
    /// non-null; the non-null property implicitly identifies the event type.
    /// </summary>
    public class ReplayDataDto
    {
        [Doc("Game turn on which the event occurred.", 0, 65535)]
        public int Turn { get; set; }

        // ── Exactly one of the following must be non-null ─────────────────────────

        [Doc("A city was founded by a civilization.")]
        public CityBuiltData CityBuilt { get; set; }

        [Doc("A city was destroyed.")]
        public CityDestroyedData CityDestroyed { get; set; }

        [Doc("A city was captured by a civilization.")]
        public CityCapturedData CityCaptured { get; set; }

        [Doc("A civilization was destroyed by another.")]
        public CivilizationDestroyedData CivilizationDestroyed { get; set; }

        [Doc("War was declared between two civilizations.")]
        public TwoCivsData WarDeclared { get; set; }

        [Doc("Peace was made between two civilizations.")]
        public TwoCivsData PeaceMade { get; set; }

        [Doc("A technology advance was discovered by a civilization.")]
        public CivWithTypeIdData AdvanceDiscovered { get; set; }

        [Doc("A unit type was built for the first time by a civilization.")]
        public CivWithTypeIdData UnitFirstBuilt { get; set; }

        [Doc("A civilization changed its government.")]
        public CivWithTypeIdData GovernmentChanged { get; set; }

        [Doc("A wonder was built by a civilization.")]
        public CivWithTypeIdData WonderBuilt { get; set; }

        [Doc("A per-turn replay summary (city count and population).")]
        public ReplaySummaryData ReplaySummary { get; set; }

        [Doc("Civilization rankings at the end of a turn.")]
        public CivRankingsData CivRankings { get; set; }

        // ── Nested data classes ───────────────────────────────────────────────────

        public class CityBuiltData
        {
            [Doc("ID of the civilization that founded the city.", 0, 7)]
            public int OwnerId { get; set; }
            [Doc("Internal city ID.")]
            public int CityId { get; set; }
            [Doc("Index into the global city name list.")]
            public int CityNameId { get; set; }
            [Doc("Map X coordinate.", 0, 79)]
            public int X { get; set; }
            [Doc("Map Y coordinate.", 0, 49)]
            public int Y { get; set; }
        }

        public class CityDestroyedData
        {
            [Doc("Internal city ID.")]
            public int CityId { get; set; }
            [Doc("Index into the global city name list.")]
            public int CityNameId { get; set; }
            [Doc("Map X coordinate.", 0, 79)]
            public int X { get; set; }
            [Doc("Map Y coordinate.", 0, 49)]
            public int Y { get; set; }
        }

        public class CityCapturedData
        {
            [Doc("ID of the civilization that captured the city.", 0, 7)]
            public int CivId { get; set; }
            [Doc("Index into the global city name list.")]
            public int CityNameId { get; set; }
            [Doc("Map X coordinate.", 0, 79)]
            public int X { get; set; }
            [Doc("Map Y coordinate.", 0, 49)]
            public int Y { get; set; }
        }

        public class CivilizationDestroyedData
        {
            [Doc("ID of the civilization that was destroyed.", 0, 7)]
            public int DestroyedId { get; set; }
            [Doc("ID of the civilization that caused the destruction.", 0, 7)]
            public int DestroyedById { get; set; }
        }

        /// <summary>Shared data for two-civilization events (war, peace).</summary>
        public class TwoCivsData
        {
            [Doc("ID of the initiating civilization.", 0, 7)]
            public int CivId { get; set; }
            [Doc("ID of the other civilization involved.", 0, 7)]
            public int CivId2 { get; set; }
        }

        /// <summary>Shared data for civ + type-id events (advance, unit, government, wonder).</summary>
        public class CivWithTypeIdData
        {
            [Doc("ID of the civilization.", 0, 7)]
            public int CivId { get; set; }
            [Doc("ID of the advance / unit type / government type / wonder.")]
            public int TypeId { get; set; }
        }

        public class ReplaySummaryData
        {
            [Doc("Total number of cities on the map.", 0, 255)]
            public int CityCount { get; set; }
            [Doc("Total world population.", 0, int.MaxValue)]
            public int Population { get; set; }
        }

        public class CivRankingsData
        {
            [Doc("Ordered list of civilization IDs by rank (index 0 = rank 1).")]
            public List<int> Rankings { get; set; }
        }
    }
}
