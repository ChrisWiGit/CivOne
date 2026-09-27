using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace CivOne.Services
{
	/// <summary>
	/// Overwrites the autosave in the autosave directory when the game quits.
	/// </summary>
	/// <remarks>
	/// The running game is resolved through a provider function so the service can be tested without
	/// a game engine.
	/// </remarks>
	public sealed class QuitAutoSaveService : IQuitAutoSaveService
	{
		/// <summary>
		/// File name of the autosave written when the game quits.
		/// </summary>
		public const string AutoSaveFileName = "autosave.cos";

		private readonly Func<Game?> _gameProvider;
		private readonly ISaveGamePathProvider _pathProvider;
		private readonly IYamlSaveGameServiceFactory _saveGameServiceFactory;
		private readonly Action<string, object[]>? _logAction;

		/// <summary>
		/// Creates the service.
		/// </summary>
		/// <param name="gameProvider">
		/// Returns the running game, or <c>null</c> when no game is loaded.
		/// </param>
		/// <param name="pathProvider">Provides the autosave directory.</param>
		/// <param name="saveGameServiceFactory">Creates the savegame writer for a game.</param>
		/// <param name="logAction">
		/// Optional log sink receiving a format string and its arguments.
		/// </param>
		public QuitAutoSaveService(
			Func<Game?> gameProvider,
			ISaveGamePathProvider pathProvider,
			IYamlSaveGameServiceFactory saveGameServiceFactory,
			Action<string, object[]>? logAction = null)
		{
			ArgumentNullException.ThrowIfNull(gameProvider);
			ArgumentNullException.ThrowIfNull(pathProvider);
			ArgumentNullException.ThrowIfNull(saveGameServiceFactory);

			_gameProvider = gameProvider;
			_pathProvider = pathProvider;
			_saveGameServiceFactory = saveGameServiceFactory;
			_logAction = logAction;
		}

		/// <inheritdoc />
		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes",
			Justification = "A failing autosave must never prevent the game from shutting down.")]
		public string? TrySaveOnQuit()
		{
			Game? game = _gameProvider();
			if (game == null)
			{
				return null;
			}

			try
			{
				string filePath = Path.Combine(_pathProvider.EnsureAutoSaveDirectory(), AutoSaveFileName);
				_saveGameServiceFactory.Create(game).SaveCos(filePath);
				Log("Autosave on quit written to {0}", filePath);
				return filePath;
			}
			catch (Exception exception)
			{
				Log("Autosave on quit failed: {0}", exception.Message);
				return null;
			}
		}

		private void Log(string format, params object[] arguments) => _logAction?.Invoke(format, arguments);
	}
}
