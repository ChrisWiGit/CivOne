using System;
using System.Globalization;
using CivOne.Persistence.Model;

namespace CivOne.Persistence.Factories
{
	/// <summary>
	/// Factory for creating SaveGameMetaDataDto instances from runtime SaveFileMetaData.
	/// This class encapsulates the logic for converting the runtime metadata into a format suitable for serialization in the save game file, including formatting dates and calculating play duration in minutes.
	/// It is used by the YamlSaveGameStateWriter when writing save game files to ensure that the metadata is correctly represented in the resulting YAML output.
	/// Currently there is no interface for this factory, but it is designed to be easily extracted into one if needed for testing or future flexibility in mapping strategies.
	/// </summary>
	#pragma warning disable CA1822 // Mark members as static
	public sealed class SaveGameMetaDataDtoFactory
	{
		public SaveGameMetaDataDto CreateFromRuntime(SaveFileMetaData metaData)
		{
			ArgumentNullException.ThrowIfNull(metaData);
			var playDuration = metaData.GetPlayDurationForSave(DateTimeOffset.UtcNow);

			return new SaveGameMetaDataDto
			{
				GameStartedAt = metaData.GameStartedAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
				GameVersion = metaData.GameVersion,
				PlayDurationMinutes = (long)Math.Max(0, playDuration.TotalMinutes),
				DisplayName = metaData.DisplayName
			};
		}
	}
}