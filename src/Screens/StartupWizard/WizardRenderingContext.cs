using System.Collections.Generic;
using System.Drawing;

namespace CivOne.Screens.StartupWizard
{
	/// <summary>
	/// Shared rendering state and hit-test collections passed to rendering delegates.
	/// </summary>
	internal sealed class WizardRenderingContext
	{
		public const int ScrollUpHitAreaNumber = -1;
		public const int ScrollDownHitAreaNumber = -2;

		public Rectangle Box { get; set; }
		public int Cols { get; set; }
		public int Rows { get; set; }
		public float Scale { get; set; }
		public int EntryScrollOffset { get; set; }
		public int ContentEndRow { get; set; }
		public string StatusMessage { get; set; } = string.Empty;
		public Rectangle MessageBox { get; set; }
		public List<(int Number, Rectangle Area)> EntryHitAreas { get; } = [];
		public List<(string Url, Rectangle Area)> LinkAreas { get; } = [];
		public List<Rectangle> GlyphAreas { get; } = [];
	}
}
