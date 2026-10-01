namespace CivOne.Screens.GamePlayPanels
{
	internal partial class GameMap
	{
		/// <summary>
		/// Handles terrain editor enable/disable session transitions.
		/// </summary>
		private sealed class GameTerrainEditorSessionDelegate(GameMap gameMap)
		{
			private readonly GameMap _gameMap = gameMap;

			public void SetEnabled(bool enabled)
			{
				if (_gameMap._editorState.Enabled == enabled)
					return;

				_gameMap._editorState.Enabled = enabled;

				if (enabled)
				{
					EnableEditor();
				}
				else
				{
					DisableEditor();
				}

				_gameMap._update = true;
				_gameMap._fullRedraw = true;
			}

			private void EnableEditor()
			{
				_gameMap._editorStoredUnit = Game.ActiveUnit;
				Game.ActiveUnit = null;

				_gameMap._editorState.CityOwner = Game.PlayerNumber(Human);

				if (_gameMap._editorStoredUnit == null)
					return;

				_gameMap._hoveredTileX = _gameMap._editorStoredUnit.X;
				_gameMap._hoveredTileY = _gameMap._editorStoredUnit.Y;
			}

			private void DisableEditor()
			{
				_gameMap.ClearEditorBaseLayer();

				var storedUnit = _gameMap._editorStoredUnit;

				if (storedUnit != null && Human == storedUnit.Owner)
					Game.ActiveUnit = storedUnit;

				_gameMap._editorStoredUnit = null;
			}
		}
	}
}