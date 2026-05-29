using System.Linq;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Screens.GamePlayPanels;
using Xunit;

namespace CivOne.UnitTests
{
	/// <summary>
	/// Verifies map-view mode keyboard behavior and map position slot hotkeys.
	/// </summary>
	public class GameMapViewModeTests : TestsBase2
	{
		/// <summary>
		/// Ensures arrow keys pan the camera in map-view mode without moving the active unit.
		/// </summary>
		[Fact]
		public void MapViewModeArrowKeysPanMapWithoutMovingUnit()
		{
			var activeUnit = Game.Instance.GetUnits().First(x => playa == x.Owner);
			Game.Instance._currentPlayer = Game.Instance.PlayerNumber(playa);
			Game.Instance.ActiveUnit = activeUnit;

			using var gameMap = new GameMapForTesting();
			gameMap.CenterOnPoint(20, 20);
			var originalMapX = gameMap.X;
			var originalMapY = gameMap.Y;
			var originalUnitX = activeUnit.X;
			var originalUnitY = activeUnit.Y;

			gameMap.ToggleMapView();
			var handled = gameMap.KeyDown(new KeyboardEventArgs(Key.Right));

			Assert.True(handled);
			Assert.NotEqual(originalMapX, gameMap.X);
			Assert.Equal(originalMapY, gameMap.Y);
			Assert.Equal(originalUnitX, activeUnit.X);
			Assert.Equal(originalUnitY, activeUnit.Y);
		}

		/// <summary>
		/// Ensures map camera slots can be saved with Ctrl+1 and restored with Alt+1.
		/// </summary>
		[Fact]
		public void MapPositionSlotCtrlSavesAndAltRestoresCamera()
		{
			Game.Instance._currentPlayer = Game.Instance.PlayerNumber(playa);
			using var gameMap = new GameMapForTesting();

			gameMap.CenterOnPoint(22, 18);
			var savedX = gameMap.X;
			var savedY = gameMap.Y;

			var saveHandled = gameMap.KeyDown(new KeyboardEventArgs('1', KeyModifier.Control));
			Assert.True(saveHandled);

			gameMap.CenterOnPoint(70, 40);
			Assert.NotEqual(savedX, gameMap.X);

			var restoreHandled = gameMap.KeyDown(new KeyboardEventArgs('1', KeyModifier.Alt));
			Assert.True(restoreHandled);
			Assert.Equal(savedX, gameMap.X);
			Assert.Equal(savedY, gameMap.Y);
		}

		/// <summary>
		/// Ensures saving a camera slot raises a one-based map position saved event.
		/// </summary>
		[Fact]
		public void MapPositionSlotCtrlSaveRaisesMapPositionSavedEvent()
		{
			Game.Instance._currentPlayer = Game.Instance.PlayerNumber(playa);
			using var gameMap = new GameMapForTesting();
			int actualSlot = 0;
			gameMap.MapPositionSaved += (_, slot) => actualSlot = slot;

			var handled = gameMap.KeyDown(new KeyboardEventArgs('1', KeyModifier.Control));

			Assert.True(handled);
			Assert.Equal(1, actualSlot);
		}

		/// <summary>
		/// Ensures saving an unnamed slot does not open the rename dialog.
		/// </summary>
		[Fact]
		public void MapPositionSlotCtrlSaveWithoutNameDoesNotOpenRenameDialog()
		{
			Game.Instance._currentPlayer = Game.Instance.PlayerNumber(playa);
			using var gameMap = new GameMapForTesting();

			var handled = gameMap.KeyDown(new KeyboardEventArgs('1', KeyModifier.Control));

			Assert.True(handled);
			Assert.False(gameMap.HasMapPositionRenameDialog);
		}

		/// <summary>
		/// Ensures saving a named slot opens the rename dialog with the current name.
		/// </summary>
		[Fact]
		public void MapPositionSlotCtrlSaveWithNameOpensRenameDialog()
		{
			Game.Instance._currentPlayer = Game.Instance.PlayerNumber(playa);
			using var gameMap = new GameMapForTesting();
			Game.Instance.HumanPlayer.MapPositionNames[0] = "Capital";

			var handled = gameMap.KeyDown(new KeyboardEventArgs('1', KeyModifier.Control));

			Assert.True(handled);
			Assert.True(gameMap.HasMapPositionRenameDialog);
			Assert.Equal("Keep name or change it?", gameMap.MapPositionRenameDialogTitle);
			Assert.Equal("Capital", gameMap.MapPositionRenameDialogText);
		}

		/// <summary>
		/// Ensures clearing a rename dialog stores an empty name again.
		/// </summary>
		[Fact]
		public void MapPositionSlotClearingRenameDialogRestoresUnnamedState()
		{
			Game.Instance._currentPlayer = Game.Instance.PlayerNumber(playa);
			using var gameMap = new GameMapForTesting();
			Game.Instance.HumanPlayer.MapPositionNames[0] = "Capital";

			Assert.True(gameMap.KeyDown(new KeyboardEventArgs('1', KeyModifier.Control)));
			Assert.True(gameMap.HasMapPositionRenameDialog);

			while (gameMap.MapPositionRenameDialogText.Length > 0)
			{
				Assert.True(gameMap.KeyDown(new KeyboardEventArgs(Key.Backspace)));
			}

			Assert.True(gameMap.KeyDown(new KeyboardEventArgs(Key.Enter)));
			Assert.False(gameMap.HasMapPositionRenameDialog);
			Assert.Equal(string.Empty, Game.Instance.HumanPlayer.MapPositionNames[0]);
		}
	}
}
