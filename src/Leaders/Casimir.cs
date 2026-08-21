using CivOne.Enums;

namespace CivOne.Leaders
{
	public class Casimir : BaseLeader
	{
		protected override Leader Leader => Leader.Casimir;

		public Casimir() : base("KING00", 40, 30)
		{
			Name = Translate("Casimir");
			DefaultName = Name;
			Militarism = MilitarismLevel.Civilized;
		}
	}
}
