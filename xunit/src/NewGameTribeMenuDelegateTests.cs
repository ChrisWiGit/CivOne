using System;
using System.Linq;
using CivOne.Civilizations;
using CivOne.Events;
using CivOne.Screens.NewGamePanels;
using CivOne.Services;
using CivOne.src;
using Xunit;

namespace CivOne.UnitTests
{
	/// <summary>
	/// Covers the "Pick your tribe" menus: the split between the civilizations of the original game and
	/// the ones added by this project, the pages of the added ones, and the rename shortcut.
	///
	/// The menus are built against a stand-in host, so no screen is involved.
	/// </summary>
	public class NewGameTribeMenuDelegateTests : IDisposable
	{
		private const int ClassicTribeCount = 3;
		private const int ExtendedTribeCount = 15;
		private const int RandomTribeIndex = 2;

		private readonly MockRuntime _runtime;
		private readonly MockedNewGameMenuHost _host = new();
		private readonly NewGameRulesDelegate _rules = new([], () => false);

		private ICivilization[] _tribes;
		private int _selectedTribe = -1;
		private int _renameTribe = -1;
		private int _cancelCount;

		/// <summary>
		/// Sets up identity translation, a runtime for the menu canvas, and a tribe list with both
		/// classic and added civilizations.
		/// </summary>
		public NewGameTribeMenuDelegateTests()
		{
			TranslationServiceFactory.ResetForTests();
			_runtime = new MockRuntime(new RuntimeSettings { InitialSeed = 24601 });
			_tribes = CreateTribes(ClassicTribeCount, ExtendedTribeCount);
		}

		private static ICivilization[] CreateTribes(int classicCount, int extendedCount)
		{
			ICivilization[] classic = [.. Enumerable.Range(1, classicCount)
				.Select(id => new MockedICivilization(1, (byte)id))];
			ICivilization[] extended = [.. Enumerable.Range(0, extendedCount)
				.Select(offset => new MockedICivilization(1, (byte)(NewGameRulesDelegate.ClassicCivilizationMaxId + 1 + offset)))];

			return [.. classic, .. extended];
		}

		private NewGameTribeMenuDelegate CreateDelegate() => new(
			_host,
			_rules,
			() => _tribes,
			tribe => _selectedTribe = tribe,
			() => _cancelCount++,
			tribe => _renameTribe = tribe,
			randomService: new MockedIRandomService(RandomTribeIndex));

		private int PageSize => NewGameTribeMenuDelegate.ExtendedMenuPageSize;

		/// <summary>
		/// The first menu lists the civilizations of the original game and the way to the added ones.
		/// </summary>
		[Fact]
		public void MainMenuListsClassicTribesAndTheWayToTheAddedOnes()
		{
			CreateDelegate().ShowMenu();

			string[] texts = _host.MenuTexts();
			int[] values = _host.MenuValues();

			Assert.Equal("Pick your tribe...", _host.OpenMenu.Title);
			Assert.Equal(NewGameTribeMenuDelegate.MenuYOffset, _host.LastYOffset);
			Assert.Equal(ClassicTribeCount + 1, texts.Length);
			Assert.Equal(_tribes[0].Name, texts[0]);
			Assert.Equal("New Civilizations...", texts[^1]);
			Assert.Equal([0, 1, 2], values.Take(ClassicTribeCount));
		}

		/// <summary>
		/// Without added civilizations the first menu shows no entry leading to them.
		/// </summary>
		[Fact]
		public void MainMenuHidesTheWayToAddedTribesWhenThereAreNone()
		{
			_tribes = CreateTribes(ClassicTribeCount, 0);

			CreateDelegate().ShowMenu();

			Assert.Equal(ClassicTribeCount, _host.MenuTexts().Length);
			Assert.DoesNotContain("New Civilizations...", _host.MenuTexts());
		}

		/// <summary>
		/// Picking a civilization closes the menu and hands its index to the screen.
		/// </summary>
		[Fact]
		public void PickingTribeReportsItsIndex()
		{
			CreateDelegate().ShowMenu();

			_host.SelectItem(1);

			Assert.Equal(1, _selectedTribe);
			Assert.Equal(1, _host.CloseCount);
			Assert.Null(_host.CurrentMenu);
		}

		/// <summary>
		/// The extended menu shows one page of the added civilizations, with the way back on top.
		/// </summary>
		[Fact]
		public void ExtendedMenuShowsFirstPageOfAddedTribes()
		{
			CreateDelegate().ShowMenu();
			_host.SelectLastItem();

			string[] texts = _host.MenuTexts();
			int[] values = _host.MenuValues();

			Assert.Equal("Pick new civilization...", _host.OpenMenu.Title);
			Assert.Equal(PageSize + 2, texts.Length);
			Assert.Equal("Original Civilizations...", texts[0]);
			Assert.Equal("Next civilizations...", texts[^1]);
			Assert.DoesNotContain("Previous civilizations...", texts);
			Assert.Equal(ClassicTribeCount, values[1]);
			Assert.Equal(_tribes[ClassicTribeCount].Name, texts[1]);
		}

		/// <summary>
		/// The last page is filled completely and offers the way back to the previous page.
		/// </summary>
		[Fact]
		public void NextPageShowsTheRemainingAddedTribes()
		{
			CreateDelegate().ShowMenu();
			_host.SelectLastItem();
			_host.SelectLastItem();

			string[] texts = _host.MenuTexts();
			int[] values = _host.MenuValues();

			Assert.Equal("Previous civilizations...", texts[1]);
			Assert.DoesNotContain("Next civilizations...", texts);
			Assert.Equal(ClassicTribeCount + ExtendedTribeCount - PageSize, values[2]);
			Assert.Equal(ClassicTribeCount + ExtendedTribeCount - 1, values[^1]);
		}

		/// <summary>
		/// Paging back returns to the first page of the added civilizations.
		/// </summary>
		[Fact]
		public void PreviousPageReturnsToFirstPage()
		{
			CreateDelegate().ShowMenu();
			_host.SelectLastItem();
			_host.SelectLastItem();

			_host.SelectItem(1);

			Assert.Equal(ClassicTribeCount, _host.MenuValues()[1]);
			Assert.DoesNotContain("Previous civilizations...", _host.MenuTexts());
		}

		/// <summary>
		/// The entry on top of the extended menu leads back to the civilizations of the original game.
		/// </summary>
		[Fact]
		public void OriginalCivilizationsEntryReturnsToMainMenu()
		{
			CreateDelegate().ShowMenu();
			_host.SelectLastItem();

			_host.SelectItem(0);

			Assert.Equal("Pick your tribe...", _host.OpenMenu.Title);
		}

		/// <summary>
		/// A civilization picked from the extended menu is reported with its index in the full tribe list.
		/// </summary>
		[Fact]
		public void PickingAddedTribeReportsItsIndex()
		{
			CreateDelegate().ShowMenu();
			_host.SelectLastItem();

			_host.SelectItem(1);

			Assert.Equal(ClassicTribeCount, _selectedTribe);
			Assert.Null(_host.CurrentMenu);
		}

		/// <summary>
		/// The rename shortcut reports the highlighted civilization and closes the menu for the input box.
		/// </summary>
		[Fact]
		public void RenameShortcutReportsHighlightedTribe()
		{
			CreateDelegate().ShowMenu();
			_host.OpenMenu.ActiveItem = 1;

			Assert.True(_host.OpenMenu.OnCustomKeyDown!(new KeyboardEventArgs('R')));

			Assert.Equal(1, _renameTribe);
			Assert.Equal(1, _host.CloseCount);
		}

		/// <summary>
		/// With a navigation entry highlighted there is no civilization to rename, so a random one is used.
		/// </summary>
		[Fact]
		public void RenameShortcutFallsBackToRandomTribe()
		{
			CreateDelegate().ShowMenu();
			_host.OpenMenu.ActiveItem = ClassicTribeCount;

			Assert.True(_host.OpenMenu.OnCustomKeyDown!(new KeyboardEventArgs('r')));

			Assert.Equal(RandomTribeIndex, _renameTribe);
		}

		/// <summary>
		/// Other keys are left to the menu itself.
		/// </summary>
		[Fact]
		public void OtherKeysAreNotHandled()
		{
			CreateDelegate().ShowMenu();

			Assert.False(_host.OpenMenu.OnCustomKeyDown!(new KeyboardEventArgs('x')));
			Assert.Equal(-1, _renameTribe);
		}

		/// <summary>
		/// Cancelling the first menu reports back to the screen, which returns to the competition selection.
		/// </summary>
		[Fact]
		public void CancellingMainMenuReportsCancellation()
		{
			NewGameTribeMenuDelegate tribeMenu = CreateDelegate();
			tribeMenu.ShowMenu();

			tribeMenu.CancelMenu();

			Assert.Equal(1, _cancelCount);
			Assert.Null(_host.CurrentMenu);
		}

		/// <summary>
		/// Cancelling the extended menu only steps back to the civilizations of the original game.
		/// </summary>
		[Fact]
		public void CancellingExtendedMenuReturnsToMainMenu()
		{
			NewGameTribeMenuDelegate tribeMenu = CreateDelegate();
			tribeMenu.ShowMenu();
			_host.SelectLastItem();

			tribeMenu.CancelExtendedMenu();

			Assert.Equal(0, _cancelCount);
			Assert.Equal("Pick your tribe...", _host.OpenMenu.Title);
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
