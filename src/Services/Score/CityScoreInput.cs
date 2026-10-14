namespace CivOne.Services.Score
{
	/// <summary>
	/// Citizen state of one city as the civilization score reads it, measured at the 40 % luxury reference.
	/// </summary>
	/// <param name="Size">City size, specialists included.</param>
	/// <param name="Happy">Number of happy citizens.</param>
	/// <param name="Unhappy">Number of unhappy citizens.</param>
	/// <param name="RedShirt">Number of red-shirt citizens; CivOneX counts them as unhappy.</param>
	public readonly record struct CityScoreInput(int Size, int Happy, int Unhappy, int RedShirt);
}
