using CivOne.Enums;

namespace CivOne.Leaders
{
	public class Sejong : BaseLeader
	{
		protected override Leader Leader => Leader.Sejong;

		public Sejong() : base("KING06", 40, 30)
		{
			Name = Translate("Sejong");
			DefaultName = Name;
			Aggression = AggressionLevel.Friendly;
			Development = DevelopmentLevel.Perfectionist;
		}
	}
}
