namespace CivOne.Services.Browser
{
	/// <summary>
	/// Service for opening URLs in the system browser or copying them to clipboard.
	/// </summary>
	public interface IBrowserService
	{
		/// <summary>
		/// Attempts to open a URL in the system browser.
		/// </summary>
		/// <param name="url">The URL to open (must be http/https).</param>
		/// <param name="errorMessage">Error message if open failed; empty string on success.</param>
		/// <returns>True if URL was opened; false otherwise.</returns>
		bool TryOpenUrl(string url, out string errorMessage);

		/// <summary>
		/// Attempts to copy a URL to the system clipboard.
		/// </summary>
		/// <param name="url">The URL to copy.</param>
		/// <param name="errorMessage">Error message if copy failed; empty string on success.</param>
		/// <returns>True if URL was copied; false otherwise.</returns>
		bool TryCopyToClipboard(string url, out string errorMessage);
	}
}
