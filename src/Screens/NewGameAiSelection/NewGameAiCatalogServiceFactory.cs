// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

namespace CivOne.Screens
{
	/// <summary>
	/// Creates AI catalog services for the new game selection screen.
	/// </summary>
	internal static class NewGameAiCatalogServiceFactory
	{
		/// <summary>
		/// Creates the default AI catalog service.
		/// </summary>
		/// <returns>A new AI catalog service instance.</returns>
		public static INewGameAiCatalogService Create()
		{
			return new DefaultNewGameAiCatalogService();
		}
	}
}