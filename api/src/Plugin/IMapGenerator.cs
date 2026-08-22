using CivOne.Enums;

namespace CivOne
{
	/// <summary>
	/// Represents one plugin-provided map generator instance.
	/// </summary>
	/// <remarks>
	/// Implement this interface to replace the built-in world generation algorithm.
	/// The engine calls <see cref="Generate"/> once per new game after the player
	/// has selected world settings. Return a 2-D terrain grid of the requested size;
	/// the engine applies huts, rivers and other post-processing on top.
	/// </remarks>
	public interface IMapGenerator
	{
		/// <summary>
		/// Generates a terrain grid from the supplied world-generation parameters.
		/// </summary>
		/// <param name="parameters">
		/// The world-generation settings chosen by the player, including map size and all presets.
		/// </param>
		/// <returns>
		/// A <c>Terrain[Width, Height]</c> grid where each cell holds the terrain type for that tile.
		/// The returned array must match <see cref="MapGenerationParameters.Width"/> and
		/// <see cref="MapGenerationParameters.Height"/> exactly.
		/// </returns>
		Terrain[,] Generate(MapGenerationParameters parameters);
	}
}
