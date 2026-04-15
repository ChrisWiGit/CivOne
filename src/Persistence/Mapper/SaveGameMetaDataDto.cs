using System;
using System.Globalization;
using CivOne.Persistence.Model.Attributes;

namespace CivOne.Persistence.Model
{
	/// <summary>
	/// DTO for the metadata section of a save game file, containing information about the save's creation time, game version, play duration, and display name.
	/// This class is designed for serialization and deserialization of save game metadata, with methods to convert to and from the runtime SaveMetaData class.
	/// It will be used as part of the overall SaveGameFileDto which encapsulates both metadata and game state for saving and loading games.
	/// </summary>
	public class SaveGameMetaDataDto
	{
		public string GameStartedAt { get; set; }

		public string GameVersion { get; set; }

		public long PlayDurationMinutes { get; set; }

		[Doc("A user-friendly name for the save game, which can be displayed in load/save dialogs. This is not used by the game logic and is purely for presentation purposes.")]
		public string DisplayName { get; set; }

		public DateTimeOffset GetCreatedAtOr(DateTimeOffset fallbackUtc)
		{
			if (DateTimeOffset.TryParse(
				GameStartedAt,
				CultureInfo.InvariantCulture,
				DateTimeStyles.RoundtripKind,
				out var value))
			{
				return value.ToUniversalTime();
			}

			return fallbackUtc.ToUniversalTime();
		}
	}
}