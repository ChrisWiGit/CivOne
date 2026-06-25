// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using CivOne.Enums;

namespace CivOne.Civilizations
{
	/// <summary>
	/// Modify the civilization leader.
	/// </summary>
	/// <param name="leader">The new leader for this civilization.</param>
	public sealed class CivilizationLeader(Leader leader) : BaseAttribute(typeof(Leader), leader, InRange)
	{
		private static bool InRange(object value) => Enum.IsDefined(typeof(Leader), value) && ((Leader)value) != Leader.Atilla;

		public Leader Value => GetRequiredValue<Leader>();

		public Leader Leader => Value;
	}
}