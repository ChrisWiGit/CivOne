namespace CivOne.Enums
{
	/// <summary>
	/// Tells which palace the Historian shows for an AI player.
	/// The human player always shows the real palace.
	/// </summary>
	public enum HistorianAiPalace
	{
		/// <summary>
		/// A palace generated from the AI player's cities. This matches the original game.
		/// </summary>
		Generated = 0,

		/// <summary>
		/// The AI player's built palace when it has any upgrade, otherwise a generated one.
		/// </summary>
		BuiltOrGenerated = 1,

		/// <summary>
		/// Only the AI player's built palace; an unbuilt palace shows nothing.
		/// </summary>
		BuiltOnly = 2,
	}
}
