using CivOne.Enums;

namespace CivOne
{
	/// <summary>
	/// Carries all parameters required to generate one map.
	/// </summary>
	/// <param name="Width">
	/// Gets the map width in tiles.
	/// </param>
	/// <param name="Height">
	/// Gets the map height in tiles.
	/// </param>
	/// <param name="LandMass">
	/// Gets the land mass preset controlling how much of the map is covered by land.
	/// </param>
	/// <param name="Temperature">
	/// Gets the temperature preset controlling biome distribution from pole to equator.
	/// </param>
	/// <param name="Climate">
	/// Gets the climate preset controlling overall moisture and rainfall.
	/// </param>
	/// <param name="Age">
	/// Gets the earth age preset controlling terrain roughness and mountain density.
	/// </param>
	public sealed record MapGenerationParameters(
		int Width,
		int Height,
		LandMass LandMass = LandMass.Normal,
		Temperature Temperature = Temperature.Temperate,
		Climate Climate = Climate.Normal,
		EarthAge Age = EarthAge.FourBillionYears);
}
