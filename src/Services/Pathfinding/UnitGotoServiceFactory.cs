namespace CivOne.Services.Pathfinding
{
	internal static class UnitGotoServiceFactory
	{
		public static IUnitGotoService Create() => new UnitGotoServiceImpl(Map.Instance);
	}
}
