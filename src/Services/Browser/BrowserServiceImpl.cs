using System;

namespace CivOne.Services.Browser
{
	/// <summary>
	/// Implementation of browser service using platform-specific runtime methods.
	/// </summary>
	internal sealed class BrowserServiceImpl : IBrowserService
	{
		public bool TryOpenUrl(string url, out string? errorMessage)
		{
			if (string.IsNullOrWhiteSpace(url))
			{
				errorMessage = "URL is empty.";
				return false;
			}

			if (!IsValidHttpUrl(url))
			{
				errorMessage = "URL must be http or https.";
				return false;
			}

			try
			{
				return RuntimeHandler.Runtime.TryOpenUrl(url, out errorMessage);
			}
			catch (Exception ex)
			{
				errorMessage = $"Error: {ex.Message}";
				return false;
			}
		}

		public bool TryCopyToClipboard(string url, out string? errorMessage)
		{
			if (string.IsNullOrWhiteSpace(url))
			{
				errorMessage = "URL is empty.";
				return false;
			}

			try
			{
				return RuntimeHandler.Runtime.TryCopyToClipboard(url, out errorMessage);
			}
			catch (Exception ex)
			{
				errorMessage = $"Error: {ex.Message}";
				return false;
			}
		}

		private static bool IsValidHttpUrl(string url)
		{
			if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
				return false;
			return uri.Scheme == "http" || uri.Scheme == "https";
		}
	}
}
