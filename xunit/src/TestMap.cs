using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace CivOne.src
{
	class MapGenerationWithoutThread : Map
	{
		protected override void TaskRunEarthMapGeneration()
		{
			Log("Map: Running Earth map generation without thread");
			RunEarthMapThread();
		}
	}
}