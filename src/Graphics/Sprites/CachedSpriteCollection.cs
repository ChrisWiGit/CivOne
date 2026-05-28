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
	internal class CachedSpriteCollection<T>(Func<T, Bytemap> getSprite) : ISpriteCollection<T>, ICached, IDisposable
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
					Bitmap = null;
				}
				_disposed = true;
			}

			~Sprite() => Dispose(false);
		}

		private readonly Func<T, Bytemap> GetSprite = getSprite;

		private readonly Dictionary<T, ISprite> _sprites = new Dictionary<T, ISprite>();

		private bool _disposed;

		public ISprite this[T index]
		{
			get
			{
				if (!_sprites.ContainsKey(index))
				{
					_sprites.Add(index, new Sprite(GetSprite(index)));
				}
				return _sprites[index];
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

		~CachedSpriteCollection() => Dispose(false);
	}
}