// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Linq;
using CivOne.Enums;
using CivOne.IO;

using static CivOne.Enums.CursorType;

namespace CivOne.Graphics.Sprites
{
	public static class Cursor
	{
		private static Resources Resources => Resources.Instance;
		private static Settings Settings => Settings.Instance;

		private static CursorType CursorType => (Settings.CursorType == CursorType.Default && !Resources.Exists("SP257")) ? Builtin : Settings.CursorType;

		private static Bytemap? CursorPointer()
		{
			return CursorType switch
			{
				Default => Resources["SP257"][113, 33, 15, 15].Bitmap,
				Builtin => new Bytemap(11, 16).FromByteArray([
						5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
						5, 5, 0, 0, 0, 0, 0, 0, 0, 0, 0,
						5,15, 5, 0, 0, 0, 0, 0, 0, 0, 0,
						5,15,15, 5, 0, 0, 0, 0, 0, 0, 0,
						5,15,15,15, 5, 0, 0, 0, 0, 0, 0,
						5,15,15,15,15, 5, 0, 0, 0, 0, 0,
						5,15,15,15,15,15, 5, 0, 0, 0, 0,
						5,15,15,15,15,15,15, 5, 0, 0, 0,
						5,15,15,15,15,15,15,15, 5, 0, 0,
						5,15,15,15,15,15,15,15,15, 5, 0,
						5, 5, 5, 5,15,15, 5, 5, 5, 5, 5,
						0, 0, 0, 0, 5,15,15, 5, 0, 0, 0,
						0, 0, 0, 0, 5,15,15, 5, 0, 0, 0,
						0, 0, 0, 0, 0, 5,15,15, 5, 0, 0,
						0, 0, 0, 0, 0, 5,15,15, 5, 0, 0,
						0, 0, 0, 0, 0, 0, 5, 5, 0, 0, 0
						]),
				Native => null,
				_ => throw new NotSupportedException($"Unsupported cursor type: {CursorType}"),
			};
		}

		private static Bytemap? CursorGoto()
		{
			return CursorType switch
			{
				Default => Resources["SP257"][33, 33, 15, 15].Bitmap,
				Builtin => new Bytemap(13, 16).FromByteArray([
						5, 0, 0, 0, 0,15,15, 0, 0, 0,15,15, 0,
						5, 5, 0, 0,15, 0, 0, 0, 0,15, 0, 0,15,
						5,15, 5, 0,15, 0,15,15, 0,15, 0, 0,15,
						5,15,15, 5,15, 0, 0,15, 0,15, 0, 0,15,
						5,15,15,15, 5,15,15, 0, 0, 0,15,15, 0,
						5,15,15,15,15, 5, 0, 0, 0, 0, 0, 0, 0,
						5,15,15,15,15,15, 5, 0, 0, 0, 0, 0, 0,
						5,15,15,15,15,15,15, 5, 0, 0, 0, 0, 0,
						5,15,15,15,15,15,15,15, 5, 0, 0, 0, 0,
						5,15,15,15,15,15,15,15,15, 5, 0, 0, 0,
						5, 5, 5, 5,15,15, 5, 5, 5, 5, 5, 0, 0,
						0, 0, 0, 0, 5,15,15, 5, 0, 0, 0, 0, 0,
						0, 0, 0, 0, 5,15,15, 5, 0, 0, 0, 0, 0,
						0, 0, 0, 0, 0, 5,15,15, 5, 0, 0, 0, 0,
						0, 0, 0, 0, 0, 5,15,15, 5, 0, 0, 0, 0,
						0, 0, 0, 0, 0, 0, 5, 5, 0, 0, 0, 0, 0
						]),
				Native => null,
				_ => throw new NotSupportedException($"Unsupported cursor type: {CursorType}"),
			};
		}

		public readonly static ISprite MousePointer = new CachedSprite(CursorPointer);
		public readonly static ISprite Goto = new CachedSprite(CursorGoto);
		public static ISprite? Current
		{
			get
			{
				return (Common.TopScreen?.Cursor) switch
				{
					MouseCursor.Pointer => MousePointer,
					MouseCursor.Goto => Goto,
					MouseCursor.None => null,
					_ => throw new NotSupportedException($"Unsupported cursor type: {Common.TopScreen?.Cursor}")
				};
			}
		}

		public static void ClearCache()
		{
			foreach (ICached cached in (new[] { MousePointer, Goto }).Cast<ICached>())
			{
				cached.Clear();
			}
		}
	}
}