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
		private readonly bool _debugMode;
		private readonly int _fontHeight;

		private bool _update = true;
		private const byte HeaderAndLeaderFontId = 0;
		private const int HeaderBoxX = 78;
		private const int HeaderBoxY = 8;
		private const int HeaderBoxWidth = 164;
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

		private int GetPlayerRatingPercent() => _debugMode ? _debugScore : 19;

		private void DrawMessageBox(int ox, int oy, LeaderOrderResult leaderOrderResult)
		{
			int boxHeight = 3 + (_fontHeight * 3);

			DrawPanel(ox + HeaderBoxX, oy + HeaderBoxY, HeaderBoxWidth, boxHeight, border: true);

			int textCenterX = ox + HeaderBoxX + (HeaderBoxWidth / 2);
			this.DrawText(Translate("Sire, your Civilization"), HeaderAndLeaderFontId, 15, textCenterX, oy + HeaderBoxY + 2, TextAlign.Center)
				.DrawText(string.Format(Translate("Rating of {0}% exceeds even"), leaderOrderResult.RatingPercent), HeaderAndLeaderFontId, 15, textCenterX, oy + HeaderBoxY + 2 + _fontHeight, TextAlign.Center)
				.DrawText(string.Format(Translate("{0}!"), leaderOrderResult.SelectedLeaderName), HeaderAndLeaderFontId, 15, textCenterX, oy + HeaderBoxY + 2 + (_fontHeight * 2), TextAlign.Center);
		}

		private void DrawPortraits(int ox, int oy)
		{
			const int frameOffsetFromBorder = 9;
			const int portraitOffsetX = 1;
			const int portraitOffsetY = 1;
			const int frameY = frameOffsetFromBorder;

			AdvisorGovernment government = ResolveAdvisorGovernment();
			AdvisorEra era = ResolveAdvisorEra();
			byte frameColour = era == AdvisorEra.Ancient ? (byte)0 : (byte)9;

			IBitmap tradePortrait = _portraitSpriteProvider.GetPortrait(AdvisorType.TradeAdvisor, government: government, era: era);
			IBitmap foreignPortrait = _portraitSpriteProvider.GetPortrait(AdvisorType.ForeignAdvisor, government: government, era: era);
			
			int frameWidth = tradePortrait.Width() + 2;
			int frameHeight = tradePortrait.Height() + 2;

			int leftFrameX = frameOffsetFromBorder;
			int rightFrameX = ox + Width - frameWidth - frameOffsetFromBorder;

			this.FillRectangle(ox + leftFrameX, oy + frameY, frameWidth, frameHeight, 15);
			DrawFrame(this, ox + leftFrameX, oy + frameY, frameWidth, frameHeight, frameColour);
			this.AddLayer(tradePortrait, ox + leftFrameX + portraitOffsetX, oy + frameY + portraitOffsetY);

			this.FillRectangle(rightFrameX, oy + frameY, frameWidth, frameHeight, 15);
			DrawFrame(this, rightFrameX, oy + frameY, frameWidth, frameHeight, frameColour);
			this.AddLayer(foreignPortrait, rightFrameX + portraitOffsetX, oy + frameY + portraitOffsetY);
		}

		private AdvisorGovernment ResolveAdvisorGovernment()
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

		private AdvisorEra ResolveAdvisorEra() => Human.HasAdvance<Adv.Invention>() ? AdvisorEra.Modern : AdvisorEra.Ancient;

		private void DrawLeaderRanking(int ox, int oy, LeaderOrderResult leaderOrderResult)
		{
			int rowStep = _fontHeight + 1;
			int listTopLimit = oy + HeaderBoxY + (3 + (_fontHeight * 3)) + 2;
			int bottomY = (Height - 9) - _fontHeight;
			if (bottomY < listTopLimit)
			{
				return;
			}

			int availableHeight = bottomY - listTopLimit;
			int maxVisibleRows = (availableHeight / rowStep) + 1;
			IReadOnlyList<string> leaderNamesFromSelectedDown = leaderOrderResult
				.OrderedLeaderNames
				.Skip(leaderOrderResult.SelectedLeaderIndex)
				.ToArray();
			int visibleRows = Math.Min(leaderNamesFromSelectedDown.Count, Math.Max(1, maxVisibleRows));
			IReadOnlyList<string> visibleLeaderNames = leaderNamesFromSelectedDown.Take(visibleRows).ToArray();

			int centerX = ox + 160;
			int highlightX = ox + 84;
			int highlightWidth = 152;

			// Row 0 = selected (top), row visibleRows-1 = Dan Quayle (bottom)
			for (int i = 0; i < visibleRows; i++)
			{
				int y = bottomY - ((visibleRows - 1 - i) * rowStep);
				if (y < listTopLimit)
				{
					continue;
				}

				bool selected = i == 0;
				if (selected)
				{
					this.FillRectangle(highlightX, y - 1, highlightWidth, _fontHeight + 2, 14);
				}

				this.DrawText(
					visibleLeaderNames[i],
					HeaderAndLeaderFontId,
					selected ? (byte)5 : (byte)8,
					centerX,
					y,
					TextAlign.Center);
			}
		}

		private void DrawDebugInfo(int ox, int oy)
		{
			if (!_debugMode)
			{
				return;
			}

			int debugTextY = Height - (_fontHeight * 2) - 1;
			this.DrawText("F1/F2 Inc/Dec Score", HeaderAndLeaderFontId, 5, ox + 8, debugTextY);
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

		private bool DecreaseScoreStep()
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

		private void Render()
		{
			int ox = OffsetX;
			int oy = OffsetY;
			LeaderOrderResult leaderOrderResult = _leaderOrderDelegate.Calculate(GetPlayerRatingPercent());

			this.Clear(15);
			DrawBorder(0);

			DrawMessageBox(ox, oy, leaderOrderResult);
			DrawPortraits(ox, oy);
			DrawLeaderRanking(ox, oy, leaderOrderResult);
			DrawDebugInfo(ox, oy);
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
				if (DecreaseScoreStep())
				{
					_update = true;
					Refresh();
				}
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

		public TopLeaderScreen() : this(debugMode: false, environment: null, portraitSpriteProvider: null, leaderOrderDelegate: null)
		{
		}

		public TopLeaderScreen(
			bool debugMode,
			TopLeaderScreenEnvironment environment = null,
			IAdvisorPortraitSpriteProvider portraitSpriteProvider = null,
			LeaderOrderDelegate leaderOrderDelegate = null) : base(MouseCursor.None)
		{
			var _environment = environment ?? new TopLeaderScreenEnvironment();
			_portraitSpriteProvider = portraitSpriteProvider ?? AdvisorPortraitSpriteProviderFactory.GetInstance();
			_leaderOrderDelegate = leaderOrderDelegate ?? new LeaderOrderDelegate(TranslationServiceFactory.CreateDefault());
			_debugMode = debugMode;
			_debugScore = 19;
			_fontHeight = _environment.GetFontHeight(HeaderAndLeaderFontId);
			

			const byte PALETTE_START_INDEX = 144;
			Palette = _environment.GetDefaultPalette().Merge(_portraitSpriteProvider.Palette, PALETTE_START_INDEX);
		}
	}
}