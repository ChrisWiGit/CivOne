namespace CivOne.Services.EndGame
{
	/// <summary>
	/// Default implementation of game service using static game state methods.
	/// </summary>
	/// <remarks>
	/// Delegates to Game.Wipe() and Map.Reset().
	/// </remarks>
	internal class GameServiceImpl : IGameService
	{
		/// <inheritdoc/>
		public void End()
		{
			Game.Wipe();
			Map.Reset();
		}
	}
}