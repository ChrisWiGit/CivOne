using System.Linq;
using CivOne.Enums;
using CivOne.Events;
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
			Game.Instance.SetCurrentPlayerForTesting(Game.Instance.PlayerNumber(playa));
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
		/// Ensures the space key does not issue "No Orders" while map-view mode is active.
		/// </summary>
		[Fact]
		public void MapViewModeSpaceKeyDoesNotSkipActiveUnitTurn()
		{
			var activeUnit = Game.Instance.GetUnits().First(x => playa == x.Owner);
			Game.Instance.SetCurrentPlayerForTesting(Game.Instance.PlayerNumber(playa));
			Game.Instance.ActiveUnit = activeUnit;

			using var gameMap = new GameMapForTesting();
			gameMap.ToggleMapView();

			bool handled = gameMap.KeyDown(new KeyboardEventArgs(Key.Space));

			Assert.True(handled);
			Assert.Equal(activeUnit, Game.Instance.ActiveUnit);
		}

		/// <summary>
		/// Ensures map camera slots can be saved with Ctrl+1 and restored with Alt+1.
		/// </summary>
		[Fact]
		public void MapPositionSlotCtrlSavesAndAltRestoresCamera()
		{
			Game.Instance.SetCurrentPlayerForTesting(Game.Instance.PlayerNumber(playa));
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
			Game.Instance.SetCurrentPlayerForTesting(Game.Instance.PlayerNumber(playa));
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
			Game.Instance.SetCurrentPlayerForTesting(Game.Instance.PlayerNumber(playa));
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
			Game.Instance.SetCurrentPlayerForTesting(Game.Instance.PlayerNumber(playa));
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
			Game.Instance.SetCurrentPlayerForTesting(Game.Instance.PlayerNumber(playa));
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

		/// <summary>
		/// Ensures Ctrl+mouse wheel down zooms out, updates viewport tile size, and keeps focus near the cursor.
		/// </summary>
		[Fact]
		public void CtrlWheelDownZoomsOutAndKeepsCursorFocus()
		{
			Game.Instance.SetCurrentPlayerForTesting(Game.Instance.PlayerNumber(playa));
			Game.Instance.CurrentPlayer.MapZoomBasisPoints = 1000;

			using var gameMap = new GameMapForTesting();
			gameMap.ResizeMap(160, 120);
			gameMap.SetViewOrigin(10, 10);

			var handled = gameMap.MouseWheel(new ScreenEventArgs(40, 40, MouseButton.None, KeyModifier.Control, -1));

			Assert.True(handled);
			Assert.Equal(900, Game.Instance.CurrentPlayer.MapZoomBasisPoints);
			Assert.Equal(14, gameMap.TilePixelSize);
			Assert.Equal(12, gameMap.VisibleTilesX);
			Assert.Equal(9, gameMap.VisibleTilesY);
			Assert.Equal(10, gameMap.X);
			Assert.Equal(10, gameMap.Y);
		}

		/// <summary>
		/// Ensures Ctrl+mouse wheel up zooms in to the next preset.
		/// </summary>
		[Fact]
		public void CtrlWheelUpZoomsInToNextPreset()
		{
			Game.Instance.SetCurrentPlayerForTesting(Game.Instance.PlayerNumber(playa));
			Game.Instance.CurrentPlayer.MapZoomBasisPoints = 750;

			using var gameMap = new GameMapForTesting();
			gameMap.ResizeMap(160, 120);

			var handled = gameMap.MouseWheel(new ScreenEventArgs(40, 40, MouseButton.None, KeyModifier.Control, 1));

			Assert.True(handled);
			Assert.Equal(900, Game.Instance.CurrentPlayer.MapZoomBasisPoints);
			Assert.Equal(14, gameMap.TilePixelSize);
			Assert.Equal(12, gameMap.VisibleTilesX);
		}

		/// <summary>
		/// Ensures Ctrl+PageDown zooms out to the next preset.
		/// </summary>
		[Fact]
		public void CtrlPageDownZoomsOutToNextPreset()
		{
			Game.Instance.SetCurrentPlayerForTesting(Game.Instance.PlayerNumber(playa));
			Game.Instance.CurrentPlayer.MapZoomBasisPoints = 1000;

			using var gameMap = new GameMapForTesting();
			gameMap.ResizeMap(160, 120);

			var handled = gameMap.KeyDown(new KeyboardEventArgs(Key.PageDown, KeyModifier.Control));

			Assert.True(handled);
			Assert.Equal(900, Game.Instance.CurrentPlayer.MapZoomBasisPoints);
			Assert.Equal(14, gameMap.TilePixelSize);
		}

		/// <summary>
		/// Ensures Ctrl+PageUp zooms in to the next preset.
		/// </summary>
		[Fact]
		public void CtrlPageUpZoomsInToNextPreset()
		{
			Game.Instance.SetCurrentPlayerForTesting(Game.Instance.PlayerNumber(playa));
			Game.Instance.CurrentPlayer.MapZoomBasisPoints = 750;

			using var gameMap = new GameMapForTesting();
			gameMap.ResizeMap(160, 120);

			var handled = gameMap.KeyDown(new KeyboardEventArgs(Key.PageUp, KeyModifier.Control));

			Assert.True(handled);
			Assert.Equal(900, Game.Instance.CurrentPlayer.MapZoomBasisPoints);
			Assert.Equal(14, gameMap.TilePixelSize);
		}

		/// <summary>
		/// Ensures wheel input without Ctrl remains ignored by the map screen.
		/// </summary>
		[Fact]
		public void WheelWithoutCtrlIsIgnored()
		{
			Game.Instance.SetCurrentPlayerForTesting(Game.Instance.PlayerNumber(playa));
			Game.Instance.CurrentPlayer.MapZoomBasisPoints = 1000;

			using var gameMap = new GameMapForTesting();
			gameMap.ResizeMap(160, 120);

			var handled = gameMap.MouseWheel(new ScreenEventArgs(40, 40, MouseButton.None, KeyModifier.None, -1));

			Assert.False(handled);
			Assert.Equal(1000, Game.Instance.CurrentPlayer.MapZoomBasisPoints);
			Assert.Equal(16, gameMap.TilePixelSize);
		}

		/// <summary>
		/// Ensures PageUp/PageDown without Ctrl keep their existing non-zoom behavior.
		/// </summary>
		[Fact]
		public void PageKeysWithoutCtrlDoNotTriggerZoom()
		{
			Game.Instance.SetCurrentPlayerForTesting(Game.Instance.PlayerNumber(playa));
			Game.Instance.CurrentPlayer.MapZoomBasisPoints = 1000;

			using var gameMap = new GameMapForTesting();
			gameMap.ResizeMap(160, 120);

			var pageDownHandled = gameMap.KeyDown(new KeyboardEventArgs(Key.PageDown));
			var pageUpHandled = gameMap.KeyDown(new KeyboardEventArgs(Key.PageUp));

			Assert.False(pageDownHandled);
			Assert.False(pageUpHandled);
			Assert.Equal(1000, Game.Instance.CurrentPlayer.MapZoomBasisPoints);
			Assert.Equal(16, gameMap.TilePixelSize);
		}

		/// <summary>
		/// Ensures zooming out on an expanded logical canvas increases the visible tile span.
		/// </summary>
		[Fact]
		public void CtrlWheelDownOnExpandedCanvasIncreasesVisibleTileSpan()
		{
			Game.Instance.SetCurrentPlayerForTesting(Game.Instance.PlayerNumber(playa));
			Game.Instance.CurrentPlayer.MapZoomBasisPoints = 1000;

			using var gameMap = new GameMapForTesting();
			gameMap.ResizeMap(320, 200);
			var visibleTilesXBefore = gameMap.VisibleTilesX;
			var visibleTilesYBefore = gameMap.VisibleTilesY;

			var handled = gameMap.MouseWheel(new ScreenEventArgs(160, 100, MouseButton.None, KeyModifier.Control, -1));

			Assert.True(handled);
			Assert.Equal(900, Game.Instance.CurrentPlayer.MapZoomBasisPoints);
			Assert.True(gameMap.VisibleTilesX > visibleTilesXBefore);
			Assert.True(gameMap.VisibleTilesY > visibleTilesYBefore);
		}

		/// <summary>
		/// Ensures zoom-out focus near the lower edge keeps the viewport inside Y bounds.
		/// </summary>
		[Fact]
		public void CtrlWheelDownNearBottomEdgeKeepsViewportInYBounds()
		{
			Game.Instance.SetCurrentPlayerForTesting(Game.Instance.PlayerNumber(playa));
			Game.Instance.CurrentPlayer.MapZoomBasisPoints = 1000;

			using var gameMap = new GameMapForTesting();
			gameMap.ResizeMap(160, 120);
			gameMap.SetViewOrigin(10, Map.HEIGHT - gameMap.VisibleTilesY);

			var handled = gameMap.MouseWheel(new ScreenEventArgs(80, 118, MouseButton.None, KeyModifier.Control, -1));

			Assert.True(handled);
			Assert.True(gameMap.Y >= 0);
			Assert.True(gameMap.Y <= Map.HEIGHT - gameMap.VisibleTilesY);
		}
	}
}
