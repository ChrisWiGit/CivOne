using CivOne.Enums;

namespace CivOne.Leaders
{
	public class Tokugawa : BaseLeader
	{
		protected override Leader Leader => Leader.Tokugawa;

		public Tokugawa() : base("KING00", 40, 30)
		{
			Name = Translate("Tokugawa");
			DefaultName = Name;
			Development = DevelopmentLevel.Perfectionist;
			Militarism = MilitarismLevel.Militaristic;
		}
	}
}
