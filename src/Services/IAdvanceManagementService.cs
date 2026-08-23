using CivOne.Advances;

namespace CivOne.Services
{
	/// <summary>
	/// Service for managing civilizations' technological advances.
	/// Handles querying, toggling, and organizing advances alphabetically.
	/// </summary>
	public interface IAdvanceManagementService
	{
		/// <summary>
		/// Gets all advances sorted alphabetically by name.
		/// </summary>
		IAdvance[] GetAllAdvances();

		/// <summary>
		/// Checks if a civilization has a specific advance.
		/// </summary>
		/// <param name="civNumber">Civilization player number.</param>
		/// <param name="advance">The advance to check.</param>
		/// <returns>True if the civilization has the advance; otherwise false.</returns>
		bool HasAdvance(byte civNumber, IAdvance advance);

		/// <summary>
		/// Toggles an advance for a civilization (adds if missing, removes if present).
		/// </summary>
		/// <param name="civNumber">Civilization player number.</param>
		/// <param name="advance">The advance to toggle.</param>
		void ToggleAdvance(byte civNumber, IAdvance advance);
	}
}
