// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.Diagnostics.CodeAnalysis;

namespace CivOne.Buildings
{
	/// <summary>
	/// Marker interface for city production entries that contribute spaceship parts instead of normal buildings.
	/// Processed by spaceship construction flow in <see cref="CivOne.Services.SpaceShip.ISpaceShipService"/>.
	/// </summary>
	//CA1040
	[SuppressMessage("Microsoft.Design", "CA1040:AvoidEmptyInterfaces", Justification = "This interface is intentionally empty and serves as a marker for spaceship-related buildings. Classes implementing BaseBuilding and this interface. ")]
	public interface ISpaceShip
	{
	}
}