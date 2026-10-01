using CivOne.Civilizations;
using CivOne.Persistence.Model;

namespace CivOne.Services.StartPositions
{
	/// <summary>
	/// Describes one civilization that needs a starting position.
	/// </summary>
	public sealed class StartPositionCandidate
	{
		/// <summary>
		/// The civilization to find a starting position for.
		/// </summary>
		public required ICivilization Civilization { get; init; }

		/// <summary>
		/// A custom starting position set for this civilization on the map (e.g. painted in the terrain editor), if any.
		/// When set and the request is for the first game turn, this position is used instead of a computed one.
		/// </summary>
		public MapLocation? MapStartPosition { get; init; }
	}
}