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
using System.Drawing;
using System.Linq;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.IO;
using CivOne.Graphics.Sprites;
using CivOne.UserInterface;

namespace CivOne.Screens
{
	public class GameMenu : BaseScreen
	{
		private const int DescriptionFontId = 1;
		private const byte DescriptionTextColour = 15;
		private const int DescriptionTopPadding = 6;
		private const int MaxDescriptionLines = 3;

		private readonly MenuItemCollection<int> _items;
		public MenuItemCollection<int> Items => _items;

		/// <summary>
		/// Help text shown at the bottom of the menu for items that do not define their own
		/// <see cref="MenuItem{T}.Description"/>.
		/// </summary>
		public string[] DefaultDescription { get; set; } = [];

		private int _activeItem = -1;
		private bool _update = true;

		private bool _keepOpen;
		public bool KeepOpen
		{
			get
			{
				return _keepOpen;
			}
			set
			{
				_keepOpen = value;
				if (value)
				{
					_activeItem = 0;
				}
			}
		}

		private int ItemWidth(MenuItem<int> menuItem)
		{
			int width = 0;
			if (menuItem != null)
			{
				if (menuItem.Text != null) width += Resources.GetTextSize(0, menuItem.Text).Width;
				string? shortcutText = GetShortcutText(menuItem);
				if (!string.IsNullOrWhiteSpace(shortcutText)) width += Resources.GetTextSize(0, shortcutText).Width + 8;
			}
			return width;
		}

		private int MaxItemWidth => Items.OfType<MenuItem<int>>().Select(ItemWidth).DefaultIfEmpty(0).Max();

		// The description area is sized for the largest description any item (or the default) can show,
		// so the menu keeps a stable size while the user navigates between items.
		private IEnumerable<string> AllDescriptionLines()
		{
			foreach (string line in DefaultDescription)
			{
				yield return line;
			}
			foreach (MenuItem<int> item in Items)
			{
				if (item == null)
				{
					continue;
				}
				foreach (string line in item.Description)
				{
					yield return line;
				}
			}
		}

		private int MaxDescriptionLineCount
		{
			get
			{
				int max = DefaultDescription.Length;
				foreach (MenuItem<int> item in Items)
				{
					if (item != null && item.Description.Length > max)
					{
						max = item.Description.Length;
					}
				}
				return Math.Min(max, MaxDescriptionLines);
			}
		}

		private int MaxDescriptionWidth => AllDescriptionLines()
			.Select(line => Resources.GetTextSize(DescriptionFontId, line).Width)
			.DefaultIfEmpty(0)
			.Max();

		private int DescriptionAreaHeight => MaxDescriptionLineCount > 0
			? DescriptionTopPadding + (MaxDescriptionLineCount * Resources.GetFontHeight(DescriptionFontId))
			: 0;

		private string[] ActiveDescription
		{
			get
			{
				if (_activeItem >= 0 && _activeItem < Items.Count && Items[_activeItem]?.Description.Length > 0)
				{
					return Items[_activeItem].Description;
				}
				return DefaultDescription;
			}
		}

		internal int PixelWidth => Math.Max(MaxItemWidth, MaxDescriptionWidth) + 17;
		internal int PixelHeight => (Resources.GetFontHeight(0) * Items.Count) + 9 + DescriptionAreaHeight;

		private string FormatShortcutToken(string shortcut)
		{
			if (string.IsNullOrWhiteSpace(shortcut))
			{
				return string.Empty;
			}

			string token = shortcut.Trim();
			if (!token.StartsWith('^'))
			{
				return token;
			}

			string keyPart = token[1..].Trim();
			return keyPart.Length == 0 ? Translate("shift") : $"{Translate("shift")}+{keyPart}";
		}

		private string? GetShortcutText(MenuItem<int> menuItem)
		{
			if (menuItem == null)
			{
				return null;
			}

			if (menuItem.Shortcuts != null && menuItem.Shortcuts.Length > 0)
			{
				string[] formatted = menuItem.Shortcuts
					.Where(x => !string.IsNullOrWhiteSpace(x))
					.Select(FormatShortcutToken)
					.Where(x => !string.IsNullOrWhiteSpace(x))
					.ToArray();
				if (formatted.Length > 0)
				{
					return string.Join("/", formatted);
				}
			}

			return string.IsNullOrWhiteSpace(menuItem.Shortcut) ? null : FormatShortcutToken(menuItem.Shortcut);
		}

		private void DrawDescription(int menuWidth, int itemsBottom)
		{
			string[] description = ActiveDescription;
			if (description.Length == 0)
			{
				return;
			}

			int lineHeight = Resources.GetFontHeight(DescriptionFontId);
			int descriptionY = itemsBottom + DescriptionTopPadding;
			for (int i = 0; i < description.Length && i < MaxDescriptionLines; i++)
			{
				this.DrawText(description[i], DescriptionFontId, DescriptionTextColour, menuWidth / 2, descriptionY, TextAlign.Center);
				descriptionY += lineHeight;
			}
		}

		private void MenuItemDraw(MenuItem<int> menuItem, int x, int y)
		{
			if (menuItem == null || menuItem.Text == null) return;
			this.DrawText(menuItem.Text, 0, (byte)(menuItem.Enabled ? 5 : 3), x, y, TextAlign.Left);
			string? shortcutText = GetShortcutText(menuItem);
			if (string.IsNullOrWhiteSpace(shortcutText)) return;
			int textWidth = Resources.GetTextSize(0, menuItem.Text).Width;
			this.DrawText(shortcutText, 0, 15, x + textWidth + 8, y, TextAlign.Left);
		}
		
		protected override bool HasUpdate(uint gameTick)
		{
			if (!_update) return true;
			
			int ww = PixelWidth;
			int hh = PixelHeight;
			
			Bitmap = new Bytemap(ww, hh);
			this.Tile(Pattern.PanelGrey, 1, 1)
				.DrawRectangle()
				.DrawRectangle3D(1, 1, ww - 2, hh - 2)
				.As<Picture>();
			
			int i = 0;
			int yy = 5;
			foreach (MenuItem<int> menuItem in Items)
			{
				if (i == _activeItem)
				{
					this.ColourReplace(7, 11, 3, yy - 1, MaxItemWidth + 11, Resources.GetFontHeight(0))
						.ColourReplace(22, 3, 3, yy - 1, MaxItemWidth + 11, Resources.GetFontHeight(0));
				}
				MenuItemDraw(menuItem, 11, yy);
				yy += Resources.GetFontHeight(0);
				i++;
			}

			DrawDescription(ww, yy);

			_update = false;
			return true;
		}
		
		public override bool KeyDown(KeyboardEventArgs args)
		{
			switch (args.Key)
			{
				case Key.NumPad8:
				case Key.Up:
					if (Items.Count > 0)
					{
						_activeItem = (_activeItem <= 0) ? (Items.Count - 1) : (_activeItem - 1);
						_update = true;
					}
					return true;
				case Key.NumPad2:
				case Key.Down:
					if (Items.Count > 0)
					{
						_activeItem = (_activeItem >= (Items.Count - 1)) ? 0 : (_activeItem + 1);
						_update = true;
					}
					return true;
				case Key.Escape:
					KeepOpen = false;
					return false;
				case Key.Enter:
					if (_activeItem < 0 || _activeItem >= Items.Count)
					{
						return true;
					}
					if (Items[_activeItem]?.Enabled == false)
					{
						return true;
					}
					Items[_activeItem]?.Select();
					return false;
			}
			return true;
		}
		
		private int MouseOverItem(ScreenEventArgs args)
		{
			int fontHeight = Resources.GetFontHeight(0);
			int yy = 5;
			
			for (int i = 0; i < Items.Count; i++)
			{
				if (new Rectangle(3, yy, MaxItemWidth + 8, fontHeight).Contains(args.Location)) return i;
				yy += fontHeight;
			}
			
			return -1;
		}
		
		public override bool MouseDrag(ScreenEventArgs args)
		{
			if (KeepOpen) return false;
			int index = MouseOverItem(args);
			if (index == _activeItem) return false;
						
			_activeItem = index;
			
			_update = true;
			return true;
		}
		
		public override bool MouseDown(ScreenEventArgs args)
		{
			if (!KeepOpen) return false;
			int index = MouseOverItem(args);
			if (index == _activeItem) return false;
						
			_activeItem = index;
			
			_update = true;
			return true;
		}
		
		public override bool MouseUp(ScreenEventArgs args)
		{
			if (_activeItem < 0 && !KeepOpen) return false;
			if (_activeItem < 0 && KeepOpen)
			{
				KeepOpen = false;
				return true;
			}
			if (Items[_activeItem]?.Enabled == false)
			{
				return false;
			}
			Items[_activeItem]?.Select();
			
			return true;
		}
		
		public GameMenu(string menuId, Palette palette) : base(8, 8)
		{
			_items = new MenuItemCollection<int>(menuId);
			
			Palette = palette.Copy();
		}
	}
}