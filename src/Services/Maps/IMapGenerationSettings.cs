using System.Diagnostics.CodeAnalysis;

namespace CivOne.Services.Maps
{
	/// <summary>
	/// Marker interface used as an injection seam for map generation settings.
	/// Implementations can provide test-specific behavior without coupling callers to global state.
	/// </summary>
	[SuppressMessage("Microsoft.Design", "CA1040:AvoidEmptyInterfaces", Justification = "This is a marker interface used as an injection seam for map generation settings.")]
	public interface IMapGenerationSettings
	{
	}
}