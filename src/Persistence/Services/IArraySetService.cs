namespace CivOne.Services
{
	public interface IArraySetService
	{
		void SetArray<T>(ref T structure, string fieldName, params byte[] values) where T : struct;
		void SetArray<T>(string fieldName, params T[] values) where T : struct;
		void SetArray(string fieldName, params short[] values);
		void SetArray(string fieldName, params ushort[] values);
		void SetArray<T>(ref T structure, string fieldName, int itemLength, params string[] values) where T : struct;
	}
}