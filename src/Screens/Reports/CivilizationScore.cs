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
using CivOne.Events;
using CivOne.Graphics;
using CivOne.Enums;
using CivOne.Screens.Services;
using CivOne.Services;

namespace CivOne.Screens.Reports
{
	/// <summary>
	/// Report screen that shows a detailed breakdown of a civilization's score.
	/// </summary>
	/// <remarks>
	/// Uses the `ICivilizationScoreService` to compute totals and individual components.
	/// </remarks>
	[ScreenResizeable]
	internal class CivilizationScore : BaseReport
	{
		private readonly ICivilizationScoreService _civilizationScoreService;

		private const int HappyCitizenScoreWeight = 2;
		private const int CityScoreWeight = 3;
		private const int AdvanceScoreWeight = 10;
		private const int WonderScoreWeight = 50;
		private const int GoldPerScorePoint = 25;
		
		// prevents accidental immediate closure of the report when opened via a key press
		private const int InitialInputDelayMs = 500;

		private bool _update = true;
		private readonly long _ignoreInputUntil;

		public override bool KeyDown(KeyboardEventArgs args)
		{
			if (Environment.TickCount64 < _ignoreInputUntil)
			{
				return true;
			}

			if (args.Key != Key.Enter && args.Key != Key.Space && args.Key != Key.Escape)
			{
				return true;
			}

			Destroy();
			return true;
		}

		public override bool MouseDown(ScreenEventArgs args)
		{
			if (Environment.TickCount64 < _ignoreInputUntil)
			{
				return true;
			}

			Destroy();
			return true;
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

		private int DrawCityCitizens(Citizen[] citizens, int startX, int maxX, int startY, ref int currentX)
		{
			int group = -1;
			int y = startY;
			const int citizenStep = 6;
			const int lineStep = 8;

			for (int i = 0; i < citizens.Length; i++)
			{
				int nextX = currentX + citizenStep;
				if (group != (group = Common.CitizenGroup(citizens[i])) && group > 0 && i > 0)
				{
					nextX += (group == 3) ? 2 : 1;
				}

				if (nextX > maxX)
				{
					y += lineStep;
					currentX = startX;
					nextX = currentX + citizenStep;
				}

				currentX = nextX;
				this.AddLayer(Icons.Citizen(citizens[i]), currentX, y);
			}

			return y;
		}

		private int CityScore(CitizenTypes citizens)
        {
			// don't count unhappy
			// happy is *2
			// all others are content
			return HappyCitizenScoreWeight * citizens.happy + (citizens.Citizens.Length - citizens.unhappy - citizens.redShirt);
        }

		private void Render()
		{
			this.Clear(3);
			DrawReportHeader();

			City[] cities = Human.Cities;
			int fontHeight = Resources.GetFontHeight(0);
			string tribeName = Human.TribeName;
			int wonderCount = 0;
			CitizenTypes[] citizens = cities.Select(c => c.GetCitizenTypes()).ToArray();

			int cityCount = cities.Length;
			int populationScore = Human.Population;
			int cityScore = cityCount * CityScoreWeight;
			int advanceScore = Human.Advances.Length * AdvanceScoreWeight;
			int goldScore = Math.Max(0, Human.Gold / GoldPerScorePoint);
			int citizenScore = 0;

			foreach (CitizenTypes cityCitizens in citizens)
			{
				citizenScore += CityScore(cityCitizens);
			}

			int yy = OffsetY + 32;
			this.DrawText(TranslateFormatted("{0} Citizens ({1})", tribeName, citizenScore), 0, 15, OffsetX + 8, yy);

			int citizenStartX = OffsetX + 8;
			int citizenMaxX = OffsetX + 312;
			int currentX = citizenStartX;
			int citizenY = yy + fontHeight;
			int lastCitizenY = citizenY;
			foreach (CitizenTypes cityCitizens in citizens)
			{
				lastCitizenY = DrawCityCitizens(cityCitizens.Citizens, citizenStartX, citizenMaxX, citizenY, ref currentX);
				currentX += 1;
				if (currentX > citizenMaxX)
				{
					lastCitizenY += 8;
					currentX = citizenStartX;
				}
			}

			foreach (City city in cities)
			{
				wonderCount += city.Wonders.Length;
			}

			int wonderScore = wonderCount * WonderScoreWeight;
			int totalScore = _civilizationScoreService.TotalScore(Human);

			yy = lastCitizenY + 16;
			this.DrawText(TranslateFormatted("Cities ({0})", cityScore), 0, 15, OffsetX + 8, yy);

			yy += fontHeight + 4;
			this.DrawText(TranslateFormatted("Population ({0})", populationScore), 0, 15, OffsetX + 8, yy);

			yy += fontHeight + 4;
			this.DrawText(TranslateFormatted("Advances ({0})", advanceScore), 0, 15, OffsetX + 8, yy);

			yy += fontHeight + 4;
			this.DrawText(TranslateFormatted("{0} Achievements ({1})", tribeName, wonderScore), 0, 15, OffsetX + 8, yy);

			yy += fontHeight + 4;
			this.DrawText(TranslateFormatted("Treasury ({0})", goldScore), 0, 15, OffsetX + 8, yy);

			yy += fontHeight + 4;
			this.DrawText(TranslateFormatted("Total Score: {0}", totalScore), 0, 15, OffsetX + 8, yy);
		}

		protected override bool HasUpdate(uint gameTick)
        {
			if (!_update)
				return false;

			Render();

			_update = false;
			return true;
        }

		protected override void Resize(int width, int height)
		{
			base.Resize(width, height);
			_update = true;
		}

		public override string Title() => Translate("CIVILIZATION SCORE");

		public CivilizationScore(ICivilizationScoreService civilizationScoreService = null) : base(3)
		{
			_civilizationScoreService = civilizationScoreService ?? CivilizationScoreServiceFactory.CreateDefault();
			// Delay initial input briefly so the key that opened the report does not close it immediately again.
			_ignoreInputUntil = Environment.TickCount64 + InitialInputDelayMs;
			Render();
		}
	}
}