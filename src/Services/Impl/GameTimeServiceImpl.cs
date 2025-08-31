using System;

namespace CivOne.Services.Impl
{
	public class GameTimeServiceImpl : IOriginalGameTime
	{
		public ushort YearToTurn(int year)
		{
			if (year < -4000) return 0;
			if (year < 1000) return (ushort)Math.Floor(((double)year + 4000) / 20);
			if (year < 1500) return (ushort)Math.Floor(((double)year + 1500) / 10);
			if (year < 1750) return (ushort)Math.Floor(((double)year) / 5);
			if (year < 1850) return (ushort)Math.Floor(((double)year - 1050) / 2);
			return (ushort)(year - 1450);
		}
		public int TurnToYear(ushort turn)
		{
			if (turn < 200) return -(200 - turn) * 20;
			else if (turn == 200) return 1;
			else if (turn < 250) return (turn - 200) * 20;
			else if (turn < 300) return ((turn - 250) * 10) + 1000;
			else if (turn < 350) return ((turn - 300) * 5) + 1500;
			else if (turn < 400) return ((turn - 350) * 2) + 1750;
			return (turn - 400) + 1850;
		}
		public string YearString(ushort turn, bool zeroAd = false)
		{
			int year = TurnToYear(turn);
			if (zeroAd && year == 1) year = 0;
			if (year < 0)
				return $"{-year} BC";
			return $"{year} AD";
		}
	}
}