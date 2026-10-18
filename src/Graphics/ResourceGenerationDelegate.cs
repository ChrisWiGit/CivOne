using System;

namespace CivOne.Graphics
{
	/// <summary>
	/// Detects that the loaded resources were discarded since the last check, for example after a
	/// graphics mode change.
	/// <br/>
	/// Callers that keep their own copies of resource data (sprites, palettes) ask
	/// <see cref="HasChanged"/> before using them and rebuild their copy when it returns
	/// <see langword="true"/>.
	/// </summary>
	/// <param name="currentGeneration">
	/// Returns the current resource generation.
	/// Defaults to <see cref="Resources.CacheGeneration"/>; tests pass their own counter.
	/// </param>
	internal sealed class ResourceGenerationDelegate(Func<int>? currentGeneration = null)
	{
		private readonly Func<int> _currentGeneration = currentGeneration ?? (() => Resources.CacheGeneration);
		private int? _generation;

		/// <summary>
		/// Returns whether the resource generation changed since the previous call.
		/// <br/>
		/// The first call only records the current generation and returns <see langword="false"/>.
		/// </summary>
		/// <returns><see langword="true"/> if resource data copied before this call is stale.</returns>
		public bool HasChanged()
		{
			int generation = _currentGeneration();
			if (_generation == generation)
			{
				return false;
			}

			bool changed = _generation.HasValue;
			_generation = generation;
			return changed;
		}
	}
}
