namespace CivOne.Services.SpaceShip
{
	/// <summary>
	/// What happened to a civilization's spaceship program when its capital fell or the civilization was destroyed.
	/// </summary>
	public enum SpaceShipLoss
	{
		/// <summary>The civilization had no spaceship program.</summary>
		None,

		/// <summary>The ship was in flight and returns to Earth.</summary>
		ReturnsToEarth,

		/// <summary>The ship was still being built; its construction is cancelled.</summary>
		ConstructionCancelled,
	}
}
