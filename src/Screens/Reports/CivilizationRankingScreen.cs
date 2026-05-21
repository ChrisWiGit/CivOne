// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Collections.Generic;
using System.Drawing;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.Screens.PalaceAssets;
using CivOne.Services.Random;

namespace CivOne.Screens.Reports
{

	public static class CivilizationRankingScreenFactory
	{
		public static CivilizationRankingScreen Create(
			bool randomSelection = true)
		{
			return new CivilizationRankingScreen(
				randomSelection: randomSelection,
				debugMode: false);
		}

		public static CivilizationRankingScreen CreateDebug(
			bool randomSelection = false)
		{
			return new CivilizationRankingScreen(
				randomSelection: randomSelection,
				debugMode: true);
		}
	}

	[Modal, ScreenResizeable]
	public class CivilizationRankingScreen : BaseScreen
	{
		private static readonly CivilizationRankingCategory[] CategoryCycle =
		[
			CivilizationRankingCategory.Richest,
			CivilizationRankingCategory.Strongest,
			CivilizationRankingCategory.MostAdvanced,
			CivilizationRankingCategory.Happiest,
			CivilizationRankingCategory.Largest
		];

		private static readonly CivilizationRankingHistorian[] Historians =
		[
			CivilizationRankingHistorian.Herodotus,
			CivilizationRankingHistorian.Pliny,
			CivilizationRankingHistorian.Toynbee,
			CivilizationRankingHistorian.Gibbon
		];

		private static readonly string[] RankingAdjectives =
		[
			"Glorious",
			"Great",
			"Good",
			"Average",
			"Poor",
			"Miserable",
			"Hopeless"
		];

		private readonly ICivilizationRankingService _rankingService;
		private readonly IPreviewPalaceRenderer _palaceRenderer;

		private readonly CivilizationRankingHistorian _historian;
		private CivilizationRankingCategory _category;
		private bool _showAllCivilizations;
		private bool _update = true;

		private readonly bool _debugMode = false;

		public CivilizationRankingScreen(
			CivilizationRankingHistorian? historian = null,
			CivilizationRankingCategory? category = null,
			bool randomSelection = true,
			bool debugMode = false,
			ICivilizationRankingService rankingService = null,
			IPreviewPalaceRenderer palaceRenderer = null,
			IRandomService randomService = null) : base(MouseCursor.None)
		{
			_rankingService = rankingService ?? CivilizationRankingServiceFactory.GetInstance();
			_palaceRenderer = palaceRenderer ?? PreviewPalaceRendererFactory.GetInstance();
			IRandomService effectiveRandomService = randomService ?? RandomServiceFactory.Create();

			_historian = randomSelection || historian == null
				? Historians[effectiveRandomService.Next(Historians.Length)]
				: historian.Value;

			_category = randomSelection || category == null
				? CategoryCycle[effectiveRandomService.Next(CategoryCycle.Length)]
				: category.Value;
			_showAllCivilizations = false;
			_debugMode = debugMode;

			Palette = Common.DefaultPalette;
			Refresh();
		}

		protected override bool HasUpdate(uint gameTick)
		{
			if (!_update && !RefreshNeeded())
			{
				return false;
			}

			_update = false;
			DrawScreen();
			return true;
		}

		public override bool KeyDown(KeyboardEventArgs args)
		{
			if (_debugMode && args[Key.F1])
			{
				CycleCategory();
				return true;
			}

			if (_debugMode && args[Key.F2])
			{
				ToggleCivilizationVisibility();
				return true;
			}

			Destroy();
			return true;
		}

		public override bool MouseDown(ScreenEventArgs args)
		{
			Destroy();
			return true;
		}

		protected override void Resize(int width, int height)
		{
			base.Resize(width, height);
			_update = true;
		}

		private void CycleCategory()
		{
			int categoryIndex = Array.IndexOf(CategoryCycle, _category);
			if (categoryIndex < 0)
			{
				categoryIndex = 0;
			}

			_category = CategoryCycle[(categoryIndex + 1) % CategoryCycle.Length];
			_update = true;
			Refresh();
		}

		private void ToggleCivilizationVisibility()
		{
			_showAllCivilizations = !_showAllCivilizations;
			_update = true;
			Refresh();
		}

		private void DrawScreen()
		{
			int offsetX = Math.Max(0, (Width - 320) / 2);
			int offsetY = Math.Max(0, (Height - 200) / 2);

			var screen = this.Clear(3)
				.DrawText(Translate("CIVILIZATION RANKING"), 0, 5, offsetX + 160, offsetY + 3, TextAlign.Center)
				.DrawText(Translate("CIVILIZATION RANKING"), 0, 15, offsetX + 160, offsetY + 2, TextAlign.Center)
				.DrawText(TranslateFormatted("{0} completes his great historical work:", GetHistorianLabel(_historian)), 0, 15, offsetX + 160, offsetY + 13, TextAlign.Center)
				.DrawText(TranslateFormatted("'The {0} Civilization of the World'", GetCategoryHeadline(_category)), 0, 15, offsetX + 160, offsetY + 21, TextAlign.Center);

			if (_debugMode)
			{
				screen.DrawText(Translate("F1: Cycle Category   F2: Toggle All/Known Civilizations"), 1, 15, offsetX + 6, offsetY + 192);
			}

			IReadOnlyList<CivilizationRankingRow> rankingRows = GetRowsByCategory(_category);
			int rowTop = offsetY + 40;
			for (int i = 0; i < rankingRows.Count; i++)
			{
				bool isFirstRow = i == 0;
				rowTop = DrawRankingRow(offsetX, rowTop, rankingRows[i], isFirstRow);
				if (rowTop > offsetY + 190)
				{
					break;
				}
			}
		}

		private int DrawRankingRow(int offsetX, int rowTop, CivilizationRankingRow rankingRow, bool isFirstRow)
		{
			const int rowWidth = 304;
			const int rowHeight = 14;
			int rowX = offsetX + 8;

			IBitmap palacePreview = _palaceRenderer.RenderPalace(rankingRow.PalaceData);
			int palaceWidth = palacePreview.Width();
			int palaceHeight = _palaceRenderer.GetMaxPalaceHeight(rankingRow.PalaceData) - 2;

			bool hasPalace = palaceWidth > 1 || palaceHeight > 1;
			int dockedTopOverlap = hasPalace ? Math.Max(0, palaceHeight) : 0;
			int rowY = rowTop + (isFirstRow ? 0 : dockedTopOverlap);

			byte rowColour = Common.ColourLight[rankingRow.PlayerColorId];
			this.FillRectangle(rowX, rowY, rowWidth, rowHeight, rowColour)
				.FillRectangle(rowX + 1, rowY + 1, rowWidth - 2, rowHeight - 2, 3);

			if (hasPalace)
			{
				int palaceX = rowX + ((rowWidth - palaceWidth) / 2);
				int palaceY = rowY - palacePreview.Height() + 1;
				this.AddLayer(palacePreview, palaceX, palaceY, dispose: true);
			}
			else
			{
				palacePreview.Dispose();
			}

			string rankingText = TranslateFormatted("{0}. The {1} {2}", rankingRow.RankNumber, GetRankingAdjective(rankingRow.RankNumber - 1), rankingRow.CivilizationName);
			this.DrawText(rankingText, font: 0, colour: 15, x: offsetX + 160, y: rowY + 4, align: TextAlign.Center);

			return rowY + rowHeight + 2;
		}

		private IReadOnlyList<CivilizationRankingRow> GetRowsByCategory(CivilizationRankingCategory category)
		{
			return category switch
			{
				CivilizationRankingCategory.Richest => _rankingService.GetRichest(_showAllCivilizations),
				CivilizationRankingCategory.Strongest => _rankingService.GetStrongest(_showAllCivilizations),
				CivilizationRankingCategory.MostAdvanced => _rankingService.GetMostAdvanced(_showAllCivilizations),
				CivilizationRankingCategory.Happiest => _rankingService.GetHappiest(_showAllCivilizations),
				CivilizationRankingCategory.Largest => _rankingService.GetLargest(_showAllCivilizations),
				_ => _rankingService.GetLargest(_showAllCivilizations)
			};
		}

		private string GetRankingAdjective(int index)
		{
			if (index < 0)
			{
				return Translate(RankingAdjectives[^1]);
			}

			if (index >= RankingAdjectives.Length)
			{
				return Translate(RankingAdjectives[^1]);
			}

			return Translate(RankingAdjectives[index]);
		}

		private string GetHistorianLabel(CivilizationRankingHistorian historian)
		{
			return historian switch
			{
				CivilizationRankingHistorian.Herodotus => Translate("Herodotus"),
				CivilizationRankingHistorian.Pliny => Translate("Pliny"),
				CivilizationRankingHistorian.Toynbee => Translate("Toynbee"),
				CivilizationRankingHistorian.Gibbon => Translate("Gibbon"),
				_ => Translate("Herodotus")
			};
		}

		private string GetCategoryHeadline(CivilizationRankingCategory category)
		{
			return category switch
			{
				CivilizationRankingCategory.Richest => Translate("RICHEST"),
				CivilizationRankingCategory.Strongest => Translate("STRONGEST"),
				CivilizationRankingCategory.MostAdvanced => Translate("MOST ADVANCED"),
				CivilizationRankingCategory.Happiest => Translate("HAPPIEST"),
				CivilizationRankingCategory.Largest => Translate("LARGEST"),
				_ => Translate("LARGEST")
			};
		}
	}
}