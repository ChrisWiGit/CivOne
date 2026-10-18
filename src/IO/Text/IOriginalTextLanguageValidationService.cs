namespace CivOne.IO.Text
{
	/// <summary>
	/// Validates whether copied original Civilization text files match a known English baseline.
	/// </summary>
	internal interface IOriginalTextLanguageValidationService
	{
		/// <summary>
		/// Validates fallback text files in the given data directory.
		/// </summary>
		/// <param name="dataDirectory">
		/// Directory that contains the copied original text files.
		/// </param>
		/// <returns>
		/// Validation result with per-file counters and overall state.
		/// </returns>
		OriginalTextLanguageValidationResult Validate(string dataDirectory);
	}
}