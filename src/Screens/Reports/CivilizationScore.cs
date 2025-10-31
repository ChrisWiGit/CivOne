// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.Linq;
using CivOne.Graphics;
using CivOne.Enums;
using CivOne.Screens.Services;

namespace CivOne.Screens.Reports
{
	internal class CivilizationScore : BaseReport
	{
		private bool _update = true;

		public CivilizationScore() : base("CIVILIZATION SCORE", 3)
		{
			
		}

		private void DrawHappyRow(Picture output, int yy, int happy, int content, int unhappy, int ent, int sci, int tax)
		{
			int dex = 0;
			for (int x = 0; x < happy; x++)
				output.AddLayer(Icons.Citizen((x % 2 == 0) ? Citizen.HappyMale : Citizen.HappyFemale), 7 + (8 * dex++), yy);
			for (int x = 0; x < content; x++)
				output.AddLayer(Icons.Citizen((x % 2 == 0) ? Citizen.ContentMale : Citizen.ContentFemale), 7 + (8 * dex++), yy);
			for (int x = 0; x < unhappy; x++)
				output.AddLayer(Icons.Citizen((x % 2 == 0) ? Citizen.UnhappyMale : Citizen.UnhappyFemale), 7 + (8 * dex++), yy);
			for (int x = 0; x < ent; x++)
				output.AddLayer(Icons.Citizen(Citizen.Entertainer), 7 + (8 * dex++), yy);
			for (int x = 0; x < sci; x++)
				output.AddLayer(Icons.Citizen(Citizen.Scientist), 7 + (8 * dex++), yy);
			for (int x = 0; x < tax; x++)
				output.AddLayer(Icons.Citizen(Citizen.Taxman), 7 + (8 * dex++), yy);
		}

		private void DrawHappyRow(Picture output, int yy, CitizenTypes group)
		{
			DrawHappyRow(output, yy, group.happy, group.content, group.unhappy, group.elvis, group.einstein, group.taxman);
		}

		private int _xx;
		int _citizenY;
		// TODO max wide is 35 citizens

		private void DrawCityCitizens(City _city)
        {
			int group = -1;
			Citizen[] citizens = _city.Citizens.ToArray();
			for (int i = 0; i < _city.Size; i++)
			{
				// TODO probably need to stick to a fixed width
				_xx += 8;
				if (group != (group = Common.CitizenGroup(citizens[i])) && group > 0 && i > 0)
				{
					_xx += (group == 3) ? 4 : 2;
				}
				this.AddLayer(Icons.Citizen(citizens[i]), _xx, _citizenY);
			}
		}

		private int CityScore(City city)
        {
			// don't count unhappy
			// happy is *2
			// all others are content
			return city.HappyCitizens + (city.Size - city.UnhappyCitizens);
        }

		protected override bool HasUpdate(uint gameTick)
        {
			if (!_update)
				return false;

			int totalScore = 0;

			var _cities = Game.GetCities().Where(c => Human == c.Owner);
			int fh = Resources.GetFontHeight(0);
			var TribeName = Human.TribeName;
			int wonderCount = 0;       // TODO

			// Citizen score
			foreach (City city in _cities)
				totalScore += CityScore(city);			

			int yy = 32;
			this.DrawText($"{TribeName} Citizens ({totalScore})", 0, 15, 8, yy);
			yy += fh;

			// Draw citizens
			_xx = 0;
			_citizenY = yy;
			foreach (City city in _cities)
			{
				DrawCityCitizens(city);
				wonderCount += city.Wonders.Length; // TODO different wonders
			}

			// count wonders
			yy += 15;
			this.DrawText($"{TribeName} Achievements ({wonderCount})", 0, 15, 8, yy);
			totalScore += wonderCount * 20;

			// TODO draw wonders
			// TODO draw peace

			yy += fh + 4;
			this.DrawText($"Total Score: {totalScore}", 0, 15, 8, yy);

			_update = false;
			return true;
        }
    }
}