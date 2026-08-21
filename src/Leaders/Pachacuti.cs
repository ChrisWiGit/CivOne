using CivOne.Enums;

namespace CivOne.Leaders
{
	public class Pachacuti : BaseLeader
	{
		protected override Leader Leader => Leader.Pachacuti;

		public Pachacuti() : base("KING08", 40, 30)
		{
			Name = Translate("Pachacuti");
			DefaultName = Name;
			Development = DevelopmentLevel.Expansionistic;
			Militarism = MilitarismLevel.Civilized;
		}
	}
}
