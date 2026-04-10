// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;

namespace CivOne
{
	/// <summary>
	/// Runtime metadata used for save game serialization.
	/// This state is owned by <see cref="Game"/> and can be initialized
	/// through controlled lifecycle methods.
	/// </summary>
	public sealed class SaveFileMetaData
	{
		private DateTimeOffset _sessionStartedAtUtc;

		public DateTimeOffset GameStartedAt { get; private set; }
		public string GameVersion { get; private set; }
		public TimeSpan PlayDuration { get; private set; }
		public string DisplayName { get; set; }

		internal void InitializeForNewGame(string gameVersion, DateTimeOffset nowUtc)
		{
			var currentUtc = nowUtc.ToUniversalTime();
			GameStartedAt = currentUtc;
			GameVersion = NormalizeVersion(gameVersion);
			PlayDuration = TimeSpan.Zero;
			_sessionStartedAtUtc = currentUtc;
		}

		internal void InitializeForLoadedGame(string gameVersion)
		{
			// For binary-loaded games where no metadata was saved.
			// Use current time since we don't know the actual creation time.
			var currentUtc = DateTimeOffset.UtcNow.ToUniversalTime();
			GameStartedAt = currentUtc;
			GameVersion = NormalizeVersion(gameVersion);
			PlayDuration = TimeSpan.Zero;
			_sessionStartedAtUtc = currentUtc;
		}

		internal void RestoreFromSave(
			DateTimeOffset createdAt,
			string gameVersion,
			TimeSpan playDuration,
			string displayName)
		{
			GameStartedAt = createdAt.ToUniversalTime();
			GameVersion = NormalizeVersion(gameVersion);
			PlayDuration = playDuration < TimeSpan.Zero ? TimeSpan.Zero : playDuration;
			DisplayName = displayName;
			_sessionStartedAtUtc = DateTimeOffset.UtcNow.ToUniversalTime();
		}

		internal TimeSpan GetPlayDurationForSave(DateTimeOffset nowUtc)
		{
			var currentUtc = nowUtc.ToUniversalTime();
			var sessionStartedAtUtc = _sessionStartedAtUtc == default ? currentUtc : _sessionStartedAtUtc;
			var elapsed = currentUtc - sessionStartedAtUtc;
			if (elapsed < TimeSpan.Zero)
			{
				elapsed = TimeSpan.Zero;
			}

			return PlayDuration + elapsed;
		}

		private static string NormalizeVersion(string version)
			=> string.IsNullOrWhiteSpace(version) ? "unknown" : version.Trim();
	}
}