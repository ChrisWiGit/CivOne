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