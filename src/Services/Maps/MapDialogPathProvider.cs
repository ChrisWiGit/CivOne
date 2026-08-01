using System;
using System.IO;

namespace CivOne.Services.Maps
{
	/// <summary>
	/// Default <see cref="IMapDialogPathProvider"/> backed by runtime settings.
	/// </summary>
	/// <remarks>
	/// Directory creation and existence checks are injected so tests can run without touching the file system.
	/// </remarks>
	/// <param name="runtime">Used to read and write the stored directory.</param>
	/// <param name="settings">Provides the maps directory of the profile.</param>
	/// <param name="createDirectoryAction">Creates a directory; defaults to <see cref="Directory.CreateDirectory(string)"/>.</param>
	/// <param name="directoryExistsFunc">Checks whether a directory exists; defaults to <see cref="Directory.Exists(string)"/>.</param>
	internal sealed class MapDialogPathProvider(
		IRuntime runtime,
		ISettings settings,
		Action<string>? createDirectoryAction = null,
		Func<string, bool>? directoryExistsFunc = null) : IMapDialogPathProvider
	{
		private const string LastUsedMapDialogPathKey = "LastUsedMapDialogPath";
		private const string DefaultMapFileName = "map.comap";

		private readonly Action<string> _createDirectoryAction = createDirectoryAction ?? (dir => Directory.CreateDirectory(dir));
		private readonly Func<string, bool> _directoryExistsFunc = directoryExistsFunc ?? Directory.Exists;

		/// <inheritdoc/>
		public string EnsureInitialMapFilePath()
		{
			string directory = GetLastUsedMapDirectory() ?? settings.MapsDirectory;
			_createDirectoryAction(directory);
			return Path.Combine(directory, DefaultMapFileName);
		}

		/// <inheritdoc/>
		public void SetLastUsedMapPath(string filePath)
		{
			if (string.IsNullOrWhiteSpace(filePath))
			{
				return;
			}

			string? directory = Path.GetDirectoryName(filePath);
			if (string.IsNullOrWhiteSpace(directory))
			{
				return;
			}

			runtime.SetSetting(LastUsedMapDialogPathKey, directory);
		}

		private string? GetLastUsedMapDirectory()
		{
			string? directory = runtime.GetSetting(LastUsedMapDialogPathKey);
			if (string.IsNullOrWhiteSpace(directory) || !_directoryExistsFunc(directory))
			{
				return null;
			}

			return directory;
		}
	}
}
