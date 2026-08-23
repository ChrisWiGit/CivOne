using CivOne.Enums;

namespace CivOne.Leaders
{
	public class Suleiman : BaseLeader
	{
		protected override Leader Leader => Leader.Suleiman;

		public Suleiman() : base("KING02", 40, 30)
		{
			Name = Translate("Suleiman");
			DefaultName = Name;
			Aggression = AggressionLevel.Aggressive;
			Development = DevelopmentLevel.Expansionistic;
			Militarism = MilitarismLevel.Militaristic;
		}
	}
}
