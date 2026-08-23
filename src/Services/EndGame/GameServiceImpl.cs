// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

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
