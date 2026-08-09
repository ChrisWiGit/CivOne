using CivOne.Screens;
using Xunit;

namespace CivOne.UnitTests
{
	/// <summary>
	/// Tests for the page navigation used by screens that cannot draw a whole list at once.
	/// </summary>
	public class PageNavigationDelegateTests
	{
		private readonly PageNavigationDelegate _testee = new();

		[Theory]
		[InlineData(0, 6, 1)]
		[InlineData(6, 6, 1)]
		[InlineData(7, 6, 2)]
		[InlineData(31, 6, 6)]
		public void ThePageCountCoversEveryItem(int itemCount, int pageSize, int expected)
		{
			_testee.SetItems(itemCount, pageSize);

			Assert.Equal(expected, _testee.PageCount);
		}

		[Fact]
		public void TheFirstItemIndexFollowsTheCurrentPage()
		{
			_testee.SetItems(31, 6);

			_testee.Next();

			Assert.Equal(1, _testee.CurrentPage);
			Assert.Equal(6, _testee.FirstItemIndex);
		}

		[Fact]
		public void PagingForwardWrapsAtTheEndOfTheList()
		{
			_testee.SetItems(13, 6);

			_testee.Next();
			_testee.Next();
			Assert.Equal(2, _testee.CurrentPage);

			_testee.Next();
			Assert.Equal(0, _testee.CurrentPage);
		}

		[Fact]
		public void PagingBackwardWrapsAtTheStartOfTheList()
		{
			_testee.SetItems(13, 6);

			_testee.Previous();

			Assert.Equal(2, _testee.CurrentPage);
		}

		[Fact]
		public void ASinglePageStaysInPlace()
		{
			_testee.SetItems(4, 6);

			_testee.Next();
			_testee.Previous();

			Assert.Equal(0, _testee.CurrentPage);
		}

		[Fact]
		public void RedrawingTheSameListKeepsTheCurrentPage()
		{
			_testee.SetItems(13, 6);
			_testee.Next();

			_testee.SetItems(13, 6);

			Assert.Equal(1, _testee.CurrentPage);
		}

		[Fact]
		public void AReorderedListStartsOverAtTheFirstPage()
		{
			_testee.SetItems(13, 6);
			_testee.Next();

			_testee.First();

			Assert.Equal(0, _testee.CurrentPage);
		}

		[Fact]
		public void AShorterListFallsBackToTheFirstPage()
		{
			_testee.SetItems(13, 6);
			_testee.Next();
			_testee.Next();

			_testee.SetItems(7, 6);

			Assert.Equal(0, _testee.CurrentPage);
		}
	}
}
