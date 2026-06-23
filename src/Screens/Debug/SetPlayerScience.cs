// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Globalization;
using System.Linq;
using CivOne.Enums;
using CivOne.Graphics;
using CivOne.Graphics.Sprites;
using CivOne.Services.Screen;
using CivOne.Tasks;
using CivOne.UserInterface;

namespace CivOne.Screens.Debug
{
	[ScreenResizeable]
	internal class SetPlayerScience : BaseScreen
	{
		private readonly CivSelectMenuDelegate _civSelectDelegate;

		private Player? _selectedPlayer;
		private int OffsetX => Math.Max(0, (Width - 320) / 2);
		private int OffsetY => Math.Max(0, (Height - 200) / 2);

		private Input? ActiveInput => Inputs.OfType<Input>().FirstOrDefault();

		private void DrawPlayerSelectDialog()
		{
			_civSelectDelegate.DrawDialog(this, OffsetX, OffsetY);
		}

		private void DrawInputDialog()
		{
			int ox = OffsetX;
			int oy = OffsetY;

			this.Clear();
			this.FillRectangle(80 + ox, 80 + oy, 161, 33, 11)
				.FillRectangle(81 + ox, 81 + oy, 159, 31, 15)
				.DrawText(Translate("Set Player Science..."), 0, 5, 88 + ox, 82 + oy)
				.FillRectangle(88 + ox, 95 + oy, 105, 14, 5)
				.FillRectangle(89 + ox, 96 + oy, 103, 12, 15);

			if (ActiveInput is Input input)
			{
				input.X = 90 + ox;
				input.Y = 97 + oy;
			}
		}

		public string? Value { get; private set; }

		public event EventHandler? Accept, Cancel;

		private void OnPlayerSelected(Player player)
		{
			Palette = Common.Screens[^1].OriginalColours;

			_selectedPlayer = player;

			if (_selectedPlayer == null)
			{
				System.Diagnostics.Debug.Assert(false, "Selected player is null in OnPlayerSelected.");
				return;
			}

			DrawInputDialog();

			CloseMenus();
			EnsureManagedInput();
		}

		protected override IScreen? CreateManagedInput()
		{
			if (_selectedPlayer == null)
			{
				return null;
			}

			Input input = new(Palette, _selectedPlayer.Science.ToString(CultureInfo.CurrentCulture), 0, 5, 11, 90 + OffsetX, 97 + OffsetY, 101, 10, 5);
			input.Accept += PlayerScience_Accept;
			input.Cancel += PlayerScience_Cancel;
			return input;
		}

		private void PlayerScience_Accept(object? sender, EventArgs args)
		{
			if (sender is not Input input) return;
			Value = input.Text;

			if (!short.TryParse(Value, out short playerScience) || playerScience < 0 || playerScience > 30000)
			{
				GameTask.Enqueue(Message.Error(Translate("-- DEBUG: Set Player Science --"), TranslateFormattedArray("The value {0} is invalid or out of range.\nPlease enter a value between 0 and\n30000.", Value)));
			}
			else
			{
				if (_selectedPlayer == null)
				{
					System.Diagnostics.Debug.Assert(false, "Selected player is null in PlayerScience_Accept.");
					return;
				}
				if (playerScience > _selectedPlayer.ScienceCost) playerScience = _selectedPlayer.ScienceCost;
				_selectedPlayer.Science = playerScience;
				GameTask.Enqueue(Message.General(TranslateFormatted("{0} science set to {1}~.", _selectedPlayer.TribeName, playerScience)));
			}

			Accept?.Invoke(this, EventArgs.Empty);
			input.Close();
			Destroy();
		}

		private void PlayerScience_Cancel(object? sender, EventArgs args)
		{
			Cancel?.Invoke(this, EventArgs.Empty);
			if (sender is Input input)
			{
				input.Close();
			}
			Destroy();
		}

		protected override bool HasUpdate(uint gameTick)
		{
			if (RefreshNeeded())
			{
				if (_selectedPlayer == null)
					DrawPlayerSelectDialog();
				else
					DrawInputDialog();
			}

			if (_selectedPlayer == null && !_screenQueryService.HasTopScreen<Menu>())
			{
				AddMenu(_civSelectDelegate.Menu);
				return false;
			}
			
			if (_selectedPlayer != null && !HasInput)
			{
				EnsureManagedInput();
			}
			return false;
		}

		private readonly IScreenQueryService _screenQueryService;

		public SetPlayerScience() : base(MouseCursor.Pointer)
		{
			_screenQueryService = ScreenServiceFactory.CreateQueryService();			
			
			Palette = Common.Screens[^1].OriginalColours;
			_civSelectDelegate = new CivSelectMenuDelegate(Palette, Translate("Set Player Science..."));
			_civSelectDelegate.PlayerSelected += OnPlayerSelected;
			_civSelectDelegate.Cancelled += PlayerScience_Cancel;

			DrawPlayerSelectDialog();
		}
	}
}