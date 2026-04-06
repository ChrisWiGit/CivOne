// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using CivOne.Civilizations;

namespace CivOne.Persistence.Model
{
	/// <summary>
	/// Creates real <see cref="Player"/> instances for the YAML load path.
	/// TribeName and TribeNamePlural are set afterwards by <see cref="PlayerDtoMapper.FromDto"/>
	/// via <see cref="IPlayerRestorable"/>.
	/// </summary>
	public sealed class RuntimePlayerFactory : IPlayerFactory
	{
		public IPlayerRestorable Create(ICivilization civilization, PlayerDto dto)
			=> new Player(civilization);
	}
}
