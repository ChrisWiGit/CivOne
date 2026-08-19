using CivOne.Enums;

namespace CivOne.Leaders
{
	public class Justinian : BaseLeader
	{
		protected override Leader Leader => Leader.Justinian;

		public Justinian() : base("KING10", 40, 30)
		{
			Name = Translate("Justinian");
			DefaultName = Name;
			Development = DevelopmentLevel.Perfectionist;
			Militarism = MilitarismLevel.Civilized;
		}
	}
}
