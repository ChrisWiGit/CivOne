// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using CivOne.Advances;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;

namespace CivOne.Screens.Reports
{
	[Modal]
	internal abstract class BaseReport : BaseScreen
	{
		private bool _update = true;

		protected readonly IBitmap[] Portrait = new Picture[4];

		protected event EventHandler<ScreenEventArgs>? OnMouseDown;
		
		protected byte BackgroundColour { get; }
		protected int OffsetX => Math.Max(0, (Width - 320) / 2);
		protected int OffsetY => Math.Max(0, (Height - 200) / 2);

		protected void DrawReportHeader()
		{
			this.DrawText(Title(), 0, 15, OffsetX + 160, OffsetY + 2, TextAlign.Center)
				.DrawText(TranslateFormatted("{0} of the {1}", Translate("Empire"), Human.TribeNamePlural), 0, 15, OffsetX + 160, OffsetY + 10, TextAlign.Center)
				.DrawText(TranslateFormatted("{0} {1}: {2}", Translate("Emperor"), Human.LeaderName, Game.GameYear), 0, 15, OffsetX + 160, OffsetY + 18, TextAlign.Center);
		}

		protected override void Resize(int width, int height)
		{
			base.Resize(width, height);
			_update = true;
		}
		
		protected override bool HasUpdate(uint gameTick)
		{
			if (!_update) return false;
			_update = false;
			return true;
		}

		protected void SetUpdate()
		{
			_update = true;
		}
		
		public override bool KeyDown(KeyboardEventArgs args)
		{
			Destroy();
			return true;
		}
		
		public override bool MouseDown(ScreenEventArgs args)
		{
			OnMouseDown?.Invoke(this, args);
			if (args.Handled) return true;

			Destroy();
			return true;
		}

		public virtual string Title() => "UNDEFINED REPORT";
		
		protected BaseReport(byte backgroundColour, MouseCursor cursor = MouseCursor.None) : base(cursor)
		{
			BackgroundColour = backgroundColour;

			bool modernGovernment = Human.HasAdvance<Invention>();
			for (int i = 0; i < 4; i++)
			{
				Portrait[i] = Icons.GovernmentPortrait(Human.Government, Enum.Parse<Advisor>($"{i}"), modernGovernment); 
			}

			using Palette palette = Common.DefaultPalette.Merge(Portrait[0].Palette, 144);
			Palette = palette;
			
			this.Clear(backgroundColour);
		}
	}
}