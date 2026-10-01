namespace CivOne.Graphics
{
	public interface IResourceFileBitmapProvider
	{
		bool Exists(string filename);
		IBitmap this[string filename] { get; }
	}

	// smae for getfontheight
	public interface IResourceFontHeightProvider
	{
		int GetFontHeight(int FontId);
	}
}