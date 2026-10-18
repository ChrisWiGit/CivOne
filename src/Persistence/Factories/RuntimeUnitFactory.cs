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