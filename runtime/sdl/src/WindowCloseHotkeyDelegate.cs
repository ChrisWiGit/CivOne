using System;
using CivOne.Enums;
using CivOne.Events;

namespace CivOne
{
	/// <summary>
	/// Decides whether a keyboard event is the operating system shortcut for closing the window.
	/// </summary>
	/// <remarks>
	/// SDL already raises a window close event for Alt+F4, so the shortcut must not be forwarded to
	/// the game screens as a normal key press.
	/// Otherwise the quit confirmation dialog opened by the close event is cancelled again by the
	/// same key press, because dialogs close on any key.
	/// </remarks>
	internal sealed class WindowCloseHotkeyDelegate
	{
		/// <summary>
		/// Checks whether the given key event is the window close shortcut and must not be forwarded.
		/// </summary>
		/// <param name="args">The keyboard event to inspect.</param>
		/// <returns><c>true</c> when the event is the window close shortcut (Alt+F4).</returns>
		public bool IsCloseShortcut(KeyboardEventArgs? args) => _isCloseShortcut(args);

		private readonly Func<KeyboardEventArgs?, bool> _isCloseShortcut =
			args => args != null && args.Key == Key.F4 && args.Modifier == KeyModifier.Alt;
	}
}
