using CivOne.Enums;

namespace CivOne.Leaders
{
	public class MansaMusa : BaseLeader
	{
		protected override Leader Leader => Leader.MansaMusa;

		public MansaMusa() : base("KING12", 40, 30)
		{
			Name = Translate("Mansa Musa");
			DefaultName = Name;
			Aggression = AggressionLevel.Friendly;
			Development = DevelopmentLevel.Expansionistic;
		}
	}
}
