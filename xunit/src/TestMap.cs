namespace CivOne.src
{
	sealed class MapGenerationWithoutThread : Map
	{
		protected override void TaskRunEarthMapGeneration()
		{
			Log("Map: Running Earth map generation without thread");
			RunEarthMapThread();
		}
	}
}