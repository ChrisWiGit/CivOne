using CivOne.Screens;

namespace CivOne.Screens.Dialogs
{
	/// <summary>
	/// The "Do you want to keep playing?" question after the final score was entered into the Hall of Fame.
	/// </summary>
	internal interface IKeepPlayingDialog : IScreen
	{
		/// <summary>
		/// Gets whether the player chose "Yes, keep playing". Read it after the dialog was closed.
		/// </summary>
		bool KeepPlaying { get; }
	}
}
