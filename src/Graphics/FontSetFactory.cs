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

namespace CivOne.Graphics
{
	/// <summary>
	/// Creates <see cref="IFont"/> instances from <c>FONTS.CV</c> data.
	/// <br/>
	/// Decides whether to return a plain <see cref="Fontset"/> or an
	/// <see cref="InternationalSimulatedFontSet"/> based on the
	/// <b>Simulate International Font</b> setting (Shift+F1 → Game Options → Language):
	/// <list type="bullet">
	///   <item><description><b>Yes</b> — always use the simulating font set.</description></item>
	///   <item><description><b>No</b> — always use the plain font set.</description></item>
	///   <item><description><b>Auto</b> — always use the simulating font set. It handles both
	///   <c>FONTS.CV</c> files that already contain international glyphs in the control-character
	///   range (mapped via Unicode → low-code lookup) and English-only files (synthesised by
	///   composing base letter + combining diacritic).</description></item>
	/// </list>
	/// </summary>
	internal static class FontSetFactory
	{
		/// <summary>
		/// Creates a font instance for the given <c>FONTS.CV</c> data block.
		/// </summary>
		/// <param name="bytes">Raw bytes of the <c>FONTS.CV</c> file.</param>
		/// <param name="offset">Byte offset pointing to the start of the font data block.</param>
		/// <returns>An <see cref="IFont"/> implementation appropriate for the current language setting.</returns>
		internal static IFont Create(byte[] bytes, ushort offset)
		{
			return Settings.Instance.SimulateInternationalFont switch
			{
				SimulateInternationalFont.No => new Fontset(bytes, offset),
				_ => new InternationalSimulatedFontSet(bytes, offset),
			};
		}

		/// <summary>
		/// Returns <see langword="true"/> when <paramref name="font"/> is an
		/// <see cref="InternationalSimulatedFontSet"/> capable of rendering Unicode characters
		/// beyond the file's stored ASCII range.
		/// </summary>
		internal static bool IsInternationalFontSet(IFont font)
		{
			return font is InternationalSimulatedFontSet;
		}
	}
}
