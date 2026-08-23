using System;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Screens.NewGamePanels;
using CivOne.Services;
using CivOne.src;
using Xunit;

namespace CivOne.UnitTests
{
	/// <summary>
	/// Covers the "Level of Competition" menus: which entries they offer, how the pages of the extended
	/// menu are reached, and what a selection reports back to the screen.
	///
	/// The menus are built against a stand-in host, so no screen is involved.
	/// </summary>
	public sealed class NewGameCompetitionMenuDelegateTests : IDisposable
	{
		private readonly MockRuntime _runtime;
		private readonly MockedNewGameMenuHost _host = new();
		private readonly NewGameRulesDelegate _rules;
		private readonly NewGameCompetitionMenuDelegate _delegateUnderTest;

		private int _selectedOpponents = -1;
		private int _cancelCount;

		/// <summary>
		/// Sets up identity translation and a runtime, which menus need for their canvas size.
		/// </summary>
		public NewGameCompetitionMenuDelegateTests()
		{
			TranslationServiceFactory.ResetForTests();
			_runtime = new MockRuntime(new RuntimeSettings { InitialSeed = 24601 });
			_rules = new NewGameRulesDelegate([], () => false);
			_delegateUnderTest = new NewGameCompetitionMenuDelegate(
				_host,
				_rules,
				opponents => _selectedOpponents = opponents,
				() => _cancelCount++);
		}

		private static int PageSize => NewGameCompetitionMenuDelegate.ExtendedMenuPageSize;

		private NewGameCompetitionMenuDelegate CreateDelegateWithBarbarianMenu(BarbarianActivity activity)
		{
			NewGameCompetitionMenuDelegate? competitionMenu = null;
			NewGameBarbarianMenuDelegate barbarianMenu = new(
				_host,
				() => competitionMenu!.ShowMenu(),
				() => activity,
				value => activity = value);

			competitionMenu = new NewGameCompetitionMenuDelegate(
				_host,
				_rules,
				opponents => _selectedOpponents = opponents,
				() => _cancelCount++,
				barbarianMenu: barbarianMenu);

			return competitionMenu;
		}

		/// <summary>
		/// Without a barbarian menu the competition menu stays as it was.
		/// </summary>
		[Fact]
		public void MainMenuHasNoBarbarianEntryWithoutBarbarianMenu()
		{
			_delegateUnderTest.ShowMenu();

			Assert.DoesNotContain(_host.MenuTexts(), text => text.StartsWith("Barbarians", StringComparison.Ordinal));
		}

		/// <summary>
		/// With a barbarian menu the last entry shows the current setting.
		/// </summary>
		[Fact]
		public void MainMenuShowsCurrentBarbarianSetting()
		{
			CreateDelegateWithBarbarianMenu(BarbarianActivity.Raids).ShowMenu();

			string[] texts = _host.MenuTexts();

			Assert.Equal(7, texts.Length);
			Assert.Equal("More Civilizations...", texts[5]);
			Assert.Equal("Barbarians: Raids", texts[6]);
		}

		/// <summary>
		/// The barbarian entry opens the barbarian menu instead of reporting an opponent count.
		/// </summary>
		[Fact]
		public void BarbarianEntryOpensBarbarianMenu()
		{
			CreateDelegateWithBarbarianMenu(BarbarianActivity.VillagesAndRaids).ShowMenu();

			_host.SelectLastItem();

			Assert.Equal("Barbarians...", _host.OpenMenu.Title);
			Assert.Equal(-1, _selectedOpponents);
		}

		/// <summary>
		/// After picking a source the competition menu is back, with the new value in its entry.
		/// </summary>
		[Fact]
		public void PickingBarbarianSourceReturnsToUpdatedMainMenu()
		{
			CreateDelegateWithBarbarianMenu(BarbarianActivity.VillagesAndRaids).ShowMenu();
			_host.SelectLastItem();

			_host.SelectItem(0);

			Assert.Equal("Level of Competition...", _host.OpenMenu.Title);
			Assert.Equal("Barbarians: None", _host.MenuTexts()[6]);
		}

		/// <summary>
		/// The first menu offers the opponent counts of the original game, plus the way to the full list.
		/// </summary>
		[Fact]
		public void MainMenuOffersClassicOpponentCounts()
		{
			_delegateUnderTest.ShowMenu();

			string[] texts = _host.MenuTexts();
			int[] values = _host.MenuValues();

			Assert.Equal("Level of Competition...", _host.OpenMenu.Title);
			Assert.Equal(6, texts.Length);
			Assert.Equal("7 Civilizations", texts[0]);
			Assert.Equal("3 Civilizations", texts[4]);
			Assert.Equal("More Civilizations...", texts[5]);
			Assert.Equal(_rules.MainMenuMaxOpponents, values[0]);
			Assert.Equal(_rules.MainMenuMinOpponents, values[4]);
		}

		/// <summary>
		/// Picking a count closes the menu and hands the opponent count to the screen.
		/// </summary>
		[Fact]
		public void PickingOpponentCountReportsSelection()
		{
			_delegateUnderTest.ShowMenu();

			_host.SelectItem(1);

			Assert.Equal(_rules.MainMenuMaxOpponents - 1, _selectedOpponents);
			Assert.Equal(1, _host.CloseCount);
			Assert.Null(_host.CurrentMenu);
		}

		/// <summary>
		/// The extended menu shows one page of every supported opponent count, framed by its navigation entries.
		/// </summary>
		[Fact]
		public void ExtendedMenuShowsFirstPageWithNavigationEntries()
		{
			_delegateUnderTest.ShowMenu();
			_host.SelectLastItem();

			string[] texts = _host.MenuTexts();
			int[] values = _host.MenuValues();

			Assert.Equal("More Civilizations...", _host.OpenMenu.Title);
			Assert.Equal(PageSize + 2, texts.Length);
			Assert.Equal("Back...", texts[0]);
			Assert.Equal("More...", texts[^1]);
			Assert.Equal(_rules.MinOpponents, values[1]);
			Assert.Equal(_rules.MinOpponents + PageSize - 1, values[^2]);
		}

		/// <summary>
		/// The "More..." entry moves on by a full page.
		/// </summary>
		[Fact]
		public void MoreEntryScrollsToNextPage()
		{
			_delegateUnderTest.ShowMenu();
			_host.SelectLastItem();
			_host.SelectLastItem();

			int[] values = _host.MenuValues();

			Assert.Equal(_rules.MinOpponents + PageSize, values[1]);
			Assert.Equal(1, _host.OpenMenu.ActiveItem);
		}

		/// <summary>
		/// The last page ends at the highest supported opponent count and offers no way further down.
		/// </summary>
		[Fact]
		public void LastPageEndsAtMaximumOpponents()
		{
			_delegateUnderTest.ShowMenu();
			Assert.True(_host.OpenMenu.OnCustomKeyDown!(new KeyboardEventArgs(Key.PageUp)));

			string[] texts = _host.MenuTexts();
			int[] values = _host.MenuValues();

			Assert.Equal("Back...", texts[0]);
			Assert.DoesNotContain("More...", texts);
			Assert.Equal(_rules.MaxOpponents, values[^1]);
			Assert.Equal(PageSize + 1, texts.Length);
		}

		/// <summary>
		/// Paging forward past the last page leaves the paged menu instead of getting stuck.
		/// </summary>
		[Fact]
		public void PagingPastLastPageReturnsToMainMenu()
		{
			_delegateUnderTest.ShowMenu();
			_host.OpenMenu.OnCustomKeyDown!(new KeyboardEventArgs(Key.PageUp));

			Assert.True(_host.OpenMenu.OnCustomKeyDown!(new KeyboardEventArgs(Key.PageDown)));

			Assert.Equal("Level of Competition...", _host.OpenMenu.Title);
		}

		/// <summary>
		/// Paging back from the first page leaves the paged menu as well.
		/// </summary>
		[Fact]
		public void PagingBeforeFirstPageReturnsToMainMenu()
		{
			_delegateUnderTest.ShowMenu();
			_host.SelectLastItem();

			Assert.True(_host.OpenMenu.OnCustomKeyDown!(new KeyboardEventArgs(Key.PageUp)));

			Assert.Equal("Level of Competition...", _host.OpenMenu.Title);
		}

		/// <summary>
		/// The "Back..." entry moves one page back while a previous page exists.
		/// </summary>
		[Fact]
		public void BackEntryScrollsToPreviousPage()
		{
			_delegateUnderTest.ShowMenu();
			_host.SelectLastItem();
			_host.SelectLastItem();

			_host.SelectItem(0);

			Assert.Equal("More Civilizations...", _host.OpenMenu.Title);
			Assert.Equal(_rules.MinOpponents, _host.MenuValues()[1]);
			Assert.Equal(1, _host.OpenMenu.ActiveItem);
		}

		/// <summary>
		/// The "Back..." entry returns to the first menu once the first page is reached.
		/// </summary>
		[Fact]
		public void BackEntryOnFirstPageReturnsToMainMenu()
		{
			_delegateUnderTest.ShowMenu();
			_host.SelectLastItem();

			_host.SelectItem(0);

			Assert.Equal("Level of Competition...", _host.OpenMenu.Title);
		}

		/// <summary>
		/// Reopening the extended menu from the first menu starts at the first page again.
		/// </summary>
		[Fact]
		public void ReopeningExtendedMenuStartsAtFirstPage()
		{
			_delegateUnderTest.ShowMenu();
			_host.SelectLastItem();
			_host.SelectLastItem();
			_host.SelectItem(0);
			_host.SelectItem(0);

			_host.SelectLastItem();

			Assert.Equal(_rules.MinOpponents, _host.MenuValues()[1]);
		}

		/// <summary>
		/// A picked count from the extended menu is reported like one from the first menu.
		/// </summary>
		[Fact]
		public void PickingOpponentCountFromExtendedMenuReportsSelection()
		{
			_delegateUnderTest.ShowMenu();
			_host.SelectLastItem();

			_host.SelectItem(1);

			Assert.Equal(_rules.MinOpponents, _selectedOpponents);
			Assert.Null(_host.CurrentMenu);
		}

		/// <summary>
		/// Cancelling the first menu reports back to the screen, which returns to the difficulty selection.
		/// </summary>
		[Fact]
		public void CancellingMainMenuReportsCancellation()
		{
			_delegateUnderTest.ShowMenu();

			_delegateUnderTest.CancelMenu();

			Assert.Equal(1, _cancelCount);
			Assert.Null(_host.CurrentMenu);
		}

		/// <summary>
		/// Cancelling the extended menu only steps back to the first menu.
		/// </summary>
		[Fact]
		public void CancellingExtendedMenuReturnsToMainMenu()
		{
			_delegateUnderTest.ShowMenu();
			_host.SelectLastItem();

			_delegateUnderTest.CancelExtendedMenu();

			Assert.Equal(0, _cancelCount);
			Assert.Equal("Level of Competition...", _host.OpenMenu.Title);
		}

		/// <summary>
		/// Releases the menus and the runtime this test class set up.
		/// </summary>
		public void Dispose()
		{
			_host.Dispose();
			_runtime.Dispose();
			RuntimeHandler.Wipe();
			GC.SuppressFinalize(this);
		}
	}
}
