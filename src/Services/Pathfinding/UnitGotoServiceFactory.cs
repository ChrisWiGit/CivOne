namespace CivOne.Services.Pathfinding
{
	internal static class UnitGotoServiceFactory
	{
		// Factory method to create an instance of IUnitGotoService. Currently returns UnitGotoServiceImpl2, but can be extended to choose different implementations based on settings or other criteria.
		public static IUnitGotoService Create() => new UnitGotoServiceImpl2(Map.Instance, Settings.Instance.RiverFastMovement);
	}
}
