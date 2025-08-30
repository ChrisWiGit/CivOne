using System;
using System.Runtime.InteropServices;

namespace CivOne.Services.Impl
{
	public class ArrayGetServiceImpl : IArrayGetService
	{
		public void GetByteArray<T>(T structure, string fieldName, ref byte[] bytes) where T : struct
		{
			IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf<T>());
			Marshal.StructureToPtr(structure, ptr, false);
			IntPtr offset = IntPtr.Add(ptr, (int)Marshal.OffsetOf<T>(fieldName));
			Marshal.Copy(offset, bytes, 0, bytes.Length);
			Marshal.FreeHGlobal(ptr);
		}

		public byte[] GetBytes<T>(T structure, string fieldName, int length) where T : struct
		{
			byte[] output = new byte[length];
			GetByteArray(structure, fieldName, ref output);
			return output;
		}

		public string[] GetArray<T>(T structure, string fieldName, int itemLength, int itemCount) where T : struct
		{
			byte[] bytes = GetBytes(structure, fieldName, itemLength * itemCount);
			string[] output = new string[itemCount];
			for (int i = 0; i < itemCount; i++)
				output[i] = bytes.ToString(i * itemLength, itemLength);
			return output;
		}

		public R[] GetArray<T, R>(T structure, string fieldName, int length) where T : struct where R : struct
		{
			R[] output = new R[length];
			int itemSize = Marshal.SizeOf<R>();

			byte[] buffer = GetBytes(structure, fieldName, length * itemSize);

			if (typeof(R).IsPrimitive)
			{
				Buffer.BlockCopy(buffer, 0, output, 0, buffer.Length);

				return output;
			}

			if (!output.GetType().IsArray)
			{
				throw new ArgumentException("Type T must be a primitive or an array type.");
			}
			
			
			IntPtr ptr = Marshal.AllocHGlobal(itemSize);
			try
			{
				for (int i = 0; i < length; i++)
				{
					Marshal.Copy(buffer, i * itemSize, ptr, itemSize);
					output[i] = Marshal.PtrToStructure<R>(ptr);
				}
			}
			finally
			{
				Marshal.FreeHGlobal(ptr);
			}

			return output;
		}
	}
}