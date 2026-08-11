namespace CivOne.Civilizations
{
	/// <summary>
	/// The pairing of the 14 regular civilizations.
	/// The original game groups them into seven pairs that share a "preferred player number", so each
	/// civilization has exactly one partner: Romans (1) and Russians (8), Babylonians (2) and Zulus (9),
	/// and so on. A destroyed civilization is normally replaced by its partner, see
	/// <see cref="Player.Respawn"/>.
	/// </summary>
	internal static class BuddyPair
	{
		/// <summary>
		/// Distance between the two civilizations of a pair.
		/// </summary>
		public const int IdOffset = 7;

		/// <summary>
		/// Returns the Id of the other civilization in the same pair.
		/// </summary>
		/// <param name="civilizationId">The Id to find the partner for.</param>
		/// <returns>The partner's Id.</returns>
		public static int BuddyId(int civilizationId)
			=> civilizationId > IdOffset ? civilizationId - IdOffset : civilizationId + IdOffset;
	}
}
