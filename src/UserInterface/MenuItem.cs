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
using CivOne.Events;

namespace CivOne.UserInterface
{
	#pragma warning disable CA1710

	public class MenuItem<T>
	{
		private MenuItemEventArgs<T?> _args => new(Value);
		private const int MaxDescriptionLines = 3;

		public event MenuItemEventAction<T?>? Selected;
		public event MenuItemEventAction<T?>? RightClick;
		public event MenuItemEventAction<T?>? GetHelp;
		public T? Value { get; private set; }
		public bool Enabled { get; set; }
		public string? Text { get; set; }
		public string[] Description { get; private set; } = [];
		public string? Shortcut { get; set; }
		public string[]? Shortcuts { get; set; }
		public Func<bool>? SelectedCondition { get; set; }

		internal void Select()
		{
			if (!Enabled) return;
			if (Selected == null) return;
			Selected(this, _args);
		}

		internal void Help()
		{
			GetHelp?.Invoke(this, _args);
		}

		internal void Context()
		{
			if (RightClick == null)
			{
				Select();
				return;
			}
			RightClick(this, _args);
		}

		internal static MenuItem<T> Create(string? text, T? value = default)
		{
			return new MenuItem<T>(text, value);
		}
		internal static MenuItem<T> CreateSeparator()
		{
			return new MenuItem<T>(null, default).Disable();
		}

		internal void SetDescription(params string[] description)
		{
			if (description == null || description.Length == 0)
			{
				Description = Array.Empty<string>();
				return;
			}

			List<string> lines = [];
			for (int i = 0; i < description.Length && lines.Count < MaxDescriptionLines; i++)
			{
				string? line = description[i]?.Trim();
				if (!string.IsNullOrWhiteSpace(line))
				{
					lines.Add(line);
				}
			}

			Description = [.. lines];
		}

		protected MenuItem(string? text, T? value = default)
		{
			Enabled = true;
			Text = text;
			Value = value;
		}
	}

	public class DescriptionItem<T> : MenuItem<T>
	{
		internal static DescriptionItem<T> Create(string text, string[] description, T? value = default)
		{
			return new DescriptionItem<T>(text, description, value);
		}

		protected DescriptionItem(string text, string[] description, T? value = default) : base(text, value)
		{
			SetDescription(description);
		}
	}

	public class MenuDescriptionItem<T> : MenuItem<T>
	{
		internal static MenuDescriptionItem<T> Create(string[] description)
		{
			return new MenuDescriptionItem<T>(description);
		}

		private MenuDescriptionItem(string[] description) : base(null)
		{
			SetDescription(description);
			Enabled = false;
		}
	}

	public static class Description<T>
	{
		#pragma warning disable CA1000
		public static MenuItem<T> Create(params string[] description)
		{
			return MenuDescriptionItem<T>.Create(description);
		}
		#pragma warning restore CA1000
	}

	public static class Description
	{
		public static MenuItem<int> Create(params string[] description)
		{
			return MenuDescriptionItem<int>.Create(description);
		}
	}

	public class MenuItem : MenuItem<int>
	{
		protected MenuItem(string text, int value = 0) : base(text, value)
		{
		}
	}

	public class DescriptionItem : DescriptionItem<int>
	{
		protected DescriptionItem(string text, string[] description, int value = 0) : base(text, description, value)
		{
		}
	}
}