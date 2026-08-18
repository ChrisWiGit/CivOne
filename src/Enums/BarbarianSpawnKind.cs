namespace CivOne.Enums
{
	/// <summary>
	/// The kind of barbarian raiding party that may appear in a single turn.
	/// </summary>
	public enum BarbarianSpawnKind
	{
		/// <summary>
		/// Nothing appears this turn.
		/// </summary>
		None = 0,

		/// <summary>
		/// A raiding party appears inland.
		/// </summary>
		Land = 1,

		/// <summary>
		/// A raiding party arrives by ship and lands on the coast.
		/// </summary>
		Sea = 2
	}
}
