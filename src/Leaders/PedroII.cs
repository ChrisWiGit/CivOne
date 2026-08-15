using CivOne.Enums;

namespace CivOne.Leaders
{
	public class PedroII : BaseLeader
	{
		protected override Leader Leader => Leader.PedroII;

		public PedroII() : base("KING02", 40, 30)
		{
			Name = Translate("Pedro II");
			DefaultName = Name;
			Aggression = AggressionLevel.Friendly;
			Development = DevelopmentLevel.Expansionistic;
		}
	}
}
