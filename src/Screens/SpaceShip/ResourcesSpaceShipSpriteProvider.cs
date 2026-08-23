// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.Diagnostics.CodeAnalysis;
using CivOne.Enums;
using CivOne.Graphics;

namespace CivOne.Screens.SpaceShipAssets
{
	/// <summary>
	/// Resolves spaceship part sprites from the shared <see cref="Resources"/> bitmap atlas.
	/// Used as the default <see cref="ISpaceShipSpriteProvider"/> implementation.
	/// </summary>
	public class ResourcesSpaceShipSpriteProvider(Resources resources) : ISpaceShipSpriteProvider
	{
		private readonly string DOCKER_PIC = "DOCKER";
		public bool TryGetPartSprite(SpaceShipComponentType partType, [NotNullWhen(true)] out Picture? sprite)
		{
			sprite = partType switch
			{
				SpaceShipComponentType.StructureHorizontal => resources[DOCKER_PIC][0, 16, 16, 16],
				SpaceShipComponentType.StructureVertical   => resources[DOCKER_PIC][16, 16, 16, 16],
				SpaceShipComponentType.StructureNode       => resources[DOCKER_PIC][32, 16, 16, 16],
				SpaceShipComponentType.Structural          => resources[DOCKER_PIC][0, 15, 16, 16],
				SpaceShipComponentType.PropulsionComponent => resources[DOCKER_PIC][148, 111, 29, 16],
				SpaceShipComponentType.FuelComponent       => resources[DOCKER_PIC][0, 80, 16, 16],
				SpaceShipComponentType.SolarPanelModule    => resources[DOCKER_PIC][0, 96, 32, 32],
				SpaceShipComponentType.CommandModule       => resources[DOCKER_PIC][191, 96, 32, 32],
				SpaceShipComponentType.HabitationModule    => resources[DOCKER_PIC][224, 96, 32, 32],
				SpaceShipComponentType.LifeSupportModule   => resources[DOCKER_PIC][288, 96, 32, 32],
				_                                          => null
			};

			return sprite != null;
		}
	}
}
