using System;
using System.IO;
using System.Runtime.InteropServices;

namespace CivOne.Persistence.Impl
{
    public interface IStreamToSaveDataService
    {
        T StreamToSaveData<T>(Stream stream) where T : struct;
    }
}
