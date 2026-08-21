using CivOne.Enums;

namespace CivOne.Leaders
{
	public class Harun : BaseLeader
	{
		protected override Leader Leader => Leader.Harun;

		public Harun() : base("KING11", 40, 30)
		{
			Name = Translate("Harun");
			DefaultName = Name;
			Aggression = AggressionLevel.Friendly;
			Development = DevelopmentLevel.Perfectionist;
		}
	}
}
