namespace CivOne.src
{
	class MapGenerationWithoutThread : Map
	{
		protected override void TaskRunEarthMapGeneration()
		{
			RunEarthMapThread();
		}
	}
}