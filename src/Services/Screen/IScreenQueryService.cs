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
	/// Provides read access to the current screen stack.
	/// </summary>
	internal interface IScreenQueryService
	{
		/// <summary>
		/// Returns a snapshot of all currently active screens in stack order (bottom to top).
		/// </summary>
		IScreen[] Screens { get; }

		/// <summary>
		/// Returns the screen at the bottom of the stack, or <c>null</c> if the stack is empty.
		/// </summary>
		IScreen LastScreen { get; }

		/// <summary>
		/// Returns the topmost active screen, favouring modal screens if any are present.
		/// Returns <c>null</c> if the stack is empty.
		/// </summary>
		IScreen TopScreen { get; }
	}
}
