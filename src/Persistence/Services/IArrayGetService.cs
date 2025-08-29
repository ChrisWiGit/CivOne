namespace CivOne.Services
{
	public interface IArrayGetService
	{
		string[] GetArray<T>(T structure, string fieldName, int itemLength, int itemCount) where T : struct;
		void GetByteArray<T>(T structure, string fieldName, ref byte[] bytes) where T : struct;
		byte[] GetBytes<T>(T structure, string fieldName, int length) where T : struct;

		public R[] GetArray<T, R>(T structure, string fieldName, int length)
			where T : struct where R : struct;
	}
}