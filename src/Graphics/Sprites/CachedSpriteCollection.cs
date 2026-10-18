// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Collections.Generic;
using CivOne.IO;

namespace CivOne.Graphics.Sprites
{
	internal class CachedSpriteCollection<T>(Func<T, Bytemap> getSprite) : ISprites<T>, ICached, IDisposable where T : notnull
	{
		private class Sprite : ISprite, IDisposable
		{
			private bool _disposed;
			public Bytemap Bitmap { get; private set; }

			internal Sprite(Bytemap bitmap)
			{
				Bitmap = bitmap;
			}

			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			protected virtual void Dispose(bool disposing)
			{
				if (_disposed) return;
				if (disposing)
				{
					Bitmap?.Dispose();
				}
				_disposed = true;
			}

			~Sprite() => Dispose(false);
		}

		private readonly Func<T, Bytemap> GetSprite = getSprite;
		private readonly ResourceGenerationDelegate _resourceGeneration = new();

		private readonly Dictionary<T, ISprite> _sprites = [];

		private bool _disposed;

		/// <summary>
		/// Gets a shared sprite entry for the given key.
		///
		/// The returned sprite and its bitmap are cache-owned.
		/// Consumers must not dispose the returned objects.
		/// </summary>
		public ISprite this[T index]
		{
			get
			{
				if (_resourceGeneration.HasChanged())
				{
					// The resources were reloaded (for example after a graphics mode change), so the
					// cached sprites show the old graphics. They are dropped but not disposed: they may
					// wrap shared fallback bitmaps or still be referenced by a layer of the current frame.
					// Their finalizers release the unmanaged buffers.
					_sprites.Clear();
				}
				if (!_sprites.TryGetValue(index, out ISprite? value))
				{
					value = new Sprite(GetSprite(index));
					_sprites.Add(index, value);
				}
				return value;
			}
		}

		public void Clear()
		{
			foreach (ISprite sprite in _sprites.Values)
				(sprite as IDisposable)?.Dispose();
			_sprites.Clear();
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (_disposed) return;
			if (disposing)
			{
				Clear();
			}
			_disposed = true;
		}
	}
}