// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;

namespace CivOne.Units
{
	/// <summary>
	/// Marks a unit type as the default unit choice for generic unit handling.
	/// </summary>
	/// <remarks>
	/// This attribute is used as metadata so game logic can discover a fallback or
	/// preferred unit type without hard-coding concrete class names.
	/// </remarks>
	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
	public sealed class DefaultUnitProductionAttribute : Attribute
	{
	}
}