using CivOne.IO;

namespace CivOne.Services.Maps
{
	internal interface IMapBitmapScaler
	{
		Bytemap Scale(Bytemap source, int targetWidth, int targetHeight);
	}
}