using CivOne.Enums;

namespace CivOne.Leaders
{
	public class Isabella : BaseLeader
	{
		protected override Leader Leader => Leader.Isabella;

		public Isabella() : base("KING03", 40, 30)
		{
			Name = Translate("Isabella");
			DefaultName = Name;
			Development = DevelopmentLevel.Expansionistic;
			Militarism = MilitarismLevel.Civilized;
		}
	}
}
