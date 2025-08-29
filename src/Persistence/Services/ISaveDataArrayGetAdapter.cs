namespace CivOne.Services
{
	public interface ISaveDataArrayGetAdapter
	{
		byte[] GetArray(string fieldName, int length);
		string[] GetArray(string fieldName, int itemLength, int itemCount);
		void GetByteArray(string fieldName, ref byte[] bytes);
		R[] GetArray<R>(string fieldName, int length) where R : struct;
	}
}