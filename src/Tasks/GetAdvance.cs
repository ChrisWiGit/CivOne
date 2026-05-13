// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Linq;
using CivOne.Advances;
using CivOne.Buildings;

namespace CivOne.Tasks
{
	internal class GetAdvance : GameTask
	{
		private readonly IAdvance _advance;
		private readonly Player _player;
		private readonly bool _human;
		
		private void CivilopediaClosed(object sender, EventArgs args)
		{
			EndTask();
		}
		
		public override void Run()
		{
			_player.AddAdvance(_advance);

			RemoveObsoleteBuildingsOnNewTech();

			if (_player.CurrentResearch == _advance)
			{
				_player.CurrentResearch = null;
				_player.Science = 0; // fire-eggs 20170729 if player acquires advance from city/steal/hut, do NOT roll-over existing lightbulbs
			}

			if (!_human)
			{
				EndTask();
				return;
			}

			if (_player.CurrentResearch == null)
			{
				GameTask.Enqueue(new TechSelect(_player));
			}

			Screens.Civilopedia civilopedia = new Screens.Civilopedia(_advance, discovered: true);
			civilopedia.Closed += CivilopediaClosed;
			Common.AddScreen(civilopedia);
		}

		private void RemoveObsoleteBuildingsOnNewTech()
		{
			foreach (City city in _player.Cities)
			{
				foreach (IBuilding building in city.Buildings)
				{
					if (building.ObsoleteTechs.Any(t => t.Id == _advance.Id))
					{
						city.RemoveBuilding(building);
					}
				}
			}
		}

		public GetAdvance(Player player, IAdvance advance)
		{
			_advance = advance;
			_player = player;
			_human = (Human == player);
		}
	}
}