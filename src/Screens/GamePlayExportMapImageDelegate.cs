using System;
using System.IO;
using CivOne.Events;
using CivOne.Services;
using CivOne.Services.Maps;
using CivOne.Tasks;
using CivOne.Units;

namespace CivOne.Screens
{
	internal partial class GamePlay
	{
		/// <summary>
		/// Handles exporting the world map, including cities and units, to an image file.
		/// </summary>
		internal sealed class GamePlayExportMapImageDelegate(
			ITranslationService translationService,
			ISettings settings,
			IRuntime runtime,
			IMap map,
			IPlayerGame playerGame,
			SaveMetaDataService saveMetaDataService,
			IGameCalendarService gameCalendarService,
			IMapImageExportService mapImageExportService,
			IGameTaskCommandQueue gameTaskCommandQueue,
			IMessageService messageService,
			IDirectoryService directoryService,
			Func<bool> revealWholeWorld)
		{
			private const string FileExtension = ".bmp";

			private readonly ITranslationService _translationService = translationService ?? throw new ArgumentNullException(nameof(translationService));
			private readonly ISettings _settings = settings ?? throw new ArgumentNullException(nameof(settings));
			private readonly IRuntime _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
			private readonly IMap _map = map ?? throw new ArgumentNullException(nameof(map));
			private readonly IPlayerGame _playerGame = playerGame ?? throw new ArgumentNullException(nameof(playerGame));
			private readonly SaveMetaDataService _saveMetaDataService = saveMetaDataService ?? throw new ArgumentNullException(nameof(saveMetaDataService));
			private readonly IGameCalendarService _gameCalendarService = gameCalendarService ?? throw new ArgumentNullException(nameof(gameCalendarService));
			private readonly IMapImageExportService _mapImageExportService = mapImageExportService ?? throw new ArgumentNullException(nameof(mapImageExportService));
			private readonly IGameTaskCommandQueue _gameTaskCommandQueue = gameTaskCommandQueue ?? throw new ArgumentNullException(nameof(gameTaskCommandQueue));
			private readonly IMessageService _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
			private readonly IDirectoryService _directoryService = directoryService ?? throw new ArgumentNullException(nameof(directoryService));
			private readonly Func<bool> _revealWholeWorld = revealWholeWorld ?? throw new ArgumentNullException(nameof(revealWholeWorld));

			/// <summary>
			/// Menu handler that exports the map image.
			/// </summary>
			/// <param name="sender">The menu item that raised the event.</param>
			/// <param name="args">The menu event arguments.</param>
			public void OnExportMapImageMenuAction(object? sender, MenuItemEventArgs<int> args) => ExportMapImage();

			/// <summary>
			/// Asks the user for a target file and writes the map image to it.
			/// Does nothing when the user cancels the dialog.
			/// </summary>
			[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Catching all exceptions to log and show an error dialog to the user.")]
			public void ExportMapImage()
			{
				try
				{
					string picturesDirectory = _settings.PicturesDirectory;
					_directoryService.CreateDirectory(picturesDirectory);

					string? selectedFile = _runtime.FileChooser(
						true,
						Translate("Export Map Image As..."),
						Path.Combine(picturesDirectory, BuildDefaultFileName()),
						$"{Translate("Bitmap Image")} (*.bmp)|*.bmp");

					if (string.IsNullOrEmpty(selectedFile))
					{
						return;
					}

					// A null player disables the fog-of-war filter, which is what the caller asks for
					// while the whole world is revealed or the terrain editor is active.
					Player? visibilityPlayer = _revealWholeWorld() ? null : _playerGame.HumanPlayer;

					_mapImageExportService.ExportToFile(_map, visibilityPlayer, Path.ChangeExtension(selectedFile, FileExtension));
				}
				catch (Exception ex)
				{
					_runtime.Log("ExportMapImage: Failed to export map image: {0}", ex.Message);
					ShowError(ex);
				}
			}

			/// <summary>
			/// Builds the file name proposed in the save dialog.
			/// Uses the same naming scheme as the savegame display names, for example
			/// <c>Chieftain Caesar of the Romans at 1234 AD.bmp</c>.
			/// </summary>
			/// <returns>A file name that is valid on the current platform.</returns>
			private string BuildDefaultFileName()
			{
				Player humanPlayer = _playerGame.HumanPlayer;
				string name = _translationService.TranslateFormatted(
					"{0} {1} of the {2} at {3}",
					_saveMetaDataService.DifficultyName(_playerGame.Difficulty),
					humanPlayer.LeaderName,
					humanPlayer.TribeNamePlural,
					_gameCalendarService.FormatYear(_playerGame.GameTurn));

				return $"{Sanitize(name)}{FileExtension}";
			}

			/// <summary>
			/// Replaces characters that the file system does not accept in a file name.
			/// </summary>
			/// <param name="name">The proposed file name without extension.</param>
			/// <returns>The file name with all invalid characters replaced by an underscore.</returns>
			private static string Sanitize(string name)
			{
				foreach (char invalid in Path.GetInvalidFileNameChars())
				{
					name = name.Replace(invalid, '_');
				}
				return name;
			}

			private void ShowError(Exception ex)
			{
				_gameTaskCommandQueue.Enqueue(_messageService.Error(
					Translate("Export Map Image"),
					_translationService.TranslateFormattedArray("Could not export map image:\n{0}", ex.Message)));
			}

			private string Translate(string key) => _translationService.Translate(key);
		}
	}
}
