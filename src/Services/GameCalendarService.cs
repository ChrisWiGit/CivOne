// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

namespace CivOne.Services
{
	/// <summary>
	/// Converts between game turns and calendar years, and formats years for display.
	/// BC/AD labels are resolved through the translation service to support localization.
	/// </summary>
	public class GameCalendarService(ITranslationService _translationService = null)
	{
		private readonly ITranslationService _translation = _translationService ?? TranslationServiceFactory.GetCurrent();

		/// <summary>
		/// Converts a calendar year to the corresponding game turn.
		/// Years before -4000 clamp to turn 0.
		/// </summary>
		public ushort YearToTurn(int year)
		{
			if (year < -4000) return 0;
			if (year < 1000) return (ushort)System.Math.Floor(((double)year + 4000) / 20);
			if (year < 1500) return (ushort)System.Math.Floor(((double)year + 1500) / 10);
			if (year < 1750) return (ushort)System.Math.Floor(((double)year) / 5);
			if (year < 1850) return (ushort)System.Math.Floor(((double)year - 1050) / 2);
			return (ushort)(year - 1450);
		}

		/// <summary>
		/// Converts a game turn to the corresponding calendar year.
		/// Returns negative values for BC years.
		/// </summary>
		public int TurnToYear(ushort turn)
		{
			if (turn < 200) return -(200 - turn) * 20;
			if (turn == 200) return 1;
			if (turn < 250) return (turn - 200) * 20;
			if (turn < 300) return ((turn - 250) * 10) + 1000;
			if (turn < 350) return ((turn - 300) * 5) + 1500;
			if (turn < 400) return ((turn - 350) * 2) + 1750;
			return turn - 400 + 1850;
		}

		/// <summary>
		/// Formats a game turn as a human-readable year string, e.g. "4000 BC" or "1 AD".
		/// The era labels "BC" and "AD" are passed through the translation service.
		/// </summary>
		public string FormatYear(ushort turn, bool zeroAd = false)
		{
			int year = TurnToYear(turn);
			if (zeroAd && year == 1) year = 0;

			if (year < 0)
			{
				string bc = _translation.Translate("BC");
				return $"{-year} {bc}";
			}

			string ad = _translation.Translate("AD");
			return $"{year} {ad}";
		}

		public string FormatEra(ushort turn)
		{
			int year = TurnToYear(turn);
			if (year < 0)
			{
				return _translation.Translate("BC");
			}

			return _translation.Translate("AD");
		}
	}
}
