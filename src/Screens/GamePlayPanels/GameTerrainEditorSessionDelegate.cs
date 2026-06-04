// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

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

				_gameMap._editorState.CityOwner = Game.PlayerNumber(Game.Human);

				if (_gameMap._editorStoredUnit == null)
					return;

				_gameMap._hoveredTileX = _gameMap._editorStoredUnit.X;
				_gameMap._hoveredTileY = _gameMap._editorStoredUnit.Y;
			}

			private void DisableEditor()
			{
				var storedUnit = _gameMap._editorStoredUnit;

				if (storedUnit != null && Game.Human == storedUnit.Owner)
					Game.ActiveUnit = storedUnit;

				_gameMap._editorStoredUnit = null;
			}
		}
	}
}