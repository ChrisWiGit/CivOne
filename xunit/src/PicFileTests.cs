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
using CivOne.Graphics;
using CivOne.Graphics.ImageFormats;
using CivOne.IO;
using Xunit;

namespace CivOne.UnitTests
{
	/// <summary>
	/// Tests ownership and dispose behavior for <see cref="PicFile"/>.
	/// </summary>
	public class PicFileTests : src.TestsBase
	{
		[Fact]
		public void DisposeFromPictureDoesNotDisposeBorrowedBuffers()
		{
			using Picture picture = new(4, 4);
			Bytemap sourceBitmap = picture.Bitmap;
			Palette sourcePalette = picture.Palette;
			PicFile testee = new(picture);

			testee.Dispose();

			Assert.False(sourceBitmap.IsDisposed);
			Assert.False(sourcePalette.IsDisposed);
		}

		[Fact]
		public void DisposeFromFileDisposesOwnedBytemaps()
		{
			string fileName = Path.Combine(Path.GetTempPath(), $"civone-picfile-{Path.GetRandomFileName()}.map");
			try
			{
				byte[] bytes;
				using (Picture picture = new(8, 8))
				using (PicFile writer = new(picture))
				{
					bytes = writer.GetBytes();
				}

				File.WriteAllBytes(fileName, bytes);

				PicFile testee = new(fileName);
				Bytemap? picture16 = testee.GetPicture16;
				Bytemap? picture256 = testee.GetPicture256;

				testee.Dispose();

				Assert.NotNull(picture256);
				if (picture16 != null)
				{
					Assert.True(picture16.IsDisposed);
				}
				Assert.True(picture256!.IsDisposed);
				Assert.Null(testee.GetPicture16);
				Assert.Null(testee.GetPicture256);
			}
			finally
			{
				if (File.Exists(fileName))
				{
					File.Delete(fileName);
				}
			}
		}

		[Fact]
		public void DisposeCanBeCalledTwiceWithoutThrowing()
		{
			using Picture picture = new(4, 4);
			using PicFile testee = new(picture);

			Exception first = Record.Exception(() => testee.Dispose());
			Exception second = Record.Exception(() => testee.Dispose());

			Assert.Null(first);
			Assert.Null(second);
		}
	}
}
