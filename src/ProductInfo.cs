namespace CivOne
{
	/// <summary>
	/// The name the game shows to the player.
	/// <br/>
	/// Everything the player reads as the name of the game takes it from here, so a later rename is a
	/// single change. Paths, file extensions and the assembly names are deliberately not covered:
	/// they identify stored data of existing installations and have to stay as they are.
	/// </summary>
	internal static class ProductInfo
	{
		/// <summary>
		/// The current name of the game.
		/// </summary>
		public const string Name = "CivOneX";

		/// <summary>
		/// The name the game was released under before it was renamed.
		/// <br/>
		/// Profiles written by an older version still hold this name wherever the name was stored as
		/// a value, so it is needed to tell a stale default from a title the player chose.
		/// </summary>
		public const string LegacyName = "CivOne";
	}
}
