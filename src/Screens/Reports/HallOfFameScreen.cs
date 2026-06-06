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
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.Services;
using CivOne.Services.HallOfFame;

namespace CivOne.Screens.Reports
{
	/// <summary>
	/// Factory methods to create and configure <see cref="HallOfFameScreen"/> instances.
	/// </summary>
	internal static class HallOfFameScreenFactory
	{
		public static HallOfFameScreen ViewScore()
		{
			ITranslationService translationService = TranslationServiceFactory.CreateDefault();
			IHallOfFameDisplayDataService displayDataService = new HallOfFameDisplayDataService();
			IHallOfFamePersistService persistService = HallOfFameServiceFactory.CreatePersistService();
			IHallOfFameEntryComposerService entryComposerService = CreateEntryComposerService(translationService);
			IHallOfFameCommandService commandService = HallOfFameServiceFactory.CreateCommandService(
				storageDirectory: GetStorageDirectory(),
				persistService: persistService,
				entryComposerService: entryComposerService,
				log: LogInfo);
			IReadOnlyList<HallOfFameEntry> entries = persistService.ViewEntries(GetStorageDirectory(), LogInfo);

			return new HallOfFameScreen(entries, displayDataService, commandService, allowClear: false);
		}

		public static HallOfFameScreen AddScore()
		{
			ITranslationService translationService = TranslationServiceFactory.CreateDefault();
			IHallOfFameDisplayDataService displayDataService = new HallOfFameDisplayDataService();
			IHallOfFamePersistService persistService = HallOfFameServiceFactory.CreatePersistService();
			IHallOfFameEntryComposerService entryComposerService = CreateEntryComposerService(translationService);
			IHallOfFameCommandService commandService = HallOfFameServiceFactory.CreateCommandService(
				storageDirectory: GetStorageDirectory(),
				persistService: persistService,
				entryComposerService: entryComposerService,
				log: LogInfo);

			IReadOnlyList<HallOfFameEntry> entries = persistService.AddEntry(
				GetStorageDirectory(),
				entryComposerService.ComposeForHuman(),
				LogInfo);

			return new HallOfFameScreen(entries, displayDataService, commandService, allowClear: true);
		}

		private static IHallOfFameEntryComposerService CreateEntryComposerService(ITranslationService translationService)
		{
			return new HallOfFameEntryComposerService(
				civilizationScoreService: CivilizationScoreServiceFactory.CreateDefault(),
				gameCalendarService: new GameCalendarService(translationService),
				leaderOrderDelegate: new LeaderOrderDelegate(translationService),
				translationService: translationService);
		}

		private static string GetStorageDirectory() => RuntimeHandler.Runtime.StorageDirectory ?? string.Empty;

		private static void LogInfo(string message)
		{
			if (RuntimeHandler.Runtime == null || string.IsNullOrWhiteSpace(message))
			{
				return;
			}

			RuntimeHandler.Runtime.Log(message);
		}
	}

	/// <summary>
	/// Screen that displays the Hall of Fame entries and optionally allows clearing them.
	/// </summary>
	/// <remarks>
	/// Constructed with display and command services supplied by the factory methods.
	/// </remarks>
	[Modal, ScreenResizeable]
	internal class HallOfFameScreen : BaseScreen
	{
		private const int LayoutWidth = 320;
		private const int LayoutHeight = 200;

		private const int MaxRows = 5;
		private const int InitialInputDelayMs = 1200;

		private const byte HeaderFontId = 4;
		private const byte HeaderColor = 5;
		private const byte PrimaryTextColor = 5;
		private const byte SecondaryTextColor = 8;
		private const byte AccentTextColor = 4;
		private const byte PlaceholderTextColor = 5;
		private const byte BackgroundColor = 15;
		private const byte FrameColor = 5;
		private const byte FooterButtonInnerColor = 15;
		private const byte FooterButtonTextColor = 5;

		
		private const int HeaderCenterX = 160;
		private const int HeaderY = 8;
		private const int RowsTop = 40;
		private const int RowStep = 30;
		private const int RowTextX = 14;
		private const int RowHeadlineY = 0;
		private const int RowDetailsY = 9;
		private const int RowRatingY = 18;
		private const int FooterButtonWidth = 74;
		private const int FooterButtonHeight = 16;
		private const int RatingCenterX = 160;
		private const byte FooterButtonFontId = 0;

		private readonly IHallOfFameDisplayDataService _displayDataService;
		private readonly IHallOfFameCommandService _commandService;
		private readonly bool _allowClear;
		private IReadOnlyList<HallOfFameDisplayRow> _rows;

		private bool _update = true;
		private readonly long _ignoreInputUntil = Environment.TickCount64 + InitialInputDelayMs;

		private int OffsetX => Math.Max(0, (Width - LayoutWidth) / 2);
		private int OffsetY => Math.Max(0, (Height - LayoutHeight) / 2);

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
			if (Environment.TickCount64 < _ignoreInputUntil)
			{
				return true;
			}

			if (_allowClear && char.ToUpperInvariant(args.KeyChar) == 'C')
			{
				ApplyClear();
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

			if (!_allowClear)
			{
				Destroy();
				return true;
			}

			(int buttonX, int buttonY) = GetFooterButtonPosition();
			bool insideButton = args.X >= buttonX
				&& args.X < buttonX + FooterButtonWidth
				&& args.Y >= buttonY
				&& args.Y < buttonY + FooterButtonHeight;
			if (insideButton)
			{
				ApplyClear();
				return true;
			}

			Destroy();
			return true;
		}

		private void ApplyClear()
		{
			if (!_allowClear)
			{
				return;
			}

			_rows = _displayDataService.BuildRows(_commandService.Clear(), MaxRows);
			_update = true;
			Refresh();
		}

		private void Render()
		{
			ClearAndFrame();
			DrawTitle();
			DrawRows();
			if (_allowClear)
			{
				DrawFooterButton();
			}
		}

		private void ClearAndFrame()
		{
			this.Clear(BackgroundColor);
			DrawBorder(FrameColor);
		}

		private void DrawTitle()
		{
			string header = $"{Translate("CIVILIZATION")} {Translate("HALL OF FAME")}";
			this.DrawText(header, HeaderFontId, HeaderColor, OffsetX + HeaderCenterX, OffsetY + HeaderY, TextAlign.Center);
		}

		private void DrawRows()
		{
			for (int i = 0; i < _rows.Count; i++)
			{
				DrawRow(_rows[i], i);
			}
		}

		private void DrawRow(HallOfFameDisplayRow row, int index)
		{
			const byte fontId = 0;
			int rowY = OffsetY + RowsTop + (index * RowStep);
			byte detailsColor = row.IsPlaceholder ? PlaceholderTextColor : SecondaryTextColor;
			byte ratingColor = row.IsPlaceholder ? PlaceholderTextColor : AccentTextColor;

			this.DrawText(row.Headline, fontId, row.IsPlaceholder ? PlaceholderTextColor : PrimaryTextColor, OffsetX + RowTextX, rowY + RowHeadlineY);

			if (!string.IsNullOrWhiteSpace(row.Details))
			{
				this.DrawText(row.Details, fontId, detailsColor, OffsetX + RowTextX, rowY + RowDetailsY);
			}

			if (!string.IsNullOrWhiteSpace(row.Rating))
			{
				this.DrawText(row.Rating, fontId, ratingColor, OffsetX + RatingCenterX, rowY + RowRatingY, TextAlign.Center);
			}
		}

		private void DrawFooterButton()
		{
			(int x, int y) = GetFooterButtonPosition();

			DrawButton(
				text: string.Empty,
				fontId: FooterButtonFontId,
				colour: FooterButtonInnerColor,
				colourDark: FooterButtonTextColor,
				x: x,
				y: y,
				width: FooterButtonWidth,
				height: FooterButtonHeight);

			int textY = y + ((FooterButtonHeight - Resources.GetFontHeight(FooterButtonFontId)) / 2);
			this.DrawText(Translate("Clear"), FooterButtonFontId, FooterButtonTextColor, x + (FooterButtonWidth / 2), textY, TextAlign.Center);
		}

		private (int X, int Y) GetFooterButtonPosition()
		{
			return (Width - FooterButtonWidth - BorderTileSize - 1, Height - FooterButtonHeight - BorderTileSize - 1);
		}

		public HallOfFameScreen(IReadOnlyList<HallOfFameEntry> entries, IHallOfFameDisplayDataService displayDataService, IHallOfFameCommandService commandService, bool allowClear = true) : this(entries, displayDataService, commandService, MouseCursor.Pointer, allowClear)
		{
		}

		private HallOfFameScreen(IReadOnlyList<HallOfFameEntry> entries, IHallOfFameDisplayDataService displayDataService, IHallOfFameCommandService commandService, MouseCursor cursor, bool allowClear) : base(cursor)
		{
			_displayDataService = displayDataService ?? throw new ArgumentNullException(nameof(displayDataService));
			_commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
			_allowClear = allowClear;
			_rows = _displayDataService.BuildRows(entries ?? [], MaxRows);
			Palette = Common.DefaultPalette;
			Refresh();
		}
	}
}
