// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

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
