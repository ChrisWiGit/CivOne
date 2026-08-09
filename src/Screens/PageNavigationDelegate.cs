using System;

namespace CivOne.Screens
{
	/// <summary>
	/// Keeps track of the page a screen is currently showing when a list is too long to be drawn at once.
	/// </summary>
	/// <remarks>
	/// Paging wraps around: moving past the last page continues at the first one and the other way round.
	/// </remarks>
	internal sealed class PageNavigationDelegate
	{
		private int _itemCount;
		private int _pageSize = 1;

		/// <summary>
		/// The page that is currently shown, counted from zero.
		/// </summary>
		public int CurrentPage { get; private set; }

		/// <summary>
		/// The number of pages the list is split into, at least one.
		/// </summary>
		public int PageCount => _itemCount <= 0 ? 1 : ((_itemCount - 1) / _pageSize) + 1;

		/// <summary>
		/// The number of items on a single page.
		/// </summary>
		public int PageSize => _pageSize;

		/// <summary>
		/// The index of the first item of the current page.
		/// </summary>
		public int FirstItemIndex => CurrentPage * _pageSize;

		/// <summary>
		/// Describes the list to page through.
		/// </summary>
		/// <remarks>
		/// The current page is kept as long as it still exists, so redrawing the same list does not jump back
		/// to the first page.
		/// A list that has become shorter, for example because a civilization was destroyed, falls back to the
		/// first page.
		/// </remarks>
		/// <param name="itemCount">The number of items in the list.</param>
		/// <param name="pageSize">The number of items that fit on a page, at least one.</param>
		public void SetItems(int itemCount, int pageSize)
		{
			_itemCount = Math.Max(0, itemCount);
			_pageSize = Math.Max(1, pageSize);

			if (CurrentPage >= PageCount)
			{
				CurrentPage = 0;
			}
		}

		/// <summary>
		/// Returns to the first page, used when the list itself changed.
		/// </summary>
		public void First() => CurrentPage = 0;

		/// <summary>
		/// Moves to the next page, continuing at the first page after the last one.
		/// </summary>
		public void Next() => CurrentPage = (CurrentPage + 1) % PageCount;

		/// <summary>
		/// Moves to the previous page, continuing at the last page before the first one.
		/// </summary>
		public void Previous() => CurrentPage = (CurrentPage + PageCount - 1) % PageCount;
	}
}
