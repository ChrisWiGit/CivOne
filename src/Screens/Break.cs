// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;

namespace CivOne.Screens
{
	/// <summary>
	/// Marks a screen that should break the screen update loop.
	/// </summary>
	/// <remarks>
	/// When a screen is decorated with this attribute, the update loop in <see cref="RuntimeHandler.Update"/> will stop 
	/// processing additional screens after this screen is updated. This prevents lower screens in the stack from being 
	/// updated, effectively giving this screen exclusive update priority.
	/// 
	/// Typical use case: Modal screens or screens that should block updates to screens beneath them in the screen stack.
	/// 
	/// The attribute is checked in <see cref="RuntimeHandler"/> during the <see cref="RuntimeHandler.Update"/> method 
	/// after each screen update. When detected, the loop returns immediately.
	/// </remarks>
	[AttributeUsage(AttributeTargets.Class)]
	public sealed class BreakAttribute : Attribute
	{
	}
}