namespace CivOne.Services.EndGame
{
	/// <summary>
	/// Provides game state management operations.
	/// </summary>
	internal interface IGameService
	{
		/// <summary>
		/// Clears and resets the game state back to a clean slate.
		/// Wipes the game instance and resets the map.
		/// </summary>
		void End();
	}
}