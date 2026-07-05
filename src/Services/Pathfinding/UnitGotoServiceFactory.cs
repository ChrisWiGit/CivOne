namespace CivOne.Services.Pathfinding
{
	internal static class UnitGotoServiceFactory
	{
		/// <summary>
		/// Factory method to create an instance of IUnitGotoService. Currently returns UnitGotoServiceImpl2, but can be extended to choose different implementations based on settings or other criteria.
		/// The original implementation of the A* pathfinding for unit GoTo orders is available in UnitGotoServiceImpl, and can be switched to by changing this factory method if needed for testing or comparison purposes.
		/// </summary>
		/// <returns>An instance of IUnitGotoService, currently UnitGotoServiceImpl2.</returns>
		public static IUnitGotoService Create() => new UnitGotoService2(Map.Instance, Settings.Instance.RiverFastMovement);
	}
}
