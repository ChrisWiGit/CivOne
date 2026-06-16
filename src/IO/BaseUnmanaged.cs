// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Runtime.InteropServices;

namespace CivOne.IO
{
	/// <summary>
	/// Base type for unmanaged byte-backed buffers used throughout rendering and bitmap operations.
	///
	/// Meta note:
	/// These safety guards were introduced after end-of-game screen transitions could dispose bitmap memory
	/// while stale update/render paths still touched the same unmanaged buffers in the same tick.
	/// That race produced native pointer access on freed memory and resulted in AccessViolationException.
	///
	/// EnsureHandle and EnsureRange turn that failure mode into deterministic managed exceptions
	/// (ObjectDisposedException / ArgumentOutOfRangeException), so callers fail fast with actionable diagnostics
	/// instead of process-level memory faults.
	/// </summary>
	public abstract class BaseUnmanaged : IDisposable
	{
		private IntPtr _handle;
		protected IntPtr Handle => _handle;
		protected int Size { get; private set; }
		public bool IsDisposed => _handle == IntPtr.Zero;

		/// <summary>
		/// Validates that unmanaged memory is still allocated.
		/// Without this guard, reads/writes on disposed instances can trigger AccessViolationException.
		/// </summary>
		private void EnsureHandle()
		{
			ObjectDisposedException.ThrowIf(_handle == IntPtr.Zero, this);
		}

		/// <summary>
		/// Validates offset boundaries before unmanaged memory access.
		/// This prevents out-of-range pointer access that can corrupt memory or crash the process.
		/// </summary>
		private void EnsureRange(int offset, int bytes)
		{
			if (offset < 0 || offset > Size - bytes)
			{
				throw new ArgumentOutOfRangeException(nameof(offset), offset, $"Offset out of bounds for size {Size}.");
			}
		}

		/// <summary>
		/// Reads a byte from unmanaged memory after disposal/range validation.
		/// </summary>
		protected byte ReadByte(int offset)
		{
			EnsureHandle();
			EnsureRange(offset, 1);
			return Marshal.ReadByte(_handle, offset);
		}

		/// <summary>
		/// Reads a 16-bit integer from unmanaged memory after disposal/range validation.
		/// </summary>
		protected short ReadShort(int offset)
		{
			EnsureHandle();
			EnsureRange(offset, sizeof(short));
			return Marshal.ReadInt16(_handle, offset);
		}

		/// <summary>
		/// Reads a 32-bit integer from unmanaged memory after disposal/range validation.
		/// </summary>
		protected int ReadInt(int offset)
		{
			EnsureHandle();
			EnsureRange(offset, sizeof(int));
			return Marshal.ReadInt32(_handle, offset);
		}

		/// <summary>
		/// Reads a 64-bit integer from unmanaged memory after disposal/range validation.
		/// </summary>
		protected long ReadLong(int offset)
		{
			EnsureHandle();
			EnsureRange(offset, sizeof(long));
			return Marshal.ReadInt64(_handle, offset);
		}

		/// <summary>
		/// Writes a byte to unmanaged memory after disposal/range validation.
		/// </summary>
		protected void WriteByte(int offset, byte value)
		{
			EnsureHandle();
			EnsureRange(offset, 1);
			Marshal.WriteByte(_handle, offset, value);
		}

		/// <summary>
		/// Writes a 16-bit integer to unmanaged memory after disposal/range validation.
		/// </summary>
		protected void WriteShort(int offset, short value)
		{
			EnsureHandle();
			EnsureRange(offset, sizeof(short));
			Marshal.WriteInt16(_handle, offset, value);
		}

		/// <summary>
		/// Writes a 32-bit integer to unmanaged memory after disposal/range validation.
		/// </summary>
		protected void WriteInt(int offset, int value)
		{
			EnsureHandle();
			EnsureRange(offset, sizeof(int));
			Marshal.WriteInt32(_handle, offset, value);
		}

		/// <summary>
		/// Writes a 64-bit integer to unmanaged memory after disposal/range validation.
		/// </summary>
		protected void WriteLong(int offset, long value)
		{
			EnsureHandle();
			EnsureRange(offset, sizeof(long));
			Marshal.WriteInt64(_handle, offset, value);
		}

		protected unsafe void Clear()
		{
			EnsureHandle();
			// Avoid the per-call managed byte[Size] allocation that the previous Marshal.Copy(new byte[Size], ...)
			// pattern produced. For typical 320x200 bitmaps this saved ~64 KB of Gen0 pressure per Clear().
			System.Runtime.CompilerServices.Unsafe.InitBlockUnaligned((void*)_handle, 0, (uint)Size);
		}

		protected byte[] ToByteArray()
		{
			EnsureHandle();
			byte[] output = new byte[Size];
			Marshal.Copy(_handle, output, 0, output.Length);
			return output;
		}

		protected unsafe BaseUnmanaged(BaseUnmanaged source)
		{
			Size = source.Size;
			_handle = Marshal.AllocHGlobal(Size);
			Buffer.MemoryCopy((byte*)source._handle, (byte*)_handle, source.Size, Size);
		}

		protected BaseUnmanaged(int size, bool initializeZero = false)
		{
			Size = size;
			_handle = Marshal.AllocHGlobal(Size);

			if (initializeZero) Clear();
		}

		~BaseUnmanaged()
		{
			// Use atomic swap so finalizer and an explicit Dispose() on another thread
			// cannot both reach Marshal.FreeHGlobal with the same pointer (double-free / heap corruption).
			IntPtr h = System.Threading.Interlocked.Exchange(ref _handle, IntPtr.Zero);
			if (h != IntPtr.Zero) Marshal.FreeHGlobal(h);
		}

		public void Dispose()
		{
			IntPtr h = System.Threading.Interlocked.Exchange(ref _handle, IntPtr.Zero);
			if (h == IntPtr.Zero) return;
			Marshal.FreeHGlobal(h);
			GC.SuppressFinalize(this);
		}
	}
}