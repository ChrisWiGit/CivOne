// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Diagnostics.CodeAnalysis;
using CivOne.Enums;

namespace CivOne.Events
{
	[SuppressMessage("Microsoft.Design", "CA1003:UseGenericEventHandlerInstances", Justification = "This is a generic event handler and too complicated to use the standard EventHandler<T> delegate.")]
	public delegate void MenuItemEventAction<T>(object sender, MenuItemEventArgs<T> args);

	public class MenuItemEventArgs<T> : EventArgs
	{
		public T Value { get; private set; }

		internal MenuItemEventArgs(T value)
		{
			Value = value;
		}
	}
}