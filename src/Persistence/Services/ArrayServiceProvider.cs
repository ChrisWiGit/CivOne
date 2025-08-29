namespace CivOne.Services
{
	public static class ArrayServiceProvider
	{
		public static IArraySetService ProvideSet() => new Impl.ArraySetServiceImpl();
		public static IArrayGetService ProvideGet() => new Impl.ArrayGetServiceImpl();
	}
		
}