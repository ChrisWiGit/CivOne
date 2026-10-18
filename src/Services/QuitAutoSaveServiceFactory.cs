using System;

namespace CivOne.Services
{
	/// <summary>
	/// Creates <see cref="IQuitAutoSaveService"/> instances with their default dependencies.
	/// </summary>
	public sealed class QuitAutoSaveServiceFactory : IQuitAutoSaveServiceFactory
	{
		/// <inheritdoc />
		public IQuitAutoSaveService Create(IRuntime runtime, ISettings settings)
		{
			ArgumentNullException.ThrowIfNull(runtime);
			ArgumentNullException.ThrowIfNull(settings);

			return new QuitAutoSaveService(
				() => settings.AutoSaveOnQuit ? GetRunningGame() : null,
				new SaveGamePathProvider(runtime, settings),
				new YamlSaveGameServiceFactory(),
				runtime.Log);
		}

		/// <summary>
		/// Returns the running game, or <c>null</c> when no game with a loaded map exists.
		/// </summary>
		/// <remarks>
		/// The map check keeps the quit autosave out of the credits and the startup screens, where a
		/// game instance may already exist but no map has been generated yet.
		/// </remarks>
		private static Game? GetRunningGame()
		{
			if (!Game.Started || !Map.Instance.Ready)
			{
				return null;
			}

			return Game.Instance;
		}
	}
}
