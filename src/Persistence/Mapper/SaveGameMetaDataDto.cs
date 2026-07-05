using System;
using System.Globalization;
using CivOne.Persistence.Model.Attributes;

namespace CivOne.Persistence.Model
{
	/// <summary>
	/// DTO for the metadata section of a save game file, containing information about the save's creation time, game version, play duration, display name, and save GUID.
	/// This class is designed for serialization and deserialization of save game metadata, with methods to convert to and from the runtime SaveMetaData class.
	/// </summary>
	public class SaveGameMetaDataDto
	{
		public string GameStartedAt { get; set; } = string.Empty;

		public string GameVersion { get; set; } = string.Empty;

		public long PlayDurationMinutes { get; set; }

		[Doc("A user-friendly name for the save game, which can be displayed in load/save dialogs. This is not used by the game logic and is purely for presentation purposes.")]
		public string DisplayName { get; set; } = string.Empty;

		[Doc("Stable save GUID that uniquely identifies this save across sessions. Generated at save time, persists across load/save cycles.")]
		public Guid? SaveGuid { get; set; }

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