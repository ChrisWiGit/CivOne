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
	/// can flip generation branches without mutating user configuration files.
	/// </summary>
	public interface IMapGenerationSettings
	{
		/// <summary>
		/// True when the user requested a non-default map size, in which case
		/// <see cref="CivOne.Map.Generate(int,int,int,int)"/> prompts the
		/// <c>CustomMapSize</c> screen before launching the generation thread.
		/// </summary>
		bool CustomMapSize { get; }
	}
}
