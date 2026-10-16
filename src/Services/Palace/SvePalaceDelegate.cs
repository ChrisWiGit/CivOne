using System;
using System.Diagnostics.CodeAnalysis;
using CivOne.Enums;

namespace CivOne.Services.Palace
{
	/// <summary>
	/// Converts the human player's palace between <see cref="PalaceData"/> and the SVE palace block.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The SVE block holds 12 levels and then 12 styles. Levels 0..8 are the sections, 9..11 the gardens.
	/// A section level of -1 means "not built"; the base sections 3..5 start at 0. Sections 0 and 8 are never used for
	/// the human player. Styles are 0 (medieval), 1 (classical) and 2 (islamic).
	/// </para>
	/// <para>
	/// CivOneX section <c>k</c> is SVE section <c>k + 1</c>. An unbuilt outer section is level 0 in CivOneX and -1 in
	/// the SVE block. This is lossless, because a new section goes directly from -1 to 1.
	/// See <c>docs/plan-palace.impl.md</c>, §3.1.
	/// </para>
	/// </remarks>
	/// <param name="logger">Reports values that had to be corrected; <c>null</c> logs nothing.</param>
	internal sealed class SvePalaceDelegate(ILogger? logger = null)
	{
		private const int SlotCount = 12;
		private const int SectionCount = 7;
		private const int GardenSlot = 9;
		private const int GardenCount = 3;
		private const int MaxSectionLevel = 4;
		private const int MaxGardenLevel = 3;

		/// <summary>
		/// Builds the SVE palace block.
		/// </summary>
		/// <param name="palace">The human player's palace.</param>
		/// <returns>24 values: 12 levels, then 12 styles.</returns>
		[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This class is a delegate, not a static utility.")]
		public short[] ToSve(IPalaceData palace)
		{
			ArgumentNullException.ThrowIfNull(palace);

			short[] block = new short[SlotCount * 2];
			for (int slot = 0; slot < GardenSlot; slot++)
			{
				block[slot] = -1;
			}

			for (int k = 0; k < SectionCount; k++)
			{
				int level = palace.GetPalaceLevel(k);
				PalaceStyle style = palace.GetPalaceStyle(k);
				block[k + 1] = (short)(level == 0 && !IsBaseSection(k) ? -1 : level);
				block[SlotCount + k + 1] = (short)(style == PalaceStyle.None ? 0 : (int)style - 1);
			}

			for (int g = 0; g < GardenCount; g++)
			{
				block[GardenSlot + g] = palace.GetGardenLevel(g);
			}

			return block;
		}

		/// <summary>
		/// Reads the SVE palace block.
		/// </summary>
		/// <param name="block">24 values: 12 levels, then 12 styles.</param>
		/// <param name="buildCount">The palace build counter.</param>
		/// <returns>The palace; invalid values are corrected and logged.</returns>
		public PalaceData FromSve(short[] block, int buildCount)
		{
			ArgumentNullException.ThrowIfNull(block);
			if (block.Length != SlotCount * 2)
			{
				throw new ArgumentException($"The SVE palace block needs {SlotCount * 2} values.", nameof(block));
			}

			if (block[0] != -1 || block[8] != -1)
			{
				Log("SVE palace: unused outer sections are set ({0}, {1}) and ignored.", block[0], block[8]);
			}

			PalaceData palace = new();
			for (int k = 0; k < SectionCount; k++)
			{
				int level = ReadSectionLevel(k, block[k + 1]);
				if (level > 0)
				{
					palace.SetPalace(k, ReadStyle(k, block[SlotCount + k + 1]), (byte)level);
				}
			}

			for (int g = 0; g < GardenCount; g++)
			{
				palace.SetGarden(g, (byte)Clamp(block[GardenSlot + g], 0, MaxGardenLevel, "garden", g));
			}

			palace.SetBuildCount(buildCount);
			return palace;
		}

		private static bool IsBaseSection(int section) => section >= 2 && section <= 4;

		private int ReadSectionLevel(int section, int level)
		{
			if (level == -1 && IsBaseSection(section))
			{
				Log("SVE palace: base section {0} is missing, using level 0.", section);
				return 0;
			}

			return Clamp(level, -1, MaxSectionLevel, "section", section);
		}

		private byte ReadStyle(int section, int style)
		{
			if (style < 0 || style > 2)
			{
				Log("SVE palace: section {0} has unknown style {1}, using medieval.", section, style);
				return (byte)PalaceStyle.Medieval;
			}

			return (byte)(style + 1);
		}

		private int Clamp(int value, int min, int max, string part, int index)
		{
			int clamped = Math.Clamp(value, min, max);
			if (clamped != value)
			{
				Log("SVE palace: {0} {1} has level {2}, using {3}.", part, index, value, clamped);
			}

			return clamped;
		}

		private void Log(string text, params object[] parameters) => logger?.Log(text, parameters);
	}
}
