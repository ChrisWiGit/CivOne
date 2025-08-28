namespace CivOne
{
	public interface IArrayService
	{
		void SetArray<T>(ref T structure, string fieldName, params byte[] values) where T : struct;
	}
}