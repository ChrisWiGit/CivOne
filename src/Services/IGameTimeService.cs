namespace CivOne.Services
{
	public interface IGameTimeService<Year, Turn>
		where Year : struct
		where Turn : struct
	{
		Turn YearToTurn(Year year);
		Year TurnToYear(Turn turn);
		string YearString(Turn turn, bool zeroAd = false);
	}

	public interface IOriginalGameTime : IGameTimeService<int, ushort> { }
}

