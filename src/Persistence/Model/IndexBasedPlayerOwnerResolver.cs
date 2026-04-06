// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

namespace CivOne.Persistence.Model
{
	/// <summary>
	/// Used on the YAML load path where <see cref="PlayerDtoMapper.ToDto"/> is never
	/// called.  <see cref="IPlayerOwnerResolver.TryResolveOwnerId"/> always returns
	/// <c>false</c> so the unit filter in <c>ToDto</c> emits all units, but since
	/// <c>ToDto</c> is never invoked during load this implementation is a no-op stub.
	/// </summary>
	public sealed class IndexBasedPlayerOwnerResolver : IPlayerOwnerResolver
	{
		public bool TryResolveOwnerId(IPlayer player, out byte ownerId)
		{
			ownerId = 0;
			return false;
		}
	}
}
