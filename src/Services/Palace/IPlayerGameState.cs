namespace CivOne.Services.Palace
{
	public interface IPlayerGameState
	{
		int CivilizationScore { get; }
		IPalaceData Palace { get; }
		bool IsHuman { get; }
	}
}