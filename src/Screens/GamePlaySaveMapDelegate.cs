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

					// Checked before the dialog opens so the *.map filter entry can be hidden entirely
					// when the current map would lose data in that format, instead of letting the user
					// pick it and only failing afterwards.
					LegacyMapSaveCompatibilityResult legacyCompatibility = _mapSaveService.GetLegacyMapCompatibility();
					bool canSaveAsLegacyMap = legacyCompatibility.CanSaveAsLegacyMap;
					if (!canSaveAsLegacyMap)
					{
						_runtime.Log("OnSaveMapMenuAction: Legacy map save unavailable: {0}", legacyCompatibility.Reason);
					}

					string? selectedFile = _runtime.FileChooser(
						true,
						Translate("Save Map As..."),
						initialFileName,
						BuildFileChooserFilter(canSaveAsLegacyMap));

					if (string.IsNullOrEmpty(selectedFile))
					{
						return;
					}

					// The *.map filter was hidden, but a save dialog still lets the user type any
					// extension by hand - fall back to *.comap silently rather than reject the save.
					if (canSaveAsLegacyMap && IsLegacyMapExtension(selectedFile))
					{
						_mapSaveService.SaveLegacyMap(Path.ChangeExtension(selectedFile, ".map"));
						return;
					}

					_mapSaveService.SaveCivOneMap(Path.ChangeExtension(selectedFile, ".comap"));
				}
				catch (Exception ex)
				{
					_runtime.Log("OnSaveMapMenuAction: Failed to save map: {0}", ex.Message);
					ShowError(ex);
				}
			}

			/// <summary>
			/// Builds the save-dialog filter, offering the legacy <c>*.map</c> entry only when
			/// <paramref name="includeLegacyMap"/> is true.
			/// </summary>
			/// <param name="includeLegacyMap">Whether the current map can be written as a legacy <c>*.map</c> file.</param>
			/// <returns>The filter string passed to <see cref="IRuntime.FileChooser"/>.</returns>
			private string BuildFileChooserFilter(bool includeLegacyMap)
			{
				string comapFilter = $"{Translate("CivOne Map")} (*.comap)|*.comap";
				if (!includeLegacyMap)
				{
					return comapFilter;
				}

				return $"{comapFilter}|{Translate("Civ1 Map")} (*.map)|*.map";
			}

			/// <summary>
			/// Determines whether the user chose the legacy <c>*.map</c> extension in the save dialog.
			/// </summary>
			/// <param name="filePath">The file path returned by the file chooser.</param>
			/// <returns><c>true</c> when the extension is <c>.map</c> (case-insensitive); otherwise <c>false</c>.</returns>
			private static bool IsLegacyMapExtension(string filePath)
				=> string.Equals(Path.GetExtension(filePath), ".map", StringComparison.OrdinalIgnoreCase);

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
