using CivOne.Enums;

namespace CivOne.Leaders
{
	public class Hannibal : BaseLeader
	{
		protected override Leader Leader => Leader.Hannibal;

		public Hannibal() : base("KING09", 40, 30)
		{
			Name = Translate("Hannibal");
			DefaultName = Name;
			Aggression = AggressionLevel.Aggressive;
			Militarism = MilitarismLevel.Militaristic;
		}
	}
}
