using CivOne.Enums;

namespace CivOne.Leaders
{
	public class Corvinus : BaseLeader
	{
		protected override Leader Leader => Leader.Corvinus;

		public Corvinus() : base("KING01", 40, 30)
		{
			Name = Translate("Corvinus");
			DefaultName = Name;
			Aggression = AggressionLevel.Aggressive;
			Militarism = MilitarismLevel.Militaristic;
		}
	}
}
