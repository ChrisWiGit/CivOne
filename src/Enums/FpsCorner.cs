// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

namespace CivOne.Enums
{
	/// <summary>
	/// Specifies the screen corner where the FPS overlay is displayed, or Off to disable it.
	/// </summary>
	public enum FpsCorner
	{
		/// <summary>FPS display is disabled.</summary>
		Off = 0,
		/// <summary>FPS display in the top-left corner.</summary>
		TopLeft = 1,
		/// <summary>FPS display in the top-right corner.</summary>
		TopRight = 2,
		/// <summary>FPS display in the bottom-left corner.</summary>
		BottomLeft = 3,
		/// <summary>FPS display in the bottom-right corner.</summary>
		BottomRight = 4
	}
}
