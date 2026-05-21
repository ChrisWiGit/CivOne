// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using CivOne.Services.EndGame;
using CivOne.Units;

namespace CivOne.Tasks
{
	[Fast]
	internal class Turn : GameTask
	{
		private const int TURN_TIME = 10;

		private ITurn _turnObject = null;
		private IUnit _unit = null;
		private bool _endTurn = false;

		private Player _gameOver = null;

		private int _step = 0;

		protected override bool Step()
		{
			if (_unit != null)
			{
				Game.CurrentPlayer.AI.Move(_unit);
				EndTask();
			}
			if (_endTurn && _step-- <= 0)
			{
				Game.EndTurn(0);
				EndTask();
			}
			return true;
		}

		public override void Run()
		{
			if (HandleNewTurn())
			{
				return;
			}

			if (_unit != null)
			{
				return;
			}

			if (PauseHumanEndTurn())
			{
				return;
			}

			if (HandleAiEndTurn())
			{
				return;
			}

			if (HandleGameOver())
			{
				return;
			}

			EndTask();
		}

		private bool PauseHumanEndTurn()
		{
			if (!_endTurn || !Game.CurrentPlayer.IsHuman)
			{
				return false;
			}

			_step = TURN_TIME;
			return true;
		}

		private bool HandleNewTurn()
		{
			if (_turnObject == null)
			{
				return false;
			}

			_turnObject.NewTurn();
			EndTask();
			return true;
		}

		private bool HandleAiEndTurn()
		{
			if (!_endTurn || Game.CurrentPlayer.IsHuman)
			{
				return false;
			}

			Game.EndTurn(1);
			EndTask();
			return true;
		}

		private bool HandleGameOver()
		{
			if (_gameOver == null)
			{
				return false;
			}

			if (_gameOver.IsHuman)
			{
				_ = EndGameServiceFactory.CreateForHuman().HandleDefeatAsync();
			}
			else
			{
				// TODO: Spawn barbarians or respawn civilization
			}

			EndTask();
			return true;
		}

		public static Turn New(ITurn turnObject)
		{
			return new Turn()
			{
				_turnObject = turnObject
			};
		}

		public static Turn Move(IUnit unit)
		{
			return new Turn()
			{
				_unit = unit
			};
		}

		public static Turn End()
		{
			return new Turn()
			{
				_endTurn = true
			};
		}

		public static Turn GameOver(Player player)
		{
			return new Turn()
			{
				_gameOver = player
			};
		}

		private Turn()
		{
			
		}
	}
}