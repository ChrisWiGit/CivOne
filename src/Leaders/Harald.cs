using CivOne.Enums;

namespace CivOne.Leaders
{
	public class Harald : BaseLeader
	{
		protected override Leader Leader => Leader.Harald;

		public Harald() : base("KING05", 40, 30)
		{
			Name = Translate("Harald");
			DefaultName = Name;
			Aggression = AggressionLevel.Aggressive;
			Militarism = MilitarismLevel.Militaristic;
		}
	}
}
