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
using CivOne.UserInterface;

namespace CivOne.Screens
{
	/// <summary>
	/// Generic menu UI component used throughout the UI to display selectable items.
	/// </summary>
	/// <typeparam name="T">Menu item payload type (commonly an enum or integer).</typeparam>
	[ScreenResizeable]
	public class Menu<T> : BaseScreen, IMenu
	{
		private const int DescriptionFontId = 0;
		private const int DescriptionTopPadding = 10;
		private const byte DescriptionTextColour = 1;

		private readonly Picture _background;
		
		public event EventHandler Cancel;
		public event EventHandler MissClick;
		
		public readonly MenuItemCollection<T> Items;

		public string Id => Items.Id;

		public string Title { get; set; }
		public int FontId { get; set; }
		public int X { get; set; }
		public int Y { get; set; }
		public int MenuWidth { get; set; }
		public int Indent { get; set; }
		public byte TitleColour { get; set; }
		public byte ActiveColour { get; set; }
		public byte TextColour { get; set; }
		public byte DisabledColour { get; set; }
		public int IndentTitle { get; set; }
		public int RowHeight { get; set; }
		public bool CenterTo320Coordinates { get; set; }
		public string[] DefaultDescription { get; set; } = [];
		
		/// <summary>
		/// Optional action to call when Shift+F1 is pressed while the menu is active.
		/// </summary>
		public Action OnShiftF1 { get; set; }

		private int CoordinateOffsetX => CenterTo320Coordinates ? Math.Max(0, (Width - 320) / 2) : 0;
		private int CoordinateOffsetY => CenterTo320Coordinates ? Math.Max(0, (Height - 200) / 2) : 0;
		
		private bool _mouseDown = false;
		private bool _change = true;
		private int _activeItem = 0;
		public int ActiveItem
		{
			get
			{
				return _activeItem;
			}
			set
			{
				_change = true;
				_activeItem = value;
				if (_activeItem < 0) _activeItem = 0;
				if (_activeItem >= Items.Count) _activeItem = (Items.Count - 1);
			}
		}

		public MenuItem<T> SelectedItem => Items[ActiveItem];

		private void SelectDefault(object sender, EventArgs args)
		{
			for (int i = 0; i < Items.Count; i++)
			{
				MenuItem<T> item = Items[i];
				if (item.SelectedCondition == null || !item.SelectedCondition()) continue;
				ActiveItem = i;
				return;
			}
		}
		
		protected override bool HasUpdate(uint gameTick)
		{
			int fontHeight = Resources.GetFontHeight(FontId);
			if (RowHeight != 0) fontHeight = RowHeight;
			if (_change)
			{
				int x = X + CoordinateOffsetX;
				int y = Y + CoordinateOffsetY;
				int yy = y + (_activeItem * fontHeight);
				int offsetY = 0;

				this.Clear();
				if (Title != null)
				{
					this.DrawText(Title, FontId, TitleColour, x + IndentTitle, y + 1);
					offsetY = fontHeight;
				}
				if (_activeItem >= 0)
				{
					if (_background == null)
					{
						this.FillRectangle(x, yy + offsetY, MenuWidth, fontHeight, ActiveColour);
					}
					else
					{
						this.AddLayer(_background[0, (_activeItem * fontHeight) + offsetY, MenuWidth, fontHeight], x, yy + offsetY, dispose: true);
					}
				}
				for (int i = 0; i < Items.Count; i++)
				{
					yy = y + (i * fontHeight) + offsetY;
					this.DrawText(Items[i].Text, FontId, (byte)(Items[i].Enabled ? TextColour : DisabledColour), x + Indent, yy + 1);
				}

				DrawDescription(fontHeight, x, y, offsetY);
				_change = false;
				return true;
			}
			return false;
		}

		private void DrawDescription(int fontHeight, int x, int y, int offsetY)
		{
			if (_activeItem >= 0 && _activeItem < Items.Count)
			{
				string[] description = Items[_activeItem].Description;
				if (description.Length == 0)
				{
					description = DefaultDescription;
				}
				if (description.Length > 0)
				{
					// Lower and center description text within the menu, with a minimum of 
					// DescriptionTopPadding pixels between the active item and the description
					int descriptionLineHeight = Resources.GetFontHeight(DescriptionFontId);
					int descriptionHeight = description.Length * descriptionLineHeight;
					int descriptionTop = y + offsetY + (Items.Count * fontHeight) + DescriptionTopPadding;
					int availableHeight = Math.Max(0, Height - descriptionTop - CoordinateOffsetY);
					int descriptionY = descriptionTop + Math.Max(0, (availableHeight - descriptionHeight) / 2);
					for (int i = 0; i < description.Length; i++)
					{
						this.DrawText(description[i], DescriptionFontId, DescriptionTextColour, x + (MenuWidth / 2), descriptionY, TextAlign.Center);
						descriptionY += descriptionLineHeight;
					}
				}
			}
		}

		public override bool KeyDown(KeyboardEventArgs args)
		{
			if (args.Shift && args.Key == Key.F1)
			{
				// CW: Allow opening the setup screen from anywhere in the credits menu.
				// Previously, the setup screen was only accessible before the credits menu
				// had been shown for the first time, due to how the credits animation
				// was initialized. This makes the game settings consistently accessible
				// throughout the entire credits menu. Other menus do not use this behavior.
				OnShiftF1?.Invoke();
				return true;
			}

			switch (args.Key)
			{
				case Key.NumPad8:
				case Key.Up:
					if (ActiveItem == 0)
					{
						ActiveItem = Items.Count - 1;
					}
					else
					{
						ActiveItem--;
					}
					return true;
				case Key.NumPad2:
				case Key.Down:
					if (ActiveItem == Items.Count - 1)
					{
						ActiveItem = 0;
					}
					else
					{
						ActiveItem++;
					}
					return true;
				case Key.Enter:
					if (!Items[_activeItem].Enabled) return false;
					Items[_activeItem].Select();
					return true;
				case Key.Escape:
					if (Cancel != null)
					{
						_activeItem = -1;
						_change = true;
						HasUpdate(0);
						Cancel(this, null);
					}
					break;
			}

			if (args.Alt && (args.KeyChar == 'h' || args.KeyChar == 'H'))
			{
				Items[_activeItem].Help();
				return true;
			}

			SelectNextMenuItemByChar(args.KeyChar);

			return false;
		}
		
		private void SelectNextMenuItemByChar(char inputChar)
		{
			if (Items == null || Items.Count == 0)
			{
				return;
			}

			char key = char.ToLower(inputChar);

			var matchingIndices = Enumerable.Range(0, Items.Count)
				.Select(i => (Index: (ActiveItem + 1 + i) % Items.Count, Text: Items[(ActiveItem + 1 + i) % Items.Count].Text))
				.Where(t => !string.IsNullOrEmpty(t.Text) && char.ToLower(t.Text[0]) == key)
				.Select(t => t.Index)
				.ToList();

			if (matchingIndices.Any())
			{
				ActiveItem = matchingIndices.First();
			}
		}

		
		private int MouseOverItem(ScreenEventArgs args)
		{
			int fontHeight = Resources.GetFontHeight(FontId);
			if (RowHeight != 0) fontHeight = RowHeight;
			int x = X + CoordinateOffsetX;
			int yy = Y + CoordinateOffsetY;

			if (Title != null) yy += fontHeight;
			for (int i = 0; i < Items.Count; i++)
			{
				if (new Rectangle(x, yy, MenuWidth, fontHeight).Contains(args.Location)) return i;
				yy += fontHeight;
			}

			return -1;
		}
		
		public override bool MouseDown(ScreenEventArgs args)
		{
			_mouseDown = true;

			int index = MouseOverItem(args);
			if (index < 0 && MissClick != null)
			{
				MissClick(this, null);
				return true;
			}
			if (index < 0 || index == _activeItem) return false;
			ActiveItem = index;
			_change = true;
			return true;
		}
		
		public override bool MouseUp(ScreenEventArgs args)
		{
			if (!_mouseDown) return true;

			int index = MouseOverItem(args);
			if (index < 0) return false;
			ActiveItem = index;
			if (Items[_activeItem].Enabled)
			{
				if ((args.Buttons & MouseButton.Right) > 0)
				{
					_mouseDown = false;
					Items[_activeItem].Context();
				}
				else
				{
					Items[_activeItem].Select();
				}
			}
			_change = true;
			return true;
		}
		
		public override bool MouseDrag(ScreenEventArgs args)
		{
			int index = MouseOverItem(args);
			if (index < 0 || index == _activeItem) return false;
			ActiveItem = index;
			_change = true;
			return true;
		}

		public void ForceUpdate()
		{
			_change = true;
			HasUpdate(0);
		}

		private void Resize(object sender, ResizeEventArgs args) => ForceUpdate();
		
		public void Close()
		{
			Destroy();
		}
		
		public Menu(string menuId, Palette palette, IBitmap background = null) : base(MouseCursor.Pointer)
		{
			OnResize += Resize;

			Items = new MenuItemCollection<T>(menuId);
			Items.ItemsChanged += SelectDefault;

			if (background != null)
			{
				_background = new Picture(background);
			}

			IndentTitle = 8;
			Indent = 8;
			
			Palette = palette.Copy();
		}

		public override void Dispose()
		{
			_background?.Dispose();
			base.Dispose();
		}
	}

	/// <summary>
	/// Non-generic convenience `Menu` specialized for `int` menu identifiers.
	/// </summary>
	public class Menu : Menu<int>
	{
		public Menu(Palette palette, IBitmap background = null) : base(null, palette, background)
		{
		}

		public Menu(string menuId, Palette palette, IBitmap background = null) : base(menuId, palette, background)
		{
		}
	}
}