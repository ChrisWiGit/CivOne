using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace CivOne
{
	/// <summary>
	/// Represents one plugin-provided image pack factory instance.
	/// </summary>
	public interface IImagePackFactory
	{
		/// <summary>
		/// Creates one image pack instance.
		/// </summary>
		/// <returns>The created image pack.</returns>
		ImageStore Create();
	}

	/// <summary>
	/// Stores image overrides exposed by one plugin image pack.
	/// </summary>
	public sealed record ImageStore
	{
		private readonly Dictionary<string, ImageAssetReference> _overrides;

		/// <summary>
		/// Gets an empty image store.
		/// </summary>
		public static ImageStore Empty { get; } = new();

		/// <summary>
		/// Gets all image overrides keyed by a stable image identifier.
		/// </summary>
		public IReadOnlyDictionary<string, ImageAssetReference> Overrides => _overrides;

		/// <summary>
		/// Initializes a new instance of the <see cref="ImageStore"/> class.
		/// </summary>
		/// <param name="overrides">
		/// Optional initial image overrides.
		/// </param>
		public ImageStore(IReadOnlyDictionary<string, ImageAssetReference>? overrides = null)
		{
			_overrides = overrides == null
				? new(StringComparer.OrdinalIgnoreCase)
				: new(overrides, StringComparer.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Returns a copy of this store with one override added or replaced.
		/// </summary>
		/// <param name="key">
		/// Stable image identifier used by the runtime.
		/// </param>
		/// <param name="asset">
		/// The replacement asset reference.
		/// </param>
		/// <returns>
		/// A new <see cref="ImageStore"/> containing the updated override set.
		/// </returns>
		public ImageStore WithOverride(string key, ImageAssetReference asset)
		{
			ArgumentNullException.ThrowIfNull(key);
			ArgumentNullException.ThrowIfNull(asset);

			Dictionary<string, ImageAssetReference> updated = new(_overrides, StringComparer.OrdinalIgnoreCase)
			{
				[key] = asset
			};
			return new(updated);
		}

		/// <summary>
		/// Tries to resolve one image override by key.
		/// </summary>
		/// <param name="key">
		/// Stable image identifier used by the runtime.
		/// </param>
		/// <param name="asset">
		/// When this method returns <see langword="true"/>, contains the matching override.
		/// </param>
		/// <returns>
		/// <see langword="true"/> when an override exists for <paramref name="key"/>; otherwise <see langword="false"/>.
		/// </returns>
		public bool TryGetOverride(string key, [NotNullWhen(true)] out ImageAssetReference? asset)
		{
			ArgumentNullException.ThrowIfNull(key);
			return _overrides.TryGetValue(key, out asset);
		}
	}

	/// <summary>
	/// Describes one replacement asset location inside a plugin-provided image source.
	/// </summary>
	/// <param name="ResourceName">
	/// Gets the plugin resource name containing the image data.
	/// </param>
	/// <param name="Left">
	/// Gets the optional crop left coordinate.
	/// </param>
	/// <param name="Top">
	/// Gets the optional crop top coordinate.
	/// </param>
	/// <param name="Width">
	/// Gets the optional crop width.
	/// </param>
	/// <param name="Height">
	/// Gets the optional crop height.
	/// </param>
	public sealed record ImageAssetReference(
		string ResourceName,
		int? Left = null,
		int? Top = null,
		int? Width = null,
		int? Height = null);
}
