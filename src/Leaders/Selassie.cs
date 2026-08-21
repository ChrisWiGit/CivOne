using CivOne.Enums;

namespace CivOne.Leaders
{
	public class Selassie : BaseLeader
	{
		protected override Leader Leader => Leader.Selassie;

		public Selassie() : base("KING13", 40, 30)
		{
			Name = Translate("Selassie");
			DefaultName = Name;
		}
	}
}
