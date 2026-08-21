using CivOne.Enums;

namespace CivOne.Leaders
{
	public class Pacal : BaseLeader
	{
		protected override Leader Leader => Leader.Pacal;

		public Pacal() : base("KING07", 40, 30)
		{
			Name = Translate("Pacal");
			DefaultName = Name;
			Development = DevelopmentLevel.Perfectionist;
		}
	}
}
