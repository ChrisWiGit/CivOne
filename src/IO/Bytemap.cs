// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace CivOne.IO
{
	public class Bytemap : BaseUnmanaged
	{
		public int Width { get; private set; }
		public int Height { get; private set; }
		public new Size Size => new(Width, Height);
		public int Length => base.Size;

		public byte this[int x, int y]
		{
			get => ReadByte((Width * y) + x);
			set => WriteByte((Width * y) + x, value);
		}

		internal Bytemap this[int left, int top, int width, int height]
		{
			get
			{
				// Reject pathological inputs early: a negative width/height previously propagated all the way
				// to `new byte[sx2 - sx1]` (OverflowException) or `new Bytemap(negative, h)` (undefined AllocHGlobal size).
				if (width <= 0 || height <= 0) return new Bytemap(Math.Max(1, width), Math.Max(1, height));

				int dx = 0;
				int sx1 = left, sy1 = top, sx2 = left + width, sy2 = top + height;
				if (sx1 < 0) { dx -= sx1; sx1 = 0; }
				if (sy1 < 0) { sy1 = 0; }
				if (sx2 > Width) sx2 = Width;
				if (sy2 > Height) sy2 = Height;

				Bytemap output = new(width, height);
				if (sx2 <= sx1 || sy2 <= sy1) return output;

				byte[] buffer = new byte[sx2 - sx1];
				for (int yy = sy1; yy < sy2; yy++)
				{
					Marshal.Copy(IntPtr.Add(Handle, (Width * yy) + sx1), buffer, 0, buffer.Length);
					// Row index in the output bitmap: (yy - top) maps source row yy to the
					// correct destination row. The destination row stride is the full output
					// width, not the clipped source row width in buffer.Length.
					Marshal.Copy(buffer, 0, IntPtr.Add(output.Handle, ((yy - top) * output.Width) + dx), buffer.Length);
				}
				return output;
			}
		}

		internal void Fill(int left, int top, int width, int height, byte colour)
		{
			// Correct clipping: negative left/top must SHRINK width/height by the overflow,
			// not enlarge them (the previous "width -= left" with negative left increased width
			// and could lead to out-of-bounds Marshal.Copy on a sufficiently large bitmap).
			if (left < 0) { width += left; left = 0; }
			if (top < 0) { height += top; top = 0; }
			if (width <= 0 || height <= 0) return;
			if (left + width > Width) width = Width - left;
			if (top + height > Height) height = Height - top;
			if (width <= 0 || height <= 0) return;

			byte[] buffer = new byte[width].Clear(colour);
			for (int yy = top; yy < (top + height); yy++)
			{
				Marshal.Copy(buffer, 0, IntPtr.Add(Handle, (Width * yy) + left), buffer.Length);
			}
		}

		public new void Clear() => base.Clear();

		public int[] ToColourMap(int[] palette, bool rightToLeft = false, bool bottomToTop = false)
		{
			// Bulk-copy unmanaged pixel buffer once, then index in managed memory.
			// Replaces Width*Height per-pixel Marshal.ReadByte + EnsureHandle + EnsureRange P/Invokes,
			// reducing render hot-path cost by roughly an order of magnitude.
			byte[] src = ToByteArray();
			int[] output = new int[Length];

			if (!rightToLeft && !bottomToTop)
			{
				for (int idx = 0; idx < Length; idx++)
				{
					output[idx] = palette[src[idx]];
				}
				return output;
			}

			int w = Width, h = Height;
			int o = 0;
			for (int yy = 0; yy < h; yy++)
			{
				int y = bottomToTop ? (h - yy - 1) : yy;
				int rowOffset = y * w;
				for (int xx = 0; xx < w; xx++)
				{
					int x = rightToLeft ? (w - xx - 1) : xx;
					output[o++] = palette[src[rowOffset + x]];
				}
			}
			return output;
		}

		public new byte[] ToByteArray() => base.ToByteArray();

		/// <summary>
		/// Bulk-copies the unmanaged pixel buffer into the provided destination array.
		///
		/// Used by the SDL render loop to avoid the per-frame <c>byte[Width*Height]</c>
		/// allocation that <see cref="ToByteArray"/> would otherwise produce.
		/// </summary>
		/// <param name="destination">Buffer that receives the pixel bytes.
		/// Must be at least <see cref="Length"/> bytes long.</param>
		public void CopyTo(byte[] destination)
		{
			ArgumentNullException.ThrowIfNull(destination);
			if (destination.Length < Length) throw new ArgumentException("Destination buffer too small.", nameof(destination));
			if (Handle == IntPtr.Zero) return;
			Marshal.Copy(Handle, destination, 0, Length);
		}

		public static Bytemap Copy(Bytemap source) => new Bytemap(source);

		private Bytemap(Bytemap source) : base(source)
		{
			Width = source.Width;
			Height = source.Height;
		}

		public Bytemap(int width, int height) : base(width * height, true)
		{
			Width = width;
			Height = height;
		}

		public Bytemap(byte[,] bytes) : this(bytes.GetLength(0), bytes.GetLength(1))
		{
			// Flatten into a row-major managed buffer, then bulk-copy to unmanaged memory once.
			// Replaces Width*Height per-pixel Marshal.WriteByte + EnsureHandle + EnsureRange calls.
			int w = Width, h = Height;
			byte[] flat = new byte[w * h];
			for (int y = 0; y < h; y++)
			{
				for (int x = 0; x < w; x++)
				{
					flat[(y * w) + x] = bytes[x, y];
				}
			}
			Marshal.Copy(flat, 0, Handle, flat.Length);
		}
	}
}