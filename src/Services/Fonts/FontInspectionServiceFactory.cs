namespace CivOne.Services.Fonts
{
	/// <summary>
	/// Creates <see cref="IFontInspectionService"/> instances.
	/// </summary>
	internal static class FontInspectionServiceFactory
	{
		/// <summary>
		/// Creates a service that inspects the fonts of the current resource instance.
		/// </summary>
		/// <returns>A new <see cref="IFontInspectionService"/>.</returns>
		public static IFontInspectionService CreateDefault() => new FontInspectionService();
	}
}
