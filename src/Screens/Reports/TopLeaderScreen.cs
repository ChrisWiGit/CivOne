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
using System.Linq;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.Screens.GovernmentPortraits;
using CivOne.Services;
using CivOne.Services.Random;
using Adv = CivOne.Advances;
using Gov = CivOne.Governments;

namespace CivOne.Screens.Reports
{
	internal static class TopLeaderScreenFactory
	{
		public static TopLeaderScreen Create()
		{
			return new TopLeaderScreen();
		}

		public static TopLeaderScreen CreateDebug()
		{
			return new TopLeaderScreen(debugMode: true);
		}
	}

	[Modal, ScreenResizeable]
	internal class TopLeaderScreen : BaseScreen
	{
		private readonly IAdvisorPortraitSpriteProvider _portraitSpriteProvider;
		private readonly LeaderOrderDelegate _leaderOrderDelegate;
		private readonly ICivilizationScoreService _civilizationScoreService;
		private readonly bool _debugMode;
		private readonly int _fontHeight;

		private bool _update = true;
		private const byte HeaderAndLeaderFontId = 0;
		private const int HeaderBoxX = 78;
		private const int HeaderBoxY = 8;
		private const int HeaderBoxWidth = 180;
		private int _debugScore;

		private int OffsetX => Math.Max(0, (Width - 320) / 2);
		private int OffsetY => Math.Max(0, (Height - 200) / 2);

		private static void DrawFrame(IBitmap bitmap, int x, int y, int width, int height, byte frameColour)
		{
			bitmap.FillRectangle(x, y, width, 1, frameColour)
				.FillRectangle(x, y + height - 1, width, 1, frameColour)
				.FillRectangle(x, y, 1, height, frameColour)
				.FillRectangle(x + width - 1, y, 1, height, frameColour);
		}

		private int GetPlayerRatingPercent()
		{
			if (_debugMode)
			{
				return _debugScore;
			}

			int totalScore = _civilizationScoreService.TotalScore(Human);
			int topLeaderThreshold = _leaderOrderDelegate.GetLeaderOrder()[0].RatingThreshold;
			return _civilizationScoreService.RatingPercent(totalScore, topLeaderThreshold);
		}

		private void DrawMessageBox(int ox, int oy, LeaderOrderResult leaderOrderResult)
		{
			const byte textLineCount = 3;
			int boxHeight = 3 + (_fontHeight * textLineCount);

			DrawPanel(ox + HeaderBoxX, oy + HeaderBoxY, HeaderBoxWidth, boxHeight, border: true);

			int textCenterX = ox + HeaderBoxX + (HeaderBoxWidth / 2);
			string[] lines =
			[
				Translate("Sire, your Civilization"),
				TranslateFormatted("Rating of {0}% exceeds even", leaderOrderResult.RatingPercent),
				TranslateFormatted("{0}!", leaderOrderResult.SelectedLeaderName)
			];

			for (int i = 0; i < lines.Length; i++)
			{
				this.DrawText(lines[i], HeaderAndLeaderFontId, 15, textCenterX, oy + HeaderBoxY + 2 + (_fontHeight * i), TextAlign.Center);
			}
		}

		private void DrawPortraits(int ox, int oy)
		{
			int frameOffsetFromBorder = BorderTileSize + 1;
			const int portraitOffsetBorderGapX = 1;
			const int portraitOffsetBorderGapY = 1;
			int frameY = frameOffsetFromBorder;

			AdvisorGovernment government = ResolveAdvisorGovernment();
			AdvisorEra era = ResolveAdvisorEra();
			byte frameColour = era == AdvisorEra.Ancient ? (byte)0 : (byte)9;

			IBitmap tradePortrait = _portraitSpriteProvider.GetPortrait(AdvisorType.TradeAdvisor, government: government, era: era);
			IBitmap foreignPortrait = _portraitSpriteProvider.GetPortrait(AdvisorType.ForeignAdvisor, government: government, era: era);

			int frameWidth = tradePortrait.Width() + 2;
			int frameHeight = tradePortrait.Height() + 2;

			int leftFrameX = frameOffsetFromBorder;
			int rightFrameX = Width - frameWidth - frameOffsetFromBorder;

			this.FillRectangle(ox + leftFrameX, oy + frameY, frameWidth, frameHeight, 15);
			DrawFrame(this, ox + leftFrameX, oy + frameY, frameWidth, frameHeight, frameColour);
			this.AddLayer(tradePortrait, ox + leftFrameX + portraitOffsetBorderGapX, oy + frameY + portraitOffsetBorderGapY);

			this.FillRectangle(ox + rightFrameX, oy + frameY, frameWidth, frameHeight, 15);
			DrawFrame(this, ox + rightFrameX, oy + frameY, frameWidth, frameHeight, frameColour);
			this.AddLayer(foreignPortrait, ox + rightFrameX + portraitOffsetBorderGapX, oy + frameY + portraitOffsetBorderGapY);
		}

		private static AdvisorGovernment ResolveAdvisorGovernment()
		{
			if (Human.Government is Gov.Monarchy)
			{
				return AdvisorGovernment.Monarchy;
			}

			if (Human.Government is Gov.Republic || Human.Government is Gov.Democracy)
			{
				return AdvisorGovernment.Democracy;
			}

			if (Human.Government is Gov.Communism)
			{
				return AdvisorGovernment.Communism;
			}

			return AdvisorGovernment.Despotism;
		}

		private static AdvisorEra ResolveAdvisorEra() => Human.HasAdvance<Adv.Invention>() ? AdvisorEra.Modern : AdvisorEra.Ancient;

		private void DrawLeaderRanking(int ox, int oy, LeaderOrderResult result)
		{
			int rowStep = _fontHeight + 1;
			int topY = oy + HeaderBoxY + 3 + (_fontHeight * 3) + 2;
			int bottomY = Height - (BorderTileSize + 1) - _fontHeight;

			if (bottomY < topY)
				return;

			// calculate how many leaders can fit in the space available.
			int maxRows = ((bottomY - topY) / rowStep) + 1;

			var names = result.OrderedLeaderNames
				.Skip(result.SelectedLeaderIndex)
				.Take(maxRows)
				.ToArray();

			int centerX = ox + 160;
			int highlightX = ox + 84;
			int highlightWidth = 152;

			for (int i = 0; i < names.Length; i++)
			{
				// display from the bottom up, with the selected leader at the bottom
				int y = bottomY - ((names.Length - 1 - i) * rowStep);
				bool selected = i == 0;

				if (selected)
				{
					this.FillRectangle(highlightX, y - 1, highlightWidth, _fontHeight + 2, 14);
				}

				this.DrawText(
					names[i],
					HeaderAndLeaderFontId,
					selected ? (byte)5 : (byte)8,
					centerX,
					y,
					TextAlign.Center);
			}
		}

		private void DrawDebugInfo()
		{
			if (!_debugMode)
			{
				return;
			}
			int borderSize = BorderTileSize;
			const byte black = 5;

			int debugTextY = CanvasHeight - borderSize - _fontHeight;
			this.DrawText(Translate("F1/F2 Inc/Dec Score"), HeaderAndLeaderFontId, black, borderSize, debugTextY);
		}

		private bool IncreaseScoreStep()
		{
			IReadOnlyList<LeaderOrderEntry> order = _leaderOrderDelegate.GetLeaderOrder();
			int? nextHigher = order.Select(x => x.RatingThreshold).Where(x => x > _debugScore).OrderBy(x => x).Cast<int?>().FirstOrDefault();
			if (nextHigher == null)
			{
				return false;
			}

			_debugScore = nextHigher.Value;
			return true;
		}

		private void Render()
		{
			int ox = OffsetX;
			int oy = OffsetY;
			LeaderOrderResult leaderOrderResult = _leaderOrderDelegate.Calculate(GetPlayerRatingPercent());

			this.Clear(15);
			DrawBorder(0);

			DrawMessageBox(OffsetX, 0, leaderOrderResult);
			DrawPortraits(0, 0);
			DrawLeaderRanking(ox, oy, leaderOrderResult);
			DrawDebugInfo();
		}

		protected override bool HasUpdate(uint gameTick)
		{
			if (!_update && !RefreshNeeded())
			{
				return false;
			}

			_update = false;
			Render();
			return true;
		}

		protected override void Resize(int width, int height)
		{
			base.Resize(width, height);
			_update = true;
		}

		public override bool KeyDown(KeyboardEventArgs args)
		{
			if (_debugMode && args[Key.F1])
			{
				if (IncreaseScoreStep())
				{
					_update = true;
					Refresh();
				}
				return true;
			}

			if (_debugMode && args[Key.F2])
			{
				if (DebugDecreaseScoreStep())
				{
					_update = true;
					Refresh();
				}
				return true;
			}

			Destroy();
			return true;
		}

		private bool DebugDecreaseScoreStep()
		{
			IReadOnlyList<LeaderOrderEntry> order = _leaderOrderDelegate.GetLeaderOrder();
			int? nextLower = order.Select(x => x.RatingThreshold).Where(x => x < _debugScore).OrderByDescending(x => x).Cast<int?>().FirstOrDefault();
			if (nextLower == null)
			{
				return false;
			}

			_debugScore = nextLower.Value;
			return true;
		}

		public override bool MouseDown(ScreenEventArgs args)
		{
			Destroy();
			return true;
		}

		public TopLeaderScreen() : this(debugMode: false, environment: null, portraitSpriteProvider: null, leaderOrderDelegate: null)
		{
		}

		public TopLeaderScreen(
			bool debugMode,
			TopLeaderScreenEnvironment environment = null,
			IAdvisorPortraitSpriteProvider portraitSpriteProvider = null,
			LeaderOrderDelegate leaderOrderDelegate = null,
			ICivilizationScoreService civilizationScoreService = null) : base(MouseCursor.None)
		{
			var _environment = environment ?? new TopLeaderScreenEnvironment();
			_portraitSpriteProvider = portraitSpriteProvider ?? AdvisorPortraitSpriteProviderFactory.GetInstance();
			_leaderOrderDelegate = leaderOrderDelegate ?? new LeaderOrderDelegate(TranslationServiceFactory.CreateDefault());
			_civilizationScoreService = civilizationScoreService ?? CivilizationScoreServiceFactory.CreateDefault();
			_debugMode = debugMode;
			_debugScore = 42;
			_fontHeight = _environment.GetFontHeight(HeaderAndLeaderFontId);


			const byte PALETTE_START_INDEX = 144;
			using var defaultPalette = _environment.GetDefaultPalette();
			AdvisorGovernment government = ResolveAdvisorGovernment();
			AdvisorEra era = ResolveAdvisorEra();
			IBitmap tradePortrait = _portraitSpriteProvider.GetPortrait(AdvisorType.TradeAdvisor, government: government, era: era);
			Palette = defaultPalette.Merge(tradePortrait.Palette, PALETTE_START_INDEX);
		}
	}
}