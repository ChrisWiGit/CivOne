using CivOne.IO;
using CivOne.Services.Impl;

namespace CivOne.Services
{
	public class SaveDataArrayGetAdapter : ISaveDataArrayGetAdapter
	{
		private readonly SaveData _saveData;
		private readonly IArrayGetService _array = new ArrayGetServiceImpl();

		public SaveDataArrayGetAdapter(SaveData saveData, IArrayGetService array)
		{
			_saveData = saveData;
			_array = array ?? _array;
		}
		public void GetByteArray(string fieldName, ref byte[] bytes) => _array.GetByteArray(_saveData, fieldName, ref bytes);
		public byte[] GetArray(string fieldName, int length)
		{
			byte[] output = new byte[length];
			GetByteArray(fieldName, ref output);
			return output;
		}

		public string[] GetArray(string fieldName, int itemLength, int itemCount)
		{
			byte[] bytes = GetArray(fieldName, itemLength * itemCount);
			string[] output = new string[itemCount];
			for (int i = 0; i < itemCount; i++)
				output[i] = bytes.ToString(i * itemLength, itemLength);
			return output;
		}

		public R[] GetArray<R>(string fieldName, int length) where R : struct
		{
			return _array.GetArray<SaveData, R>(_saveData, fieldName, length);
		}
	}
}