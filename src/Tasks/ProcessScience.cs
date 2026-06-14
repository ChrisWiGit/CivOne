// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Diagnostics;
using System.Linq;
using CivOne.Screens;

namespace CivOne.Tasks
{
	internal class ProcessScience : GameTask
	{
		private readonly Player _player;
		private readonly bool _human;
		
		private void CivilopediaClosed(object? _, EventArgs __)
		{
			_player.CurrentResearch = null;
			Insert(new TechSelect(_player));
			EndTask();
		}

		private void ClosedDiscovery(object? _, EventArgs __)
		{
			if (_player.CurrentResearch == null)
			{
				Log($"ProcessScience: No current research after discovery screen closed. For player {_player.TribeName}.");
				EndTask();
				return;
			}
			Civilopedia civilopedia = new(_player.CurrentResearch, discovered: true);
			civilopedia.Closed += CivilopediaClosed;
			Common.AddScreen(civilopedia);
		}

		private void TryRegisterFutureTech()
		{
			if (_player.Science >= _player.ScienceCost)
			{
				_player.Science -= _player.ScienceCost;
				Game.RegisterFutureTech(_player);
			}
		}

		public override void Run()
		{
			if (_player.CurrentResearch == null)
			{
				if (!_player.AvailableResearch.Any())
				{
					TryRegisterFutureTech();

					EndTask();
					return;
				}

				if (_human)
				{
					Enqueue(new TechSelect(_player));
				}
				else
				{
					Debug.Assert(_player.AI != null, "AI player has no AI implementation");
					Log($"Warning: The player {_player.TribeName} is not human but has no field AI. Skipping research selection.");
					_player.AI?.ChooseResearch();
				}
				EndTask();
				return;
			}

			if (_player.Science < _player.ScienceCost)
			{
				// Not enough lightbulbs, end the task
				EndTask();
				return;
			}

			_player.Science -= _player.ScienceCost;
			_player.AddAdvance(_player.CurrentResearch);

			if (!_human)
			{
				// This is an AI player, handle everything in the background.
				_player.CurrentResearch = null;
				Debug.Assert(_player.AI != null, "AI player has no AI implementation");
				_player.AI?.ChooseResearch();
				EndTask();
				return;
			}

			IScreen discovery;
			if (Game.Animations)
			{
				discovery = new Discovery(_player.CurrentResearch);
			}
			else
			{
				discovery = new Newspaper(null, 
					TranslateFormattedArray("{0} wise men\ndiscover the secret\nof {1}!", _player.TribeName, _player.CurrentResearch.TranslatedName),
					showGovernment: false);
			}
			discovery.Closed += ClosedDiscovery;
			Common.AddScreen(discovery);
		}

		public ProcessScience(Player player)
		{
			_player = player;
			_human = (Human == player);
		}
	}
}