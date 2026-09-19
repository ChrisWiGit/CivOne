using CivOne.Civilizations;
using CivOne.Persistence.Game;
using CivOne.Persistence.Model;

namespace CivOne.Persistence.Factories
{
	/// <summary>
	/// Creates real <see cref="Player"/> instances for the YAML load path.
	/// TribeName and TribeNamePlural are set afterwards by <see cref="PlayerDtoMapper.FromDto"/>
	/// via <see cref="IPlayerRestorable"/>.
	/// </summary>
	public sealed class RuntimePlayerFactory : IPlayerFactory
	{
		public IPlayerRestorable Create(ICivilization civilization, PlayerDto dto)
		=> new Player(civilization, customTribeName: null);
	}
}