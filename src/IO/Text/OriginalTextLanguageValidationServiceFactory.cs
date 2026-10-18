namespace CivOne.IO.Text
{
	/// <summary>
	/// Factory for original text language validation service.
	/// </summary>
	internal static class OriginalTextLanguageValidationServiceFactory
	{
		/// <summary>
		/// Creates a new validation service instance.
		/// </summary>
		public static IOriginalTextLanguageValidationService Create()
		{
			return new OriginalTextLanguageValidationService();
		}
	}
}