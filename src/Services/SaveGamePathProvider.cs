using System;
using System.IO;

namespace CivOne.Services
{
    public class SaveGamePathProvider : ISaveGamePathProvider
    {
		// DI proc to CreateDirectory
		private readonly Action<string> _createDirectoryAction;
		private readonly Func<string, bool> _directoryExistsFunc;

        private const string CosDefaultFileName = "savegame.cos";
        private const string LastUsedSaveGameDialogPathKey = "LastUsedSaveGameDialogPath";

        private readonly IRuntime _runtime;
        private readonly ISettings _settings;

        public SaveGamePathProvider(
                IRuntime runtime, ISettings settings,
				Action<string> createDirectoryAction = null,
				Func<string, bool> directoryExistsFunc = null
				)
        {
            _runtime = runtime;
            _settings = settings;
			_createDirectoryAction = createDirectoryAction ?? 
				(dir => { Directory.CreateDirectory(dir); });
			_createDirectoryAction(_settings.CosSavesDirectory);
			_directoryExistsFunc = directoryExistsFunc ??
				(dir => Directory.Exists(dir));
        }

        public string EnsureCurrentSaveDirectory()
        {
            _createDirectoryAction(_settings.CosSavesDirectory);
            return _settings.CosSavesDirectory;
        }

        string EnsurePathExists(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Path cannot be null or whitespace.", nameof(path));
            _createDirectoryAction(path);
            return path;
        }

        public string GetInitialSaveFilePath()
        {
            string lastUsed = GetLastUsedSaveGamePath();
            if (!string.IsNullOrWhiteSpace(lastUsed))
            {
                return Path.Combine(EnsurePathExists(lastUsed), CosDefaultFileName);
            }

            return Path.Combine(EnsurePathExists(EnsureCurrentSaveDirectory()), CosDefaultFileName);
        }

        public string GetLastUsedSaveGamePath()
        {
            string path = _runtime.GetSetting(LastUsedSaveGameDialogPathKey);
            if (string.IsNullOrWhiteSpace(path) || !_directoryExistsFunc(path))
                return null;

            return path;
        }

        public void SetLastUsedSaveGamePath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return;

            string dir = Path.GetDirectoryName(filePath);
            if (string.IsNullOrWhiteSpace(dir)) return;

            _runtime.SetSetting(LastUsedSaveGameDialogPathKey, dir);
        }
    }
}
