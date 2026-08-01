namespace CivOne.Services.Sorting
{
	/// <summary>
	/// Creates <see cref="INaturalSortService"/> instances.
	/// </summary>
	public static class NaturalSortServiceFactory
	{
		/// <summary>
		/// Creates a new <see cref="INaturalSortService"/>.
		/// </summary>
		public static INaturalSortService Create() => new NaturalSortService();
	}
}
