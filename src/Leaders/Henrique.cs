using CivOne.Enums;

namespace CivOne.Leaders
{
	public class Henrique : BaseLeader
	{
		protected override Leader Leader => Leader.Henrique;

		public Henrique() : base("KING04", 40, 30)
		{
			Name = Translate("Henrique");
			DefaultName = Name;
			Aggression = AggressionLevel.Friendly;
			Development = DevelopmentLevel.Expansionistic;
		}
	}
}
