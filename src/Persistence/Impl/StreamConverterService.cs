using System;
using System.IO;
using System.Runtime.InteropServices;

namespace CivOne.Persistence.Impl
{
    public class StreamToSaveDataService : IStreamToSaveDataService
    {
        public T StreamToSaveData<T>(Stream stream) where T : struct
        {
            using MemoryStream ms = new();
            stream.CopyTo(ms);
            byte[] data = ms.ToArray();

            int expectedSize = Marshal.SizeOf<T>();
            if (data.Length != expectedSize)
            {
                throw new InvalidDataException($"Invalid file size {data.Length} (expected {expectedSize})");
            }

            nint dataPtr = Marshal.AllocHGlobal(expectedSize);
            try
            {
                Marshal.Copy(data, 0, dataPtr, data.Length);
                return Marshal.PtrToStructure<T>(dataPtr);
            }
            finally
            {
                Marshal.FreeHGlobal(dataPtr);
            }
        }
    }
}
