using System;
using System.Collections.Generic;
using CivOne;
using CivOne.Services;
using Xunit;

namespace CivOne.UnitTests.Services
{
	/// <summary>
	/// Tests for <see cref="QuitAutoSaveService"/>.
	/// </summary>
	public class QuitAutoSaveServiceTests
	{
		/// <summary>
		/// No game running means no savegame is written.
		/// </summary>
		[Fact]
		public void TrySaveOnQuitWithoutGameWritesNothing()
		{
			FakePathProvider pathProvider = new();
			QuitAutoSaveService service = new(() => null, pathProvider, new FakeSaveGameServiceFactory());

			string? result = service.TrySaveOnQuit();

			Assert.Null(result);
			Assert.Equal(0, pathProvider.AutoSaveDirectoryCalls);
		}

		/// <summary>
		/// Every dependency is required.
		/// </summary>
		[Fact]
		public void ConstructorWithMissingDependencyThrows()
		{
			Assert.Throws<ArgumentNullException>(
				() => new QuitAutoSaveService(null!, new FakePathProvider(), new FakeSaveGameServiceFactory()));
			Assert.Throws<ArgumentNullException>(
				() => new QuitAutoSaveService(() => null, null!, new FakeSaveGameServiceFactory()));
			Assert.Throws<ArgumentNullException>(
				() => new QuitAutoSaveService(() => null, new FakePathProvider(), null!));
		}

		private sealed class FakePathProvider : ISaveGamePathProvider
		{
			public int AutoSaveDirectoryCalls { get; private set; }

			public string EnsureAutoSaveDirectory()
			{
				AutoSaveDirectoryCalls++;
				return AppContext.BaseDirectory;
			}

			public string EnsureCurrentSaveDirectory() => AppContext.BaseDirectory;

			public string EnsureInitialSaveFilePath() => AppContext.BaseDirectory;

			public string? EnsureLastUsedSaveGamePath() => null;

			public void SetLastUsedSaveGamePath(string path)
			{
			}
		}

		private sealed class FakeSaveGameServiceFactory : IYamlSaveGameServiceFactory
		{
			public List<string> SavedFiles { get; } = [];

			public IYamlSaveGameService Create(Game game) => new FakeSaveGameService(SavedFiles);
		}

		private sealed class FakeSaveGameService(List<string> savedFiles) : IYamlSaveGameService
		{
			public void SaveCos(string filePath) => savedFiles.Add(filePath);
		}
	}
}
