using System.Diagnostics.CodeAnalysis;

namespace CivOne
{
	/// <summary>
	/// Decides which window title the game starts with.
	/// <br/>
	/// The title is a setting the player can change, so it is stored in the profile. A profile
	/// written before the game was renamed holds the old name as its title, which is not a choice the
	/// player made but the default of that version, and it would keep the old name on screen forever.
	/// Such a value is therefore replaced by the current name, while any other title is kept.
	/// </summary>
	internal sealed class WindowTitleDelegate
	{
		/// <summary>
		/// Returns the title to use for a value read from a profile.
		/// </summary>
		/// <param name="storedTitle">The title the profile holds, which may be empty.</param>
		/// <returns>The stored title, or the current name of the game.</returns>
		[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This class is a delegate, not a static utility.")]
		public string Resolve(string? storedTitle)
		{
			if (string.IsNullOrWhiteSpace(storedTitle) || storedTitle == ProductInfo.LegacyName)
			{
				return ProductInfo.Name;
			}
			return storedTitle;
		}
	}
}
