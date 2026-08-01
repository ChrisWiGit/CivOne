// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

namespace CivOne.Services.Maps
{
	/// <summary>
	/// Creates <see cref="ICustomMapLoaderService"/> instances.
	/// </summary>
	public static class CustomMapLoaderServiceFactory
	{
		/// <summary>
		/// Creates a new <see cref="ICustomMapLoaderService"/> backed by the runtime settings.
		/// </summary>
		public static ICustomMapLoaderService Create() => new CustomMapLoaderService(Settings.Instance);
	}
}
