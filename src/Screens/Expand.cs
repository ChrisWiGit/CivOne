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
	/// Indicates that a class that extends BaseScreen receives resize events and can be resized.
	/// 
	/// To do so, the class must override the Resize method.
	/// Example:
	/// <code>
	/// protected override void Resize(int width, int height)
	/// {
	///     // Your resize code here
	///    _update = true; // If you have an update flag, set it here
	///     base.Resize(width, height);
	/// }
	/// </code>
	/// </summary>
	public class ScreenResizeable : Attribute
	{
	}
}