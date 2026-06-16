using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CivOne.Services.HallOfFame
{
	/// <summary>
	/// File-based implementation for reading and writing Hall of Fame entries as YAML.
	/// </summary>
	internal sealed class HallOfFameFileRepositoryImpl(IAtomicFileReplacementService atomicFileReplacementService) : IHallOfFameFileRepository
	{
		private const string FileName = "HallOfFame.yaml";
		private readonly IAtomicFileReplacementService _atomicFileReplacementService = atomicFileReplacementService;

		public bool TryLoad(string? storageDirectory, out IReadOnlyList<HallOfFameEntry> entries, [NotNullWhen(false)] out string? error)
		{
			entries = [];
			error = null;

			if (string.IsNullOrWhiteSpace(storageDirectory))
			{
				error = "Storage directory is empty.";
				return false;
			}

			string filePath = GetFilePath(storageDirectory);
			if (!File.Exists(filePath))
			{
				error = $"File not found: {filePath}";
				return false;
			}

			try
			{
				string yaml = File.ReadAllText(filePath);
				var deserializer = new DeserializerBuilder()
					.WithNamingConvention(PascalCaseNamingConvention.Instance)
					.IgnoreUnmatchedProperties()
					.Build();

				var model = deserializer.Deserialize<HallOfFameFileModel>(yaml) ?? new HallOfFameFileModel();
				entries = [.. model.Entries.Select(MapToEntry)];
				return true;
			}
			catch (Exception ex)
			{
				error = ex.Message;
				return false;
			}
		}

		public bool TrySave(string? storageDirectory, IReadOnlyList<HallOfFameEntry> entries, [NotNullWhen(false)] out string? error)
		{
			error = null;

			if (string.IsNullOrWhiteSpace(storageDirectory))
			{
				error = "Storage directory is empty.";
				return false;
			}

			if (entries == null)
			{
				error = "Entries are null.";
				return false;
			}

			string filePath = GetFilePath(storageDirectory);
			try
			{
				Directory.CreateDirectory(storageDirectory);
				var serializer = new SerializerBuilder()
					.WithNamingConvention(PascalCaseNamingConvention.Instance)
					.ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
					.Build();

				string yaml = serializer.Serialize(new HallOfFameFileModel
				{
					Entries = [.. entries.Select(MapToModel)]
				});

				_atomicFileReplacementService.ReplaceFile(filePath, stream =>
				{
					byte[] data = Encoding.UTF8.GetBytes(yaml);
					stream.Write(data, 0, data.Length);
				});

				return true;
			}
			catch (Exception ex)
			{
				error = ex.Message;
				return false;
			}
		}

		private static string GetFilePath(string storageDirectory) => Path.Combine(storageDirectory, FileName);

		private static HallOfFameEntry MapToEntry(HallOfFameEntryModel model)
		{
			DateTimeOffset createdAtUtc = DateTimeOffset.UtcNow;
			if (!string.IsNullOrWhiteSpace(model.CreatedAtUtc)
				&& DateTimeOffset.TryParse(
					model.CreatedAtUtc,
					CultureInfo.InvariantCulture,
					DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
					out DateTimeOffset parsedCreatedAtUtc))
			{
				createdAtUtc = parsedCreatedAtUtc.ToUniversalTime();
			}

			return new HallOfFameEntry(
				LeaderName: model.LeaderName ?? string.Empty,
				LeaderTitle: model.LeaderTitle ?? string.Empty,
				CivilizationNamePlural: model.CivilizationNamePlural ?? string.Empty,
				YearLabel: model.YearLabel ?? string.Empty,
				Population: model.Population,
				Score: model.Score,
				RatingRankLabel: model.RatingRankLabel ?? string.Empty,
				RatingPercent: model.RatingPercent,
				CreatedAtUtc: createdAtUtc);
		}

		private static HallOfFameEntryModel MapToModel(HallOfFameEntry entry)
		{
			return new HallOfFameEntryModel
			{
				LeaderName = entry.LeaderName,
				LeaderTitle = entry.LeaderTitle,
				CivilizationNamePlural = entry.CivilizationNamePlural,
				YearLabel = entry.YearLabel,
				Population = entry.Population,
				Score = entry.Score,
				RatingRankLabel = entry.RatingRankLabel,
				RatingPercent = entry.RatingPercent,
				CreatedAtUtc = entry.CreatedAtUtc.UtcDateTime.ToString("o", CultureInfo.InvariantCulture)
			};
		}

	}

	/// <summary>
	/// Serialized file model for the Hall of Fame YAML storage.
	/// </summary>
	public sealed class HallOfFameFileModel
	{
		public int Version { get; set; } = 1;
		public List<HallOfFameEntryModel> Entries { get; set; } = [];
	}

	/// <summary>
	/// Individual entry model used for YAML serialization/deserialization.
	/// </summary>
	public sealed class HallOfFameEntryModel
	{
		public string LeaderName { get; set; } = string.Empty;
		public string LeaderTitle { get; set; } = string.Empty;
		public string CivilizationNamePlural { get; set; } = string.Empty;
		public string YearLabel { get; set; } = string.Empty;
		public int Population { get; set; }
		public int Score { get; set; }
		public string RatingRankLabel { get; set; } = string.Empty;
		public int RatingPercent { get; set; }
		public string CreatedAtUtc { get; set; } = string.Empty;
	}
}
