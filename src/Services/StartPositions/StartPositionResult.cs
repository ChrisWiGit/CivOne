using CivOne.Civilizations;
using CivOne.Persistence.Model;

namespace CivOne.Services.StartPositions
{
	/// <summary>
	/// The outcome of trying to find a starting position for one <see cref="StartPositionCandidate"/>.
	/// </summary>
	public sealed class StartPositionResult
	{
		/// <summary>
		/// The civilization this result belongs to. Matches the corresponding <see cref="StartPositionCandidate.Civilization"/>.
		/// </summary>
		public required ICivilization Civilization { get; init; }

		/// <summary>
		/// Whether a valid starting position was found. When false, <see cref="Position"/> is meaningless.
		/// </summary>
		public bool Success { get; init; }

		/// <summary>
		/// The tile where the Settlers unit should be placed.
		/// </summary>
		public MapLocation Position { get; init; } = new();
	}
}