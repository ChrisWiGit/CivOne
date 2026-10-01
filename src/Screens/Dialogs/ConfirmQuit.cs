// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Collections.Generic;
using CivOne.Graphics;
using CivOne.Services;

namespace CivOne.Screens.Dialogs
{
	internal class ConfirmQuit : BaseDialog
	{
		private const int DialogFontId = 0;

		private readonly ConfirmQuitLayout _layout;
		private readonly Func<byte, byte> _mapColour;
		private readonly bool _useSolidDialogBackground;
		private readonly byte _dialogTextColour;
		private readonly byte _dialogBackgroundColour;
		private readonly byte _menuActiveColour;
		private readonly byte _menuTextColour;
		private readonly byte _selectionActiveColour;
		private readonly byte _selectionShadowColour;

		private byte MapColour(byte colourIndex) => _mapColour(colourIndex);

		private IBitmap SelectionMapped(int left, int top, int width, int height)
		{
			if (_useSolidDialogBackground)
			{
				Picture selection = new(width, height);
				selection
					.FillRectangle(0, 0, width, height, _selectionActiveColour)
					.FillRectangle(0, height - 1, width, 1, _selectionShadowColour)
					.FillRectangle(width - 1, 0, 1, height, _selectionShadowColour);
				return selection;
			}

			return DialogBox[left, top, width, height].ColourReplace((7, _selectionActiveColour), (22, _selectionShadowColour));
		}

		private void MenuQuit(object sender, EventArgs args)
		{
			Runtime.Quit();
			Cancel();
		}

		/// <summary>
		/// Creates the standard quit confirmation dialog using default dialog colors.
		/// </summary>
		public ConfirmQuit() : this(null, null)
		{
		}

		protected override IMenu? CreateManagedMenu()
		{
			Menu menu = new Menu(Palette, SelectionMapped(_layout.MenuOffsetX, _layout.MenuTop, _layout.MenuWidth, _layout.MenuHeight))
			{
				X = _layout.DialogLeft + _layout.MenuOffsetX,
				Y = _layout.DialogTop + _layout.MenuTop,
				CenterTo320Coordinates = true,
				MenuWidth = _layout.MenuWidth,
				ActiveColour = _menuActiveColour,
				TextColour = _menuTextColour,
				FontId = DialogFontId
			};
			foreach (string choice in _layout.MenuItems)
			{
				menu.Items.Add(choice);
			}
			menu.Items[0].Selected += Cancel;
			menu.Items[1].Selected += MenuQuit;

			menu.MissClick += Cancel;
			menu.Cancel += Cancel;
			return menu;
		}

		/// <summary>
		/// Creates the quit confirmation dialog with optional palette and color remapping.
		/// </summary>
		/// <param name="sourcePalette">
		/// Optional palette source used by the dialog.
		/// Pass the active top-screen palette to avoid visible palette switches when opening the dialog as an overlay.
		/// </param>
		/// <param name="colourIndexMap">
		/// Optional index mapper that translates default dialog color indices to context-specific targets.
		/// Use this for screens like Credits, where dialog indices should map to the host menu color scheme.
		/// When omitted, dialog colors are used unchanged.
		/// </param>
		public ConfirmQuit(Palette? sourcePalette = null, Func<byte, byte>? colourIndexMap = null) : this(CreateLayout(), sourcePalette, colourIndexMap)
		{
		}

		/// <summary>
		/// Measures the translated dialog texts and derives the dialog size from them.
		/// </summary>
		/// <returns>The dialog metrics for the current language.</returns>
		private static ConfirmQuitLayout CreateLayout()
		{
			ITranslationService translationService = TranslationServiceFactory.GetCurrent();
			IReadOnlyList<string> questionLines = translationService.TranslateArray("Are you sure you\nwant to Quit?");
			IReadOnlyList<string> menuItems =
			[
				translationService.Translate("Keep Playing"),
				translationService.Translate("Yes, Quit")
			];

			ConfirmQuitLayoutDelegate layoutDelegate = new();
			return layoutDelegate.Calculate(DialogFontId, questionLines, menuItems);
		}

		private ConfirmQuit(ConfirmQuitLayout layout, Palette? sourcePalette, Func<byte, byte>? colourIndexMap)
			: base(layout.DialogLeft, layout.DialogTop, layout.Width, layout.Height, sourcePalette)
		{
			_layout = layout;
			_mapColour = colourIndexMap ?? (colourIndex => colourIndex);
			_useSolidDialogBackground = colourIndexMap != null;
			_dialogTextColour = MapColour(15);
			_dialogBackgroundColour = MapColour(7);
			_menuActiveColour = MapColour(11);
			_menuTextColour = MapColour(5);
			_selectionActiveColour = MapColour(11);
			_selectionShadowColour = MapColour(3);

			if (_useSolidDialogBackground)
			{
				DialogBox.ColourReplace((7, _dialogBackgroundColour), (22, _dialogBackgroundColour));
			}

			for (int i = 0; i < _layout.QuestionLines.Count; i++)
			{
				DialogBox.DrawText(_layout.QuestionLines[i], DialogFontId, _dialogTextColour, _layout.TextIndent, _layout.TextTop + (i * _layout.FontHeight));
			}
		}
	}
}