using System;
using System.Globalization;

namespace CivOne.Persistence.Model
{
	/// <summary>
	/// DTO for save game file root (format version 1).
	/// Contains: FormatVersion, Meta (with SaveGuid), and GameState.
	/// </summary>
	public class SaveGame1FileRootDto
	{
		public const uint CurrentFormatVersion = 1;

		public uint FormatVersion { get; set; } = CurrentFormatVersion;

		public SaveGameMetaDataDto? Meta { get; set; }

		public GameStateDto? GameState { get; set; }
	}
}