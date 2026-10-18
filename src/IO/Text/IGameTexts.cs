namespace CivOne.IO.Text
{
	/// <summary>
	/// Read-only access to parsed Civilization text entries.
	/// Keys use file/marker format, for example ERROR/DEMOCRACY.
	/// </summary>
	internal interface IGameTexts
	{
		/// <summary>
		/// Returns text lines for a single game text key.
		/// </summary>
		/// <param name="key">
		/// Composite key in format FILE/MARKER.
		/// Example: HELP/HELP1.
		/// </param>
		/// <returns>
		/// Matching lines.
		/// Returns an empty array when key is unknown.
		/// </returns>
		string[] GetGameText(string key);
	}
}