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
	/// Exposes the subset of <see cref="Settings"/> consumed by
	/// <see cref="CivOne.Map"/> during procedural generation.
	/// Abstracted from the global <see cref="Settings"/> singleton so tests
	/// can inject stubs without mutating user configuration files.
	/// </summary>
	public interface IMapGenerationSettings
	{
	}
}
