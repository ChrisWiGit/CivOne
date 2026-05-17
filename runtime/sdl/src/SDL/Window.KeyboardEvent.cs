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
using CivOne.Events;

namespace CivOne
{
	internal static partial class SDL
	{
		internal abstract partial class Window
		{
			protected event KeyboardEventHandler OnKeyDown, OnKeyUp;

			private KeyModifier ConvertModifier(SDL_KMOD kmod)
			{
				KeyModifier output = KeyModifier.None;
				if ((kmod & SDL_KMOD.KMOD_CTRL) > 0) output |= KeyModifier.Control;
				if ((kmod & SDL_KMOD.KMOD_ALT) > 0) output |= KeyModifier.Alt;
				if ((kmod & SDL_KMOD.KMOD_SHIFT) > 0) output |= KeyModifier.Shift;
				return output;
			}

			private Key ConvertKey(SDL_Scancode scanCode)
			{
				switch (scanCode)
				{
					case SDL_Scancode.SDL_SCANCODE_TAB:
						return Key.Tab;
					case SDL_Scancode.SDL_SCANCODE_F1:
						return Key.F1;
					case SDL_Scancode.SDL_SCANCODE_F2:
						return Key.F2;
					case SDL_Scancode.SDL_SCANCODE_F3:
						return Key.F3;
					case SDL_Scancode.SDL_SCANCODE_F4:
						return Key.F4;
					case SDL_Scancode.SDL_SCANCODE_F5:
						return Key.F5;
					case SDL_Scancode.SDL_SCANCODE_F6:
						return Key.F6;
					case SDL_Scancode.SDL_SCANCODE_F7:
						return Key.F7;
					case SDL_Scancode.SDL_SCANCODE_F8:
						return Key.F8;
					case SDL_Scancode.SDL_SCANCODE_F9:
						return Key.F9;
					case SDL_Scancode.SDL_SCANCODE_F10:
						return Key.F10;
					case SDL_Scancode.SDL_SCANCODE_F11:
						return Key.F11;
					case SDL_Scancode.SDL_SCANCODE_F12:
						return Key.F12;
					case SDL_Scancode.SDL_SCANCODE_PAUSE:
						return Key.Pause;
					case SDL_Scancode.SDL_SCANCODE_SLASH:
					case SDL_Scancode.SDL_SCANCODE_KP_DIVIDE:
						return Key.Slash;
					case SDL_Scancode.SDL_SCANCODE_RETURN:
					case SDL_Scancode.SDL_SCANCODE_KP_ENTER:
						return Key.Enter;
					case SDL_Scancode.SDL_SCANCODE_ESCAPE:
						return Key.Escape;
					case SDL_Scancode.SDL_SCANCODE_BACKSPACE:
						return Key.Backspace;
					case SDL_Scancode.SDL_SCANCODE_HOME:
						return Key.Home;
					case SDL_Scancode.SDL_SCANCODE_END:
						return Key.End;
					case SDL_Scancode.SDL_SCANCODE_PAGEUP:
						return Key.PageUp;
					case SDL_Scancode.SDL_SCANCODE_PAGEDOWN:
						return Key.PageDown;
					case SDL_Scancode.SDL_SCANCODE_DELETE:
						return Key.Delete;
					case SDL_Scancode.SDL_SCANCODE_UP:
						return Key.Up;
					case SDL_Scancode.SDL_SCANCODE_DOWN:
						return Key.Down;
					case SDL_Scancode.SDL_SCANCODE_LEFT:
						return Key.Left;
					case SDL_Scancode.SDL_SCANCODE_RIGHT:
						return Key.Right;
					case SDL_Scancode.SDL_SCANCODE_SPACE:
						return Key.Space;
					case SDL_Scancode.SDL_SCANCODE_MINUS:
					case SDL_Scancode.SDL_SCANCODE_KP_MINUS:
						return Key.Minus;
					case SDL_Scancode.SDL_SCANCODE_KP_PLUS:
						return Key.Plus;
					case SDL_Scancode.SDL_SCANCODE_KP_0:
						return Key.NumPad0;
					case SDL_Scancode.SDL_SCANCODE_KP_1:
						return Key.NumPad1;
					case SDL_Scancode.SDL_SCANCODE_KP_2:
						return Key.NumPad2;
					case SDL_Scancode.SDL_SCANCODE_KP_3:
						return Key.NumPad3;
					case SDL_Scancode.SDL_SCANCODE_KP_4:
						return Key.NumPad4;
					case SDL_Scancode.SDL_SCANCODE_KP_5:
						return Key.NumPad5;
					case SDL_Scancode.SDL_SCANCODE_KP_6:
						return Key.NumPad6;
					case SDL_Scancode.SDL_SCANCODE_KP_7:
						return Key.NumPad7;
					case SDL_Scancode.SDL_SCANCODE_KP_8:
						return Key.NumPad8;
					case SDL_Scancode.SDL_SCANCODE_KP_9:
						return Key.NumPad9;
					default:
						return Key.None;
				}
			}

			/// <summary>
			/// Converts physical top-row digit keys by scancode (30..39) to '1'..'0'.
			/// This is required because keycode values depend on keyboard layout and Shift state,
			/// while scancodes identify the physical key reliably.
			/// </summary>
			private KeyboardEventArgs ConvertTopRowDigitByScancode(SDL_Scancode scanCode, KeyModifier modifier)
			{
				// Top-row digits by scancode are stable across keyboard layouts:
				// 30..38 => 1..9 and 39 => 0.
				int scancode = (int)scanCode;
				if (scancode < 30 || scancode > 39) return null;

				char digit = (scancode == 39) ? '0' : (char)('1' + (scancode - 30));
				return new KeyboardEventArgs(digit, modifier);
			}

			/// <summary>
			/// Maps control-modified symbol keycodes (for example '!' instead of '1') back to digits.
			/// This is needed for certain layout/OS combinations where Ctrl+digit arrives as a symbol
			/// and would otherwise be filtered out as non-digit input.
			/// </summary>
			private KeyboardEventArgs ConvertControlDigitSymbolToDigit(char keyChar, KeyModifier modifier)
			{
				if ((modifier & KeyModifier.Control) == 0) return null;

				switch (keyChar)
				{
					case '!': return new KeyboardEventArgs('1', modifier);
					case '"':
					case '@': return new KeyboardEventArgs('2', modifier);
					case '#':
					case '§': return new KeyboardEventArgs('3', modifier);
					case '$': return new KeyboardEventArgs('4', modifier);
					case '%': return new KeyboardEventArgs('5', modifier);
					case '^':
					case '&': return new KeyboardEventArgs('6', modifier);
					case '/': return new KeyboardEventArgs('7', modifier);
					case '(': return new KeyboardEventArgs('8', modifier);
					case ')': return new KeyboardEventArgs('9', modifier);
					case '=': return new KeyboardEventArgs('0', modifier);
					default: return null;
				}
			}

			private string BuildKeyboardDebugMessage(SDL_KeyboardEvent keyboardEvent, KeyboardEventArgs args)
			{
				char rawChar = (char)keyboardEvent.KeySym.Keycode;
				string rawCharText = char.IsControl(rawChar)
					? $"0x{((int)rawChar):X2}"
					: $"'{rawChar}'";

				string convertedText;
				if (args == null)
				{
					convertedText = "null";
				}
				else if (args.Key == Key.Character)
				{
					convertedText = $"Key=Character, KeyChar='{args.KeyChar}', Modifier={args.Modifier}";
				}
				else
				{
					convertedText = $"Key={args.Key}, KeyChar=0x{((int)args.KeyChar):X2}, Modifier={args.Modifier}";
				}

				return $"[Keyboard] State={keyboardEvent.State}, Scancode={keyboardEvent.KeySym.Scancode} ({(int)keyboardEvent.KeySym.Scancode}), Keycode={(int)keyboardEvent.KeySym.Keycode} ({rawCharText}), RawModifier={keyboardEvent.KeySym.Modifier}, Converted={convertedText}";
			}

			/// <summary>
			/// Normalizes SDL keyboard events into CivOne keyboard events with stable semantics.
			/// The conversion order intentionally prefers explicit key mappings, then layout-independent
			/// digit recovery, and finally character fallback.
			/// </summary>
			private KeyboardEventArgs ConvertKeyEvent(SDL_KeyboardEvent keyboardEvent)
			{
				KeyModifier modifier = ConvertModifier(keyboardEvent.KeySym.Modifier);
				Key key = ConvertKey(keyboardEvent.KeySym.Scancode);
				if (key != Key.None)
				{
					return new KeyboardEventArgs(key, modifier);
				}

				KeyboardEventArgs topRowDigit = ConvertTopRowDigitByScancode(keyboardEvent.KeySym.Scancode, modifier);
				if (topRowDigit != null) return topRowDigit;

				char keyChar = (char)keyboardEvent.KeySym.Keycode;
				KeyboardEventArgs controlDigit = ConvertControlDigitSymbolToDigit(keyChar, modifier);
				if (controlDigit != null) return controlDigit;
				if (keyChar != '.' && keyChar != ',' && (char.ToLower(keyChar) < 'a' || (int)char.ToLower(keyChar) > 'z') && (keyChar < '0' || keyChar > '9')) return null;
				return new KeyboardEventArgs(char.ToUpper(keyChar), modifier);
			}

			private void HandleEventKeyboard(SDL_KeyboardEvent keyboardEvent)
			{
				if (_paused && ConvertKey(keyboardEvent.KeySym.Scancode) != Key.Pause)
				{
					// discard all key events except for Pause when paused, to avoid processing a backlog of inputs after unpausing
					return;
				}

				KeyboardEventArgs args = ConvertKeyEvent(keyboardEvent);

#if DEBUG
				Log(BuildKeyboardDebugMessage(keyboardEvent, args));
#endif

				if (args == null) return;

				switch (keyboardEvent.State)
				{
					case SDL_KeyState.SDL_PRESSED:
						OnKeyDown?.Invoke(this, args);
						return;
					case SDL_KeyState.SDL_RELEASED:
						OnKeyUp?.Invoke(this, args);
						return;
				}
			}
		}
	}
}