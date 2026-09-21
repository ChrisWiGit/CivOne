// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using CivOne.Advances;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.Governments;
using CivOne.Sound;

using Gov = CivOne.Governments;

namespace CivOne.Screens
{
	/// <summary>
	/// The throne room of a foreign leader, shown for an audience.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This screen plays no sound. The original's audience has three of them, and CivOne can serve
	/// none of them correctly yet:
	/// </para>
	/// <list type="bullet">
	/// <item>
	/// The short anthem of the visiting leader's civilization (tune 19-32) while the question
	/// "Will you receive him?" is on screen. CivOne has no such prompt - the audience opens
	/// unasked - so there is nothing to play it against.
	/// </item>
	/// <item>
	/// The long anthem of that civilization (tune 5-18) once the throne room is drawn. This one
	/// could be added here, but it runs far longer than this screen does, which only lives until
	/// the next key or click; it would need the same owned-sound handling that CityView has.
	/// </item>
	/// <item>
	/// The ultimatum sting (tune 33, <see cref="SoundNames.EventUltimatum"/>) over the already open
	/// throne room, whenever the foreign leader turns hostile: tribute or technology demanded, a
	/// provocation, a rejection, units ordered out, mobilisation. CivOne's diplomacy has none of
	/// these exchanges, so the sting has no trigger at all and its tune stays unused.
	/// </item>
	/// </list>
	/// </remarks>
	[ScreenResizeable]
	internal class King : BaseScreen
	{
		private readonly Player _player;

		private readonly Picture _background;

		private bool _update = true;
		private int OffsetX => System.Math.Max(0, (Width - 320) / 2);
		private int OffsetY => System.Math.Max(0, (Height - 200) / 2);

		private byte OpaqueBlackColour
		{
			get
			{
				for (int i = 1; i < Palette.Length; i++)
				{
					Colour c = Palette[i];
					if (c.A > 0 && c.R == 0 && c.G == 0 && c.B == 0)
						return (byte)i;
				}
				return 5;
			}
		}

		private void Render()
		{
			this.Clear(OpaqueBlackColour);
			this.AddLayer(_background, OffsetX, OffsetY)
				.AddLayer(_player.Civilization.Leader.GetPortrait(), OffsetX + 90, OffsetY);
		}
		
		protected override bool HasUpdate(uint gameTick)
		{
			if (!_update) return false;
			Render();
			_update = false;
			return true;
		}

		protected override void Resize(int width, int height)
		{
			base.Resize(width, height);
			_update = true;
		}
		
		public override bool KeyDown(KeyboardEventArgs args)
		{
			Destroy();
			return true;
		}
		
		public override bool MouseDown(ScreenEventArgs args)
		{
			Destroy();
			return true;
		}
		
		public King(Player player)
		{
			_player = player;

			bool modern = player.HasAdvance<Invention>();
			int govId = 0;
			if (player.Government is Gov.Monarchy)
				govId = 1;
			else if (player.Government is Republic || player.Government is Gov.Democracy)
				govId = 2;
			else if (player.Government is Gov.Communism)
			{
				govId = 3;
				modern = false;
			}

			_background = Resources[$"BACK{govId}{(modern ? "M" : "A")}"];

			_background.ColourReplace(0, 5);
			IBitmap portrait = _player.Civilization.Leader.GetPortrait();
			
			using (Palette palette = _background.Palette.Copy())
			{
				palette.Merge(portrait.Palette, 64, 80);
				Palette = palette;
			}

			Render();
		}
	}
}