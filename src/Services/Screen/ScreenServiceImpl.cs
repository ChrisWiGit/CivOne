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
	/// Default implementation of screen query and command services using Common static methods.
	/// </summary>
	/// <remarks>
	/// Delegates to Common.Screens, Common.LastScreen, Common.TopScreen, Common.AddScreen, and Common.DestroyScreen.
	/// </remarks>
	internal class ScreenServiceImpl : IScreenQueryService, IScreenCommandService
	{
		/// <inheritdoc/>
		public IScreen[] Screens => Common.Screens;

		/// <inheritdoc/>
		public IScreen? LastScreen => Common.LastScreen;

		/// <inheritdoc/>
		public IScreen? TopScreen => Common.TopScreen;

		/// <inheritdoc/>
		public void AddScreen(IScreen screen) => Common.AddScreen(screen);

		/// <inheritdoc/>
		public void DestroyScreen(IScreen screen) => Common.DestroyScreen(screen);

		public bool HasScreenType<T>() where T : IScreen
		{
			return Common.HasScreenType<T>();
		}

		
		public bool HasTopScreen<T>() where T : IScreen
		{
			return HasScreenType<T>() && TopScreen != null && TopScreen.GetType() == typeof(T);
		}

		public bool HasLastScreen<T>() where T : IScreen
		{
			return HasScreenType<T>() && LastScreen != null && LastScreen.GetType() == typeof(T);
		}
	}
}
