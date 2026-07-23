// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

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
		/// Handles saving the current map to a standalone *.comap file from the terrain editor menu.
		/// </summary>
		internal sealed class GamePlaySaveMapDelegate(
			ITranslationService translationService,
			ISettings settings,
			IRuntime runtime,
			IMapSaveService mapSaveService,
			IGameTaskCommandQueue gameTaskCommandQueue,
			IMessageService messageService,
			IDirectoryService directoryService)
		{
			private readonly ITranslationService _translationService = translationService ?? throw new ArgumentNullException(nameof(translationService));
			private readonly ISettings _settings = settings ?? throw new ArgumentNullException(nameof(settings));
			private readonly IRuntime _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
			private readonly IMapSaveService _mapSaveService = mapSaveService ?? throw new ArgumentNullException(nameof(mapSaveService));
			private readonly IGameTaskCommandQueue _gameTaskCommandQueue = gameTaskCommandQueue ?? throw new ArgumentNullException(nameof(gameTaskCommandQueue));
			private readonly IMessageService _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
			private readonly IDirectoryService _directoryService = directoryService ?? throw new ArgumentNullException(nameof(directoryService));

			[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Catching all exceptions to log and show an error dialog to the user.")]
			public void OnSaveMapMenuAction(object? sender, MenuItemEventArgs<int> args)
			{
				string mapsDirectory = _settings.MapsDirectory;

				try
				{
					_directoryService.CreateDirectory(mapsDirectory);
					string initialFileName = Path.Combine(mapsDirectory, "map.comap");

					string? selectedFile = _runtime.FileChooser(
						true,
						Translate("Save Map As..."),
						initialFileName,
						$"{Translate("CivOne Map")} (*.comap)|*.comap");

					if (string.IsNullOrEmpty(selectedFile))
					{
						return;
					}

					string filePath = Path.ChangeExtension(selectedFile, ".comap");
					_mapSaveService.SaveCivOneMap(filePath);
				}
				catch (Exception ex)
				{
					_runtime.Log("OnSaveMapMenuAction: Failed to save map: {0}", ex.Message);
					ShowError(ex);
				}
			}

			private void ShowError(Exception ex)
			{
				_gameTaskCommandQueue.Enqueue(_messageService.Error(
										Translate("Save Map"),
										_translationService.TranslateFormattedArray("Could not save map:\n{0}", ex.Message)));
			}

			private string Translate(string key) => _translationService.Translate(key);
		}
	}
}
