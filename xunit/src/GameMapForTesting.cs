using CivOne.Screens.GamePlayPanels;

namespace CivOne.UnitTests
{
	/// <summary>
	/// Test subclass of GameMap that exposes protected delegate members for unit testing.
	/// </summary>
	internal class GameMapForTesting : GameMap
	{
		/// <summary>
		/// Gets whether the rename dialog is currently active.
		/// </summary>
		internal bool HasMapPositionRenameDialog => _mapPositionDelegate.HasRenameDialog;

		/// <summary>
		/// Gets the current text in the rename dialog.
		/// </summary>
		internal string MapPositionRenameDialogText => _mapPositionDelegate.RenameDialogText;

		/// <summary>
		/// Gets the title message shown in the rename dialog.
		/// </summary>
		internal string MapPositionRenameDialogTitle => _mapPositionDelegate.RenameDialogTitle;
	}
}
