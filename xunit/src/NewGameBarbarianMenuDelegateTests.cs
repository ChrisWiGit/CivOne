using System;
using CivOne.Enums;
using CivOne.Screens.NewGamePanels;
using CivOne.Services;
using CivOne.src;
using Xunit;

namespace CivOne.UnitTests
{
	/// <summary>
	/// Covers the barbarian menu reached from the competition menu: the offered sources, the
	/// preselection of the current value, and what a selection writes back.
	///
	/// The setting is injected, so no global settings file is touched.
	/// </summary>
	public class NewGameBarbarianMenuDelegateTests : IDisposable
	{
		private readonly MockRuntime _runtime;
		private readonly MockedNewGameMenuHost _host = new();
		private readonly NewGameBarbarianMenuDelegate _delegateUnderTest;

		private BarbarianActivity _activity = BarbarianActivity.VillagesAndRaids;
		private int _returnCount;

		/// <summary>
		/// Sets up identity translation and a runtime, which menus need for their canvas size.
		/// </summary>
		public NewGameBarbarianMenuDelegateTests()
		{
			TranslationServiceFactory.ResetForTests();
			_runtime = new MockRuntime(new RuntimeSettings { InitialSeed = 24601 });
			_delegateUnderTest = new NewGameBarbarianMenuDelegate(
				_host,
				() => _returnCount++,
				() => _activity,
				value => _activity = value);
		}

		/// <summary>
		/// The menu offers all four barbarian sources, in a fixed order.
		/// </summary>
		[Fact]
		public void MenuOffersAllBarbarianSources()
		{
			_delegateUnderTest.ShowMenu();

			string[] texts = _host.MenuTexts();
			int[] values = _host.MenuValues();

			Assert.Equal("Barbarians...", _host.OpenMenu.Title);
			Assert.Equal(8, texts.Length);
			Assert.Equal("None", texts[0]);
			Assert.Equal("Villages Only", texts[1]);
			Assert.Equal("Land Raids Only", texts[2]);
			Assert.Equal("Sea Raids Only", texts[3]);
			Assert.Equal("Raids Only", texts[4]);
			Assert.Equal("Villages + Land", texts[5]);
			Assert.Equal("Villages + Sea", texts[6]);
			Assert.Equal("Villages + Raids", texts[7]);
			Assert.Equal((int)BarbarianActivity.SeaRaids, values[3]);
			Assert.Equal((int)BarbarianActivity.VillagesAndRaids, values[7]);
		}

		/// <summary>
		/// The hint line tells the player that the choice also changes the global setting.
		/// </summary>
		[Fact]
		public void MenuHintsAtTheGlobalSetting()
		{
			_delegateUnderTest.ShowMenu();

			string[] expectedHints = ["Esc: Back", "Changes the global setting"];
			Assert.Equal(expectedHints, _host.OpenMenu.Hints);
		}

		/// <summary>
		/// Opening the menu highlights the value currently in use.
		/// </summary>
		[Fact]
		public void CurrentValueIsPreselected()
		{
			_activity = BarbarianActivity.Villages;

			_delegateUnderTest.ShowMenu();

			Assert.Equal(1, _host.OpenMenu.ActiveItem);
		}

		/// <summary>
		/// Picking a source stores it and returns to the competition menu.
		/// </summary>
		[Fact]
		public void PickingSourceStoresItAndReturns()
		{
			_delegateUnderTest.ShowMenu();

			_host.SelectItem(0);

			Assert.Equal(BarbarianActivity.None, _activity);
			Assert.Equal(1, _returnCount);
			Assert.Equal(1, _host.CloseCount);
		}

		/// <summary>
		/// The entry text of the competition menu carries the current value.
		/// </summary>
		[Fact]
		public void MenuEntryTextShowsCurrentValue()
		{
			_activity = BarbarianActivity.Raids;

			Assert.Equal("Barbarians: Raids", _delegateUnderTest.MenuEntryText);
		}

		/// <summary>
		/// Land and sea raiders are separate flags, so a setting outside the four menu presets still has
		/// a text of its own instead of falling back to the unknown label.
		/// </summary>
		[Theory]
		[InlineData(BarbarianActivity.LandRaids, "Barbarians: Land")]
		[InlineData(BarbarianActivity.SeaRaids, "Barbarians: Sea")]
		[InlineData(BarbarianActivity.Villages | BarbarianActivity.SeaRaids, "Barbarians: Vil.+Sea")]
		[InlineData(BarbarianActivity.Villages | BarbarianActivity.LandRaids, "Barbarians: Vil.+Land")]
		public void SeparateRaidKindsHaveTheirOwnText(BarbarianActivity activity, string expectedText)
		{
			_activity = activity;

			Assert.Equal(expectedText, _delegateUnderTest.MenuEntryText);
		}

		/// <summary>
		/// Land and sea raiders can be picked on their own, without the other kind coming along.
		/// </summary>
		[Fact]
		public void PickingASingleRaidKindStoresOnlyThatFlag()
		{
			_delegateUnderTest.ShowMenu();

			_host.SelectItem(3);

			Assert.Equal(BarbarianActivity.SeaRaids, _activity);
		}

		/// <summary>
		/// A combination is highlighted just like a single flag.
		/// </summary>
		[Fact]
		public void CombinedValueIsPreselected()
		{
			_activity = BarbarianActivity.Villages | BarbarianActivity.SeaRaids;

			_delegateUnderTest.ShowMenu();

			Assert.Equal(6, _host.OpenMenu.ActiveItem);
		}

		/// <summary>
		/// Leaving the menu keeps the setting untouched.
		/// </summary>
		[Fact]
		public void CancellingKeepsTheSetting()
		{
			_delegateUnderTest.ShowMenu();

			_delegateUnderTest.CancelMenu();

			Assert.Equal(BarbarianActivity.VillagesAndRaids, _activity);
			Assert.Equal(1, _returnCount);
		}

		/// <summary>
		/// Releases the menus and the runtime this test class set up.
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				_host.Dispose();
				_runtime.Dispose();
				RuntimeHandler.Wipe();
			}
		}
	}
}
