using CivOne.Graphics;

namespace CivOne.Services.Maps
{
	/// <summary>
	/// Default <see cref="IMapResourceProvider"/> that delegates to the global
	/// <see cref="Resources"/> singleton. Used by <see cref="CivOne.Map"/> when no
	/// test provider is injected.
	/// </summary>
	internal sealed class DefaultMapResourceProvider : IMapResourceProvider
	{
		public Picture GetPicture(string filename) => Resources.Instance[filename];
	}
}