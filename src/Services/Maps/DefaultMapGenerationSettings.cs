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
	/// Default <see cref="IMapGenerationSettings"/> that forwards every
	/// property to <see cref="Settings.Instance"/>. Used by <see cref="CivOne.Map"/>
	/// when no test settings are injected.
	/// </summary>
	internal sealed class DefaultMapGenerationSettings : IMapGenerationSettings
	{
		public bool CustomMapSize => Settings.Instance.CustomMapSize;
	}
}
