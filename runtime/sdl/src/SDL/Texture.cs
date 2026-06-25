// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using CivOne.Graphics;
using CivOne.IO;

namespace CivOne
{
	#pragma warning disable S101 // Types should be named in PascalCase - but these are named to match SDL as a name.
	internal static partial class SDL
	{
		internal sealed class Texture : IDisposable
		{
			private static uint SDL_PIXELFORMAT_ABGR8888 => DefinePixelformat(SDL_PixelType.SDL_PIXELTYPE_PACKED32, SDL_PixelOrder.SDL_PACKEDORDER_ABGR, SDL_PixelLayout.SDL_PACKEDLAYOUT_8888, 32, 4);

			private readonly IntPtr _renderer;
			private IntPtr _handle;
			private bool _disposed;

			// Reusable buffers — keep them alive for the lifetime of the texture so the render
			// hot-path (60 FPS × ~3 layers) does not allocate ~256 KB / frame / layer.
			private int[]? _paletteCache;
			private int[]? _colourBuffer;
			private byte[]? _byteBuffer;
			private bool _alphaBlendApplied;

			public int Width { get; private set; }
			public int Height { get; private set; }

			private static int[] PaletteArray(Palette palette)
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

			private static bool HasAlpha(Palette palette) => palette.Entries.Any(x => x.A != 255);

			public bool IsEmpty => _handle == IntPtr.Zero;

			private void DestroyTextureHandle()
			{
				if (_handle == IntPtr.Zero) return;
				IntPtr handle = _handle;
				_handle = IntPtr.Zero;
				SDL_DestroyTexture(handle);
			}

			public void Draw(int x, int y, int width, int height)
			{
				if (IsEmpty) return;

				SDL_Rect dst = new() { X = x, Y = y, W = width, H = height };
				int result = SDL_RenderCopy(_renderer, _handle, ref _rect, ref dst);
				if (result != 0)
				{
					var error = GetSdlErrorMessage();
					Debug.Assert(false, $"SDL_RenderCopy failed: {error}");
					Console.Error.WriteLine($"SDL_RenderCopy failed: {error}");
				}
			}

			private SDL_Rect _rect;

			~Texture() => Dispose(disposing: false);

			internal Texture(IntPtr? renderer, Palette? palette, Bytemap? bytemap)
			{
				if (renderer == null || renderer == IntPtr.Zero || palette == null || palette == Palette.Empty || palette.Length == 0 || bytemap == null)
				{
					// Do not load empty bitmap
					_handle = IntPtr.Zero;
					return;
				}

				Width = bytemap.Width;
				Height = bytemap.Height;

				_rect = new SDL_Rect { X = 0, Y = 0, W = Width, H = Height };
				_renderer = renderer.Value;
				_handle = SDL_CreateTexture(renderer.Value, SDL_PIXELFORMAT_ABGR8888, SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING, Width, Height);
				if (_handle == IntPtr.Zero)
				{
					return;
				}

				SDL_Rect rect = new() { X = 0, Y = 0, W = Width, H = Height };
				int[] paletteData = PaletteArray(palette);
				if (HasAlpha(palette) && SDL_SetTextureBlendMode(_handle, SDL_BlendMode.SDL_BLENDMODE_BLEND) == 0)
				{
					// Only flag as applied on success; non-zero indicates SDL refused the blend mode
					// (e.g. out-of-memory or driver state) and we want to retry on the next update.
					_alphaBlendApplied = true;
				}
				if (SDL_LockTexture(_handle, ref rect, out IntPtr pixels, out _) == 0)
				{
					int[] src = bytemap.ToColourMap(paletteData);
					Marshal.Copy(src, 0, pixels, Width * Height);
					SDL_UnlockTexture(_handle);
				}
				else
				{
					// Constructor failed to initialize the texture payload; release native handle.
					DestroyTextureHandle();
				}
			}

			/// <summary>
			/// Creates an empty streaming texture of the given size, intended to be filled
			/// repeatedly via <see cref="UpdateFrom"/> from the render loop.
			///
			/// Replaces the previous pattern of allocating and destroying a fresh SDL texture
			/// per layer per frame (the dominant GPU-side allocation hotspot).
			/// </summary>
			internal Texture(IntPtr renderer, int width, int height)
			{
				if (renderer == IntPtr.Zero || width <= 0 || height <= 0)
				{
					_handle = IntPtr.Zero;
					return;
				}

				Width = width;
				Height = height;
				_rect = new SDL_Rect { X = 0, Y = 0, W = Width, H = Height };
				_renderer = renderer;
				_handle = SDL_CreateTexture(renderer, SDL_PIXELFORMAT_ABGR8888, SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING, Width, Height);
			}

			/// <summary>
			/// Refills the texture with the current palette + bytemap contents.
			///
			/// Dimensions of <paramref name="bytemap"/> must match the texture; mismatched calls
			/// are silently ignored so the caller (cache owner) can dispose and recreate.
			/// </summary>
			public void UpdateFrom(Palette palette, Bytemap bytemap)
			{
				if (IsEmpty || palette == null || bytemap == null) return;
				if (bytemap.Width != Width || bytemap.Height != Height) return;

				int paletteLen = palette.Length;
				if (_paletteCache == null || _paletteCache.Length != paletteLen) _paletteCache = new int[paletteLen];
				for (int i = 0; i < paletteLen; i++)
				{
					Colour c = palette[i];
					_paletteCache[i] = (c.A << 24) | (c.B << 16) | (c.G << 8) | c.R;
				}

				if (!_alphaBlendApplied && HasAlpha(palette) && 
					SDL_SetTextureBlendMode(_handle, SDL_BlendMode.SDL_BLENDMODE_BLEND) == 0)
				{
					// Only flag as applied on success; on failure we retry next frame instead of
					// silently rendering opaque pixels until disposal.
					_alphaBlendApplied = true;
				}

				int pixelCount = Width * Height;
				if (_colourBuffer == null || _colourBuffer.Length < pixelCount) _colourBuffer = new int[pixelCount];
				if (_byteBuffer == null || _byteBuffer.Length < pixelCount) _byteBuffer = new byte[pixelCount];

				bytemap.CopyTo(_byteBuffer);
				for (int i = 0; i < pixelCount; i++)
				{
					_colourBuffer[i] = _paletteCache[_byteBuffer[i]];
				}

				SDL_Rect rect = new() { X = 0, Y = 0, W = Width, H = Height };
				if (SDL_LockTexture(_handle, ref rect, out IntPtr pixels, out _) == 0)
				{
					Marshal.Copy(_colourBuffer, 0, pixels, pixelCount);
					SDL_UnlockTexture(_handle);
				}
			}

			public void Dispose()
			{
				Dispose(disposing: true);
				GC.SuppressFinalize(this);
			}

			public void Dispose(bool disposing)
			{
				if (_disposed) return;

				if (disposing)
				{
					_paletteCache = null;
					_colourBuffer = null;
					_byteBuffer = null;
				}

				DestroyTextureHandle();
				_disposed = true;
			}
		}
	}
}