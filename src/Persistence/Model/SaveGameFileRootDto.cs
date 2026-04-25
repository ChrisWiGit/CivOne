using System;
using System.Globalization;

namespace CivOne.Persistence.Model
{
	/// <summary>
	/// DTO for the metadata section of a save game file, containing information about the save's creation time, game version, play duration, and display name.
	/// This class is designed for serialization and deserialization of save game metadata, with methods to convert to and from the runtime SaveMetaData class.
	/// It will be used as part of the overall SaveGameFileDto which encapsulates both metadata and game state for saving and loading games.
	/// </summary>
	public class SaveGameFileRootDto
	{
		public const uint CurrentFormatVersion = 1;

		public uint FormatVersion { get; set; } = CurrentFormatVersion;

		public SaveGameMetaDataDto Meta { get; set; }

		public GameStateDto GameState { get; set; }
	}
}