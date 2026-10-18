namespace CivOne.IO.Text
{
	/// <summary>
	/// Loads raw text resources by logical file name.
	/// Implementations resolve language-specific and fallback paths.
	/// </summary>
	internal interface ITextFileLoader
	{
		/// <summary>
		/// Loads a text resource as cleaned line array.
		/// </summary>
		/// <param name="filename">
		/// Logical file name without extension.
		/// Example: ERROR.
		/// </param>
		/// <returns>
		/// All lines from the resolved file.
		/// Returns an empty array when no file exists.
		/// </returns>
		string[] LoadArray(string filename);
	}
}