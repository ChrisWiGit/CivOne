using CivOne.Enums;
using CivOne.Units;

namespace CivOne.TestPlugin
{
	/// <summary>
	/// Renames the militia unit.
	/// Used to verify that the host discovers <see cref="Modification"/> subclasses inside a plugin
	/// assembly and applies them.
	/// </summary>
	[Name("Test Militia")]
	public sealed class TestWarriorModification() : UnitModification(UnitType.Militia);
}
