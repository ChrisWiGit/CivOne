using CivOne.Enums;

namespace CivOne.Persistence.Model
{
    public class UnitDto
    {
        public string ClassName { get; set; }
        public MapLocation Location { get; set; }
        public MapLocation Goto { get; set; }

        public string HomeCity { get; set; }

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