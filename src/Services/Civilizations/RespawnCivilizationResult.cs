using CivOne.Civilizations;

namespace CivOne.Services.Civilizations
{
	/// <summary>
	/// The civilization a respawning player slot is given.
	/// </summary>
	internal sealed class RespawnCivilizationResult
	{
		/// <summary>
		/// The civilization for the new player.
		/// </summary>
		public required ICivilization Civilization { get; init; }

		/// <summary>
		/// How many living players already use <see cref="Civilization"/>.
		/// 0 means it is free; higher values are passed to <see cref="CivilizationNameDelegate"/> so the new
		/// player gets a distinguishable name.
		/// </summary>
		public int Occurrence { get; init; }
	}
}
