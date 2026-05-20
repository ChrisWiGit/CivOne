// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using CivOne.Screens;

namespace CivOne.Services.Screen
{
	/// <summary>
	/// Provides screen management commands: adding and removing screens from the screen stack.
	/// </summary>
	internal interface IScreenCommandService
	{
		/// <summary>
		/// Adds a screen to the top of the screen stack and makes it active.
		/// </summary>
		/// <param name="screen">The screen to add.</param>
		void AddScreen(IScreen screen);

		/// <summary>
		/// Removes a screen from the screen stack and disposes it.
		/// </summary>
		/// <param name="screen">The screen to remove and dispose.</param>
		void DestroyScreen(IScreen screen);
	}
}
