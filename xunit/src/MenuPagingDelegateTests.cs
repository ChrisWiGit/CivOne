using System;
using CivOne.Screens.NewGamePanels;
using Xunit;

namespace CivOne.UnitTests
{
	/// <summary>
	/// Covers the page arithmetic shared by the paged new game menus: page bounds, scrolling and
	/// correction of an offset that no longer fits the list.
	/// </summary>
	public class MenuPagingDelegateTests
	{
		private static MenuPagingDelegate CreatePaging(int pageSize = 5) => new(pageSize);

		/// <summary>
		/// A page size below one would produce a menu that can never show an entry.
		/// </summary>
		[Fact]
		public void PageSizeMustBePositive()
		{
			Assert.Throws<ArgumentOutOfRangeException>(() => new MenuPagingDelegate(0));
		}

		/// <summary>
		/// A list that fits on one page needs no navigation entries.
		/// </summary>
		[Fact]
		public void ShortListNeedsNoPaging()
		{
			MenuPagingDelegate paging = CreatePaging();

			Assert.False(paging.RequiresPaging(5));
			Assert.True(paging.RequiresPaging(6));
			Assert.Equal(0, paging.MaxOffset(5));
			Assert.True(paging.IsFirstPage);
			Assert.True(paging.IsLastPage(5));
		}

		/// <summary>
		/// The current page ends at the page size, or at the end of the list.
		/// </summary>
		[Fact]
		public void PageEndStopsAtListEnd()
		{
			MenuPagingDelegate paging = CreatePaging();

			Assert.Equal(5, paging.PageEndExclusive(12));
			paging.NextPage(12);
			Assert.Equal(10, paging.PageEndExclusive(12));
			paging.NextPage(12);
			Assert.Equal(12, paging.PageEndExclusive(12));
		}

		/// <summary>
		/// The last page starts so that it is still filled completely.
		/// </summary>
		[Fact]
		public void LastPageIsFilledCompletely()
		{
			MenuPagingDelegate paging = CreatePaging();

			paging.MoveToLastPage(12);

			Assert.Equal(7, paging.Offset);
			Assert.True(paging.IsLastPage(12));
			Assert.Equal(12, paging.PageEndExclusive(12));
		}

		/// <summary>
		/// Scrolling reports when it hits a border, so the caller can leave the paged menu instead.
		/// </summary>
		[Fact]
		public void ScrollingReportsListBorders()
		{
			MenuPagingDelegate paging = CreatePaging();

			Assert.False(paging.PreviousPage());
			Assert.True(paging.NextPage(12));
			Assert.Equal(5, paging.Offset);
			Assert.True(paging.NextPage(12));
			Assert.Equal(7, paging.Offset);
			Assert.False(paging.NextPage(12));
			Assert.Equal(7, paging.Offset);
			Assert.True(paging.PreviousPage());
			Assert.Equal(2, paging.Offset);
		}

		/// <summary>
		/// An offset kept from a longer list is pulled back when the list shrinks.
		/// </summary>
		[Fact]
		public void OffsetIsClampedToShrunkList()
		{
			MenuPagingDelegate paging = CreatePaging();
			paging.MoveToLastPage(30);
			Assert.Equal(25, paging.Offset);

			Assert.Equal(3, paging.ClampOffset(8));
			Assert.Equal(3, paging.Offset);

			Assert.Equal(0, paging.ClampOffset(4));
			Assert.True(paging.IsFirstPage);
		}

		/// <summary>
		/// Reset returns to the first page.
		/// </summary>
		[Fact]
		public void ResetReturnsToFirstPage()
		{
			MenuPagingDelegate paging = CreatePaging();
			paging.NextPage(12);

			paging.Reset();

			Assert.Equal(0, paging.Offset);
			Assert.True(paging.IsFirstPage);
		}
	}
}
