using CivOne.Graphics;

namespace CivOne.Services.Maps
{
	/// <summary>
	/// Provides access to picture resources (MAP.PIC, SP299.PIC, saved .MAP files)
	/// consumed by <see cref="CivOne.Map"/> during load/save and Earth-map import.
	/// Abstracted away from the global <see cref="Resources"/> singleton so tests can
	/// supply synthetic bitmaps without touching the file system or proprietary assets.
	/// </summary>
	public interface IMapResourceProvider
	{
		/// <summary>
		/// Loads the picture resource with the given key/filename.
		/// The caller is responsible for disposing the returned picture's bitmap/palette
		/// in the same way as when read directly from <see cref="Resources"/>.
		/// </summary>
		Picture GetPicture(string filename);
	}
}