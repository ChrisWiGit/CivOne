namespace CivOne.Enums
{
	/// <summary>
	/// The five single sprites a palace section is built from, as cut from the CASTLE pictures.
	/// </summary>
	public enum PalacePieceKind
	{
		/// <summary>The center building, 52 pixels wide.</summary>
		Center = 0,

		/// <summary>A wall half with a shadow on its left edge, 24 pixels wide.</summary>
		WallShadow = 1,

		/// <summary>A plain wall half, 24 pixels wide.</summary>
		Wall = 2,

		/// <summary>The left end tower.</summary>
		LeftTower = 3,

		/// <summary>The right end tower.</summary>
		RightTower = 4,
	}
}
