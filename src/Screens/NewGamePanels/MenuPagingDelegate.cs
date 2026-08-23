using System;

namespace CivOne.Screens.NewGamePanels
{
	/// <summary>
	/// Keeps the scroll position of a menu that shows a long list in pages of a fixed size.
	/// The class only holds the position and does the page arithmetic, it never builds a menu.
	/// </summary>
	/// <example>
	/// <code>
	/// MenuPagingDelegate paging = new(pageSize: 11);
	/// paging.ClampOffset(entries.Length);
	/// for (int i = paging.PageStart; i &lt; paging.PageEndExclusive(entries.Length); i++)
	/// {
	///     menu.Items.Add(entries[i], i);
	/// }
	/// </code>
	/// </example>
	internal class MenuPagingDelegate
	{
		/// <summary>
		/// Creates the paging state for a menu.
		/// </summary>
		/// <param name="pageSize">Number of list entries shown on a single page. Must be greater than zero.</param>
		public MenuPagingDelegate(int pageSize)
		{
			ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);

			PageSize = pageSize;
		}

		/// <summary>
		/// Number of list entries shown on a single page.
		/// </summary>
		public int PageSize { get; }

		/// <summary>
		/// Index of the first list entry shown on the current page.
		/// </summary>
		public int Offset { get; private set; }

		/// <summary>
		/// Index of the first list entry shown on the current page.
		/// </summary>
		public int PageStart => Offset;

		/// <summary>
		/// Tells whether the current page is the first one.
		/// </summary>
		public bool IsFirstPage => Offset == 0;

		/// <summary>
		/// Moves back to the first page.
		/// </summary>
		public virtual void Reset()
		{
			Offset = 0;
		}

		/// <summary>
		/// Returns the highest offset that still fills a page.
		/// </summary>
		/// <param name="totalEntries">Total number of list entries.</param>
		/// <returns>The highest valid offset, never below zero.</returns>
		public virtual int MaxOffset(int totalEntries)
		{
			return Math.Max(0, totalEntries - PageSize);
		}

		/// <summary>
		/// Tells whether the list is long enough to need more than one page.
		/// </summary>
		/// <param name="totalEntries">Total number of list entries.</param>
		/// <returns><see langword="true"/> when the list does not fit on a single page.</returns>
		public virtual bool RequiresPaging(int totalEntries)
		{
			return totalEntries > PageSize;
		}

		/// <summary>
		/// Tells whether the current page is the last one.
		/// </summary>
		/// <param name="totalEntries">Total number of list entries.</param>
		/// <returns><see langword="true"/> when no further page follows.</returns>
		public virtual bool IsLastPage(int totalEntries)
		{
			return Offset >= MaxOffset(totalEntries);
		}

		/// <summary>
		/// Pulls the offset back into the valid range, for example after the list has shrunk.
		/// </summary>
		/// <param name="totalEntries">Total number of list entries.</param>
		/// <returns>The corrected offset.</returns>
		public virtual int ClampOffset(int totalEntries)
		{
			Offset = Math.Clamp(Offset, 0, MaxOffset(totalEntries));
			return Offset;
		}

		/// <summary>
		/// Returns the index behind the last list entry shown on the current page.
		/// </summary>
		/// <param name="totalEntries">Total number of list entries.</param>
		/// <returns>The exclusive end index of the current page.</returns>
		public virtual int PageEndExclusive(int totalEntries)
		{
			return Math.Min(totalEntries, Offset + PageSize);
		}

		/// <summary>
		/// Moves one page forward, stopping on the last page.
		/// </summary>
		/// <param name="totalEntries">Total number of list entries.</param>
		/// <returns><see langword="false"/> when the last page was already reached.</returns>
		public virtual bool NextPage(int totalEntries)
		{
			if (IsLastPage(totalEntries))
			{
				return false;
			}

			Offset = Math.Min(MaxOffset(totalEntries), Offset + PageSize);
			return true;
		}

		/// <summary>
		/// Moves one page back, stopping on the first page.
		/// </summary>
		/// <returns><see langword="false"/> when the first page was already reached.</returns>
		public virtual bool PreviousPage()
		{
			if (IsFirstPage)
			{
				return false;
			}

			Offset = Math.Max(0, Offset - PageSize);
			return true;
		}

		/// <summary>
		/// Jumps to the last page.
		/// </summary>
		/// <param name="totalEntries">Total number of list entries.</param>
		public virtual void MoveToLastPage(int totalEntries)
		{
			Offset = MaxOffset(totalEntries);
		}
	}
}
