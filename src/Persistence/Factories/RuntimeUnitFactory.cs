// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Linq;
using CivOne.Persistence.Model;
using CivOne.Units;

namespace CivOne.Persistence.Factories
{
	/// <summary>
	/// Creates real <see cref="IUnitRestorable"/> instances for the YAML load path
	/// by locating the unit prototype via <see cref="Reflect.GetUnits"/> and
	/// instantiating its concrete type.
	/// </summary>
	public sealed class RuntimeUnitFactory : IUnitFactory
	{
		public IUnitRestorable Create(string className, byte player, Guid? homeCityGuid)
		{
			var proto = Reflect.GetUnits()
			.FirstOrDefault(u => u.GetType().Name == className)
			?? throw new InvalidOperationException($"Unit type '{className}' not found in registered units.");

			return (IUnitRestorable)Activator.CreateInstance(proto.GetType())!;
		}
	}
}
