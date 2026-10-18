namespace CivOne.Enums
{
	/// <summary>
	/// Specifies the screen corner where the FPS overlay is displayed, or Off to disable it.
	/// </summary>
	public enum FpsCorner
	{
		/// <summary>FPS display is disabled.</summary>
		Off = 0,
		/// <summary>FPS display in the top-left corner.</summary>
		TopLeft = 1,
		/// <summary>FPS display in the top-right corner.</summary>
		TopRight = 2,
		/// <summary>FPS display in the bottom-left corner.</summary>
		BottomLeft = 3,
		/// <summary>FPS display in the bottom-right corner.</summary>
		BottomRight = 4
	}
}