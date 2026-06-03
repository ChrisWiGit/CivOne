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

namespace CivOne.Events
{
	public delegate void KeyboardEventHandler(object sender, KeyboardEventArgs args);

	public class KeyboardEventArgs : EventArgs
	{
		public new static readonly KeyboardEventArgs Empty = new(Key.None);
		public Key Key { get; private set; }
		public char KeyChar { get; private set; }
		public KeyModifier Modifier { get; private set; }
		
		public bool Control => (Modifier & KeyModifier.Control) > 0;
		public bool Alt => (Modifier & KeyModifier.Alt) > 0;
		public bool Shift => (Modifier & KeyModifier.Shift) > 0;
		/// <summary>
		/// Special state for the Caps Lock key, which is not a modifier in the same sense 
		/// as Control/Alt/Shift but is still relevant to track for input handling purposes.
		/// Adding it to Modifier would break the semantics of Modifier as a bitfield of simultaneous modifiers, 
		/// since code already checks for Alt or Control without expecting Caps Lock to be part of that, 
		/// thus breaking existing hotkey handling logic.
		/// </summary>
		public bool CapsLock { get; internal set; }
		public bool None => (Key == Key.None);

		public bool this[Key key] => (Key == key);
		public bool this[KeyModifier modifier, Key key] => (Modifier == modifier) && (Key == key);
		
		public KeyboardEventArgs(Key key, KeyModifier modifier = KeyModifier.None)
		{
			Key = key;
			KeyChar = (char)0x00;
			switch (key)
			{
				case Key.Space: KeyChar = ' '; break;
			}
			Modifier = modifier;
		}
		
		public KeyboardEventArgs(char keyChar, KeyModifier modifier = KeyModifier.None)
		{
			Key = Key.Character;
			KeyChar = keyChar;
			Modifier = modifier;
		}
	}
}