using CivOne.Services.SpaceShip;
using CivOne.Tasks;

namespace CivOne
{
	/// <summary>
	/// Loss of a civilization's spaceship program.
	/// </summary>
	public partial class Game
	{
		/// <summary>
		/// Loses the spaceship program of <paramref name="player"/>, like the original game when its capital is captured
		/// or the civilization is destroyed. The human player is told about it, whoever lost the ship.
		/// </summary>
		/// <param name="player">The player who loses the program.</param>
		internal void LoseSpaceShip(Player player)
		{
			SpaceShipLoss loss = SpaceShipArrivalService.LoseShip(player);
			string[]? message = loss switch
			{
				SpaceShipLoss.ReturnsToEarth => TranslateFormattedArray("{0} spaceship\nreturns to Earth.", player.TribeName),
				SpaceShipLoss.ConstructionCancelled => TranslateFormattedArray("{0} spaceship\nconstruction cancelled.", player.TribeName),
				_ => null,
			};

			if (message != null)
			{
				GameTask.Insert(Message.General(message));
			}
		}
	}
}
