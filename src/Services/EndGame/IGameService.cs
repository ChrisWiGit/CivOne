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
