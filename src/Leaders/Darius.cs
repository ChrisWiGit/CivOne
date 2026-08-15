using CivOne.Enums;

namespace CivOne.Leaders
{
	public class Darius : BaseLeader
	{
		protected override Leader Leader => Leader.Darius;

		public Darius() : base("KING01", 40, 30)
		{
			Name = Translate("Darius");
			DefaultName = Name;
			Development = DevelopmentLevel.Expansionistic;
			Militarism = MilitarismLevel.Civilized;
		}
	}
}
