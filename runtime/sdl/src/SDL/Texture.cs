// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using CivOne.Graphics;
using CivOne.IO;

namespace CivOne
{
	internal static partial class SDL
	{
		internal class Texture : IDisposable
		{
			private static uint SDL_PIXELFORMAT_ABGR8888 => DefinePixelformat(SDL_PixelType.SDL_PIXELTYPE_PACKED32, SDL_PixelOrder.SDL_PACKEDORDER_ABGR, SDL_PixelLayout.SDL_PACKEDLAYOUT_8888, 32, 4);

			private readonly IntPtr _renderer, _handle;

			public int Width { get; private set; }
			public int Height { get; private set; }

			private int[] PaletteArray(Palette palette)
			{
				// Direct managed-buffer fill: removes per-call AllocHGlobal/FreeHGlobal roundtrip
				// and eliminates the leak risk if WriteInt32 throws.
				int[] output = new int[palette.Length];
				for (int i = 0; i < output.Length; i++)
				{
					Colour c = palette[i];
					output[i] = (c.A << 24) | (c.B << 16) | (c.G << 8) | c.R;
				}
				return output;
			}

			private bool HasAlpha(Palette palette) => palette.Entries.Any(x => x.A != 255);

			public bool IsEmpty => (_handle == IntPtr.Zero);

			public void Draw(int x, int y, int width, int height)
			{
				if (IsEmpty) return;

				SDL_Rect dst = new() { X = x, Y = y, W = width, H = height };
				SDL_RenderCopy(_renderer, _handle, ref _rect, ref dst);
			}

            private SDL_Rect _rect;

			internal Texture(IntPtr renderer, Palette palette, Bytemap bytemap)
			{
				if (palette == null || bytemap == null)
				{
					// Do not load empty bitmap
					_handle = IntPtr.Zero;
					return;
				}

				Width = bytemap.Width;
				Height = bytemap.Height;

                _rect = new SDL_Rect {X = 0, Y = 0, W = Width, H = Height};
				_renderer = renderer;
				_handle = SDL_CreateTexture(renderer, SDL_PIXELFORMAT_ABGR8888, SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING, Width, Height);
				SDL_Rect rect = new SDL_Rect() { X = 0, Y = 0, W = Width, H = Height };
				int[] paletteData = PaletteArray(palette);
				//bool hasAlpha = palette.Entries.Any(x => x.A != 255);
				if (HasAlpha(palette)) 
                    SDL_SetTextureBlendMode(_handle, SDL_BlendMode.SDL_BLENDMODE_BLEND);
				if (SDL_LockTexture(_handle, ref rect, out IntPtr pixels, out int pitch) == 0)
				{
					int[] src = bytemap.ToColourMap(paletteData);
					Marshal.Copy(src, 0, pixels, Width * Height);
					SDL_UnlockTexture(_handle);
				}
			}

			public void Dispose()
			{
				if (IsEmpty) return;
				SDL_DestroyTexture(_handle);
			}
		}
	}
}