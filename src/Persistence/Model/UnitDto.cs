using System;
using CivOne.Enums;
using CivOne.Persistence.Model.Attributes;

namespace CivOne.Persistence.Model
{
    public class UnitDto
    {
        [Doc("ClassName of the Unit.", nameof(AllUnitsClassNames))]
        public string ClassName { get; set; }
        // This property is used by DocAttribute to provide list of all available Unit class names. Must be initialized outside.
        public static string[] AllUnitsClassNames { get; set; } = [];
        
        [Doc("Location of the Unit on the Map. X and Y coordinates must be non-negative and in range of the map, otherwise the behavior is undefined.")]
        public MapLocation Location { get; set; }
        [Doc("Goto location of the Unit on the Map. X and Y coordinates must be non-negative and in range of the map, otherwise the behavior is undefined.")]
        public MapLocation Goto { get; set; }

        [Doc("Home city of the Unit. It is optional and can be null. If not null, it must be a valid City Guid. E.g. {00000000-0000-0000-0000-000000000001}")]
        public Guid? HomeCityGuid { get; set; }

        public bool Busy { get; set; }
        public bool HasAction { get; set; }
        public bool HasMovesLeft { get; set; }
        public bool Veteran { get; set; }
        public bool Sentry { get; set; }
        public bool FortifyActive { get; set; }
        public bool Fortify { get; set; }

        public byte FuelOrProgress { get; set; }
        public byte Fuel { get; set; }
        public byte WorkProgress { get; set; }

        public Order Order { get; set; }

        public int MovesSkip { get; set; }
        public byte MovesLeft { get; set; }
        public byte PartMoves { get; set; }

        // Owner wil be set from Player
        public byte PlayerId { get; set; }
    }
}