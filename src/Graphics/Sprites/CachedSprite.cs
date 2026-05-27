// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using CivOne.IO;

namespace CivOne.Graphics.Sprites
{
	internal class CachedSprite(Func<Bytemap> getSprite) : BaseInstance, ISprite, ICached, IDisposable
	{
		private readonly Func<Bytemap> GetSprite = getSprite;

		private Bytemap _bitmap;
		private bool _disposed;

		public Bytemap Bitmap
		{
			get
			{
				ObjectDisposedException.ThrowIf(_disposed, this);
				return _bitmap ??= GetSprite();
			}
		}

		public void Clear()
		{
			_bitmap?.Dispose();
			_bitmap = null;
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

		~CachedSprite() => Dispose(false);
	}
}