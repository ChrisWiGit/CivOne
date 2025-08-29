using System;
using System.Runtime.InteropServices;

namespace CivOne.Services.Impl
{
	public class ArraySetServiceImpl : IArraySetService
	{
		public void SetArray<T>(ref T structure, string fieldName, params byte[] values) where T : struct
		{
			IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf<T>());
			Marshal.StructureToPtr(structure, ptr, false);
			IntPtr offset = IntPtr.Add(ptr, (int)Marshal.OffsetOf<T>(fieldName));
			Marshal.Copy(values, 0, offset, values.Length);
			structure = Marshal.PtrToStructure<T>(ptr);
			Marshal.FreeHGlobal(ptr);
		}

		public void SetArray<T>(string fieldName, params T[] values) where T : struct
		{
			int itemSize = Marshal.SizeOf<T>();
			byte[] bytes = new byte[values.Length * itemSize];
			for (int i = 0; i < values.Length; i++)
			{
				T value = values[i];
				IntPtr ptr = Marshal.AllocHGlobal(itemSize);
				Marshal.StructureToPtr(value, ptr, false);
				Marshal.Copy(ptr, bytes, (i * itemSize), itemSize);
				Marshal.FreeHGlobal(ptr);
			}
			SetArray(fieldName, bytes);
		}

		public void SetArray(string fieldName, params short[] values)
		{
			byte[] bytes = new byte[values.Length * 2];
			Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length);
			SetArray(fieldName, bytes);
		}

		public void SetArray(string fieldName, params ushort[] values)
		{
			byte[] bytes = new byte[values.Length * 2];
			Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length);
			SetArray(fieldName, bytes);
		}

		public void SetArray<T>(ref T structure, string fieldName, int itemLength, params string[] values) where T : struct
		{
			byte[] bytes = new byte[itemLength * values.Length];
			for (int i = 0; i < values.Length; i++)
				for (int c = 0; c < itemLength; c++)
					bytes[(i * itemLength) + c] = (c >= values[i].Length) ? (byte)0 : (byte)values[i][c];
			SetArray(ref structure, fieldName, bytes);
		}

		byte[] CopyArrayMax(byte[] source, int maxSourceLength, int maxDestLength, byte fillValue = 0)
		{
			byte[] dest = new byte[maxDestLength];
			Array.Fill(dest, fillValue);

			if (source == null || source.Length == 0)
			{
				return dest;
			}

			int length = Math.Min(source.Length, maxSourceLength);
			Array.Copy(source, dest, length);

			return dest;
		}


	}
}