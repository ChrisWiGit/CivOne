namespace CivOne.Services
{
	public interface IArrayGetService<T> where T : struct
	{
		string[] GetArray(T structure, string fieldName, int itemLength, int itemCount);
		void GetByteArray(T structure, string fieldName, ref byte[] bytes);
		byte[] GetBytes(T structure, string fieldName, int length);

		public R[] GetArray<R>(T structure, string fieldName, int length) where R : struct;
	}
}