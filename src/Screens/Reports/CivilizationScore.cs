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
using CivOne.Services.Score;
using CivOne.Wonders;

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

		private const int TextX = 16;
		private const int TotalMaxY = 186;
		private const int ScoreBarX = 35;
		private const int ScoreBarY = 192;
		private const int ScoreBarWidth = 250;
		private const int ScoreBarHeight = 7;

		private readonly CivilizationScoreLayoutDelegate _layout = new();

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

		private static void DrawHappyRow(Picture output, int yy, int happy, int content, int unhappy, int redShirt, int ent, int sci, int tax)
		{
			int dex = 0;
			for (int x = 0; x < happy; x++)
				output.AddLayer(Icons.Citizen((x % 2 == 0) ? Citizen.HappyMale : Citizen.HappyFemale), 7 + (8 * dex++), yy);
			for (int x = 0; x < content; x++)
				output.AddLayer(Icons.Citizen((x % 2 == 0) ? Citizen.ContentMale : Citizen.ContentFemale), 7 + (8 * dex++), yy);
			for (int x = 0; x < redShirt; x++)
				output.AddLayer(Icons.Citizen((x % 2 == 0) ? Citizen.RedShirtMale : Citizen.RedShirtFemale), 7 + (8 * dex++), yy);
			for (int x = 0; x < unhappy; x++)
				output.AddLayer(Icons.Citizen((x % 2 == 0) ? Citizen.UnhappyMale : Citizen.UnhappyFemale), 7 + (8 * dex++), yy);
			for (int x = 0; x < ent; x++)
				output.AddLayer(Icons.Citizen(Citizen.Entertainer), 7 + (8 * dex++), yy);
			for (int x = 0; x < sci; x++)
				output.AddLayer(Icons.Citizen(Citizen.Scientist), 7 + (8 * dex++), yy);
			for (int x = 0; x < tax; x++)
				output.AddLayer(Icons.Citizen(Citizen.Taxman), 7 + (8 * dex++), yy);
		}

		private static void DrawHappyRow(Picture output, int yy, CitizenTypes group)
		{
			DrawHappyRow(output, yy, group.happy, group.content, group.unhappy, group.redShirt, group.elvis, group.einstein, group.taxman);
		}

		private void Render()
		{
			this.Clear(3);
			DrawReportHeader();

			City[] cities = Human.Cities;
			string tribeName = Human.TribeName;
			Citizen[] citizens = [.. cities.SelectMany(c => c.GetCitizenTypes(CivilizationScoreService.ScoringLuxuryRate).Citizens)];
			IWonder[] wonders = [.. cities.SelectMany(c => c.Wonders).OrderBy(w => w.Id)];
			CivilizationScoreBreakdown score = _civilizationScoreService.Calculate(Human);
			string otherTerms = OtherTermsLine(score);
			CivilizationScoreLayout layout = _layout.Calculate(citizens.Length, wonders.Length, score.SpaceShipValue != 0, otherTerms.Length > 0);

			DrawLine(TranslateFormatted("{0} Citizens ({1})", tribeName, score.CitizenScore), layout.CitizensHeaderY);
			for (int i = 0; i < layout.CitizenIcons.Count; i++)
			{
				(int x, int y) = layout.CitizenIcons[i];
				this.AddLayer(Icons.Citizen(citizens[i]), OffsetX + x, OffsetY + y);
			}

			DrawLine(TranslateFormatted("{0} Achievements ({1})", tribeName, score.WonderScore), layout.AchievementsHeaderY);
			for (int i = 0; i < layout.Wonders.Count; i++)
			{
				(int iconX, int iconY, int nameX, int nameY) = layout.Wonders[i];
				if (wonders[i].SmallIcon is IBitmap icon)
				{
					this.AddLayer(icon, OffsetX + iconX, OffsetY + iconY);
				}
				this.DrawText(wonders[i].Name, 0, 11, OffsetX + nameX, OffsetY + nameY);
			}

			if (layout.SpaceShipY is int spaceShipY)
			{
				DrawLine(score.SpaceShipApplied
					? TranslateFormatted("SpaceShip: ({0})", score.SpaceShipValue)
					: TranslateFormatted("SpaceShip: ({0} possible)", score.SpaceShipValue), spaceShipY);
			}

			if (layout.OtherTermsY is int otherTermsY)
			{
				DrawLine(otherTerms, otherTermsY);
			}

			if (layout.TotalY < TotalMaxY)
			{
				DrawLine(score.IsBonus
					? TranslateFormatted("Bonus Score: {0}", score.Total)
					: TranslateFormatted("Total Score: {0}", score.Total), layout.TotalY);
			}

			DrawScoreBar(score.Total);
		}

		private void DrawLine(string text, int y) => this.DrawText(text, 0, 15, OffsetX + TextX, OffsetY + y);

		private void DrawScoreBar(int total)
		{
			this.FillRectangle(OffsetX + ScoreBarX, OffsetY + ScoreBarY, ScoreBarWidth, ScoreBarHeight, 8);
			if (total > 0)
			{
				this.FillRectangle(OffsetX + ScoreBarX, OffsetY + ScoreBarY, Math.Clamp(total / 4, 0, ScoreBarWidth), ScoreBarHeight, 11);
			}
		}

		private string OtherTermsLine(CivilizationScoreBreakdown score)
		{
			string line = string.Empty;
			if (score.PollutionScore != 0)
			{
				line += TranslateFormatted("Pollution: ({0})", score.PollutionScore) + "   ";
			}
			if (score.PeaceScore != 0)
			{
				line += TranslateFormatted("Peace: (+{0})", score.PeaceScore) + " ";
			}
			if (score.FutureTechScore != 0)
			{
				line += TranslateFormatted("Future Tech: (+{0})", score.FutureTechScore);
			}

			return line.TrimEnd();
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

		public CivilizationScore(ICivilizationScoreService? civilizationScoreService = null) : base(3)
		{
			_civilizationScoreService = civilizationScoreService ?? CivilizationScoreServiceFactory.CreateDefault();
			// Delay initial input briefly so the key that opened the report does not close it immediately again.
			_ignoreInputUntil = Environment.TickCount64 + InitialInputDelayMs;
			Render();
		}
	}
}