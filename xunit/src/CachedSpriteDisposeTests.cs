// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using CivOne.Graphics.Sprites;
using CivOne.IO;
using Xunit;

namespace CivOne.UnitTests
{
	public class CachedSpriteDisposeTests
	{
		[Fact]
		public void BitmapLazyInitialisesOnce()
		{
			int factoryCalls = 0;
			using var sprite = new CachedSprite(() => { factoryCalls++; return new Bytemap(2, 2); });

			Assert.Equal(0, factoryCalls);
			_ = sprite.Bitmap;
			_ = sprite.Bitmap;

			Assert.Equal(1, factoryCalls);
		}

		[Fact]
		public void DisposeIsIdempotent()
		{
			var sprite = new CachedSprite(() => new Bytemap(1, 1));
			_ = sprite.Bitmap;

			sprite.Dispose();
			sprite.Dispose(); // must not throw
		}

		[Fact]
		public void BitmapAfterDisposeThrows()
		{
			var sprite = new CachedSprite(() => new Bytemap(1, 1));
			_ = sprite.Bitmap;
			sprite.Dispose();

			Assert.Throws<ObjectDisposedException>(() => sprite.Bitmap);
		}

		[Fact]
		public void DisposeWithoutAccessDoesNotInvokeFactory()
		{
			int factoryCalls = 0;
			var sprite = new CachedSprite(() => { factoryCalls++; return new Bytemap(1, 1); });

			sprite.Dispose();

			Assert.Equal(0, factoryCalls);
		}
	}

	public class CachedSpriteCollectionDisposeTests
	{
		[Fact]
		public void IndexerLazyInitialisesPerKey()
		{
			int factoryCalls = 0;
			using var coll = new CachedSpriteCollection<int>(_ => { factoryCalls++; return new Bytemap(1, 1); });

			_ = coll[0];
			_ = coll[0];
			_ = coll[1];

			Assert.Equal(2, factoryCalls);
		}

		[Fact]
		public void ClearDisposesAndAllowsReBuild()
		{
			int factoryCalls = 0;
			using var coll = new CachedSpriteCollection<int>(_ => { factoryCalls++; return new Bytemap(1, 1); });
			_ = coll[0];

			coll.Clear();
			_ = coll[0];

			Assert.Equal(2, factoryCalls);
		}

		[Fact]
		public void DisposeIsIdempotent()
		{
			var coll = new CachedSpriteCollection<int>(_ => new Bytemap(1, 1));
			_ = coll[0];

			coll.Dispose();
			coll.Dispose(); // must not throw
		}

		[Fact]
		public void DisposeClearsEntries()
		{
			int factoryCalls = 0;
			var coll = new CachedSpriteCollection<int>(_ => { factoryCalls++; return new Bytemap(1, 1); });
			_ = coll[0];
			_ = coll[1];

			coll.Dispose();
			_ = coll[0]; // collection is reusable after Dispose; entries rebuilt

			Assert.Equal(3, factoryCalls);
		}
	}
}
