namespace CivOne.Services.Browser
{
	/// <summary>
	/// Factory for getting the browser service.
	/// </summary>
	public static class BrowserServiceFactory
	{
		private static IBrowserService? _instance;

		/// <summary>
		/// Gets the singleton browser service instance.
		/// </summary>
		public static IBrowserService Instance => _instance ??= new BrowserServiceImpl();
	}
}
