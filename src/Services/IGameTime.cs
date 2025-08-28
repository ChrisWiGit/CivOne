namespace CivOne.Services
{
	public interface IGameTime<Year, Turn>
		where Year : struct
		where Turn : struct
	{
		Turn YearToTurn(Year year);
		Year TurnToYear(Turn turn);
		string YearString(Turn turn, bool zeroAd = false);
	}

	public interface IOriginalGameTime : IGameTime<int, ushort> { }
}

