using System.Diagnostics.CodeAnalysis;

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
		[SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", Justification = "We want to use string for simplicity, and we validate the URL format in the implementation.")]
		bool TryOpenUrl(string url, out string? errorMessage);

		/// <summary>
		/// Attempts to copy text to the system clipboard.
		/// </summary>
		/// <param name="text">The text to copy.</param>
		/// <param name="errorMessage">Error message if copy failed; empty string on success.</param>
		/// <returns>True if text was copied; false otherwise.</returns>
		bool TryCopyToClipboard(string text, out string? errorMessage);
	}
}
