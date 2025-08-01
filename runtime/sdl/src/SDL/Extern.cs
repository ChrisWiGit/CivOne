// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace CivOne
{
	internal static partial class SDL
	{
#if MACOS
		private const string DLL_SDL = "/Library/Frameworks/SDL2.framework/Versions/Current/SDL2";
#elif LINUX
		private const string DLL_SDL = "libSDL2-2.0.so.0";
#else
		private const string DLL_SDL = "SDL2";
#endif

		private static byte[] ToBytes(this string input) => Encoding.UTF8.GetBytes($"{input}{'\0'}");

		[DllImportAttribute(DLL_SDL, CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr SDL_CreateRenderer(IntPtr window, int index, SDL_RENDERER_FLAGS flags);

		[DllImportAttribute(DLL_SDL, CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr SDL_CreateWindow(byte[] title, int x, int y, int w, int h, SDL_WINDOW flags);

		private static IntPtr SDL_CreateWindow(string title, int x, int y, int w, int h, SDL_WINDOW flags) => SDL_CreateWindow(title.ToBytes(), x, y, w, h, flags);

		[DllImportAttribute(DLL_SDL, CallingConvention = CallingConvention.Cdecl)]
		private static extern void SDL_SetWindowTitle(IntPtr window, byte[] title);

		private static void SDL_SetWindowTitle(IntPtr window, string title) => SDL_SetWindowTitle(window, title.ToBytes());

		[DllImport(DLL_SDL, CallingConvention = CallingConvention.Cdecl)]
		private static extern void SDL_SetWindowIcon(IntPtr window, IntPtr icon);

		[DllImportAttribute(DLL_SDL, CallingConvention = CallingConvention.Cdecl)]
		private static extern void SDL_GetWindowSize(IntPtr window, out int width, out int height);

		[DllImportAttribute(DLL_SDL, CallingConvention = CallingConvention.Cdecl)]
		private static extern void SDL_SetWindowSize(IntPtr window, int width, int height);

		[DllImportAttribute(DLL_SDL, CallingConvention = CallingConvention.Cdecl)]
		private static extern void SDL_SetWindowFullscreen(IntPtr window, SDL_WINDOW flags);

		[DllImportAttribute(DLL_SDL, CallingConvention = CallingConvention.Cdecl)]
		private static extern void SDL_Delay(uint ms);

		[DllImportAttribute(DLL_SDL, CallingConvention = CallingConvention.Cdecl)]
		private static extern void SDL_DestroyRenderer(IntPtr renderer);

		[DllImportAttribute(DLL_SDL, CallingConvention = CallingConvention.Cdecl)]
		private static extern void SDL_DestroyWindow(IntPtr handle);

		[DllImportAttribute(DLL_SDL, CallingConvention = CallingConvention.Cdecl)]
		private static extern void SDL_ShowCursor(int toggle);

		[DllImportAttribute(DLL_SDL, CallingConvention = CallingConvention.Cdecl)]
		private static extern uint SDL_GetMouseState(out int x, out int y);

		[DllImportAttribute(DLL_SDL, CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr SDL_Init(SDL_INIT flags);

		[DllImportAttribute(DLL_SDL, CallingConvention = CallingConvention.Cdecl)]
		private static extern int SDL_PollEvent(out SDL_Event sdlEvent);

		[DllImportAttribute(DLL_SDL, CallingConvention = CallingConvention.Cdecl)]
		private static extern void SDL_Quit();

		[DllImportAttribute(DLL_SDL, CallingConvention = CallingConvention.Cdecl)]
		private static extern int SDL_RenderClear(IntPtr renderer);

		[DllImportAttribute(DLL_SDL, CallingConvention = CallingConvention.Cdecl)]
		private static extern int SDL_RenderCopy(IntPtr renderer, IntPtr texture, ref SDL_Rect srcrect, ref SDL_Rect dstrect);

		[DllImportAttribute(DLL_SDL, CallingConvention = CallingConvention.Cdecl)]
		private static extern void SDL_RenderPresent(IntPtr renderer);

		[DllImportAttribute(DLL_SDL, CallingConvention = CallingConvention.Cdecl)]
		private static extern int SDL_SetRenderDrawColor(IntPtr renderer, byte r, byte g, byte b, byte a);

		[DllImportAttribute(DLL_SDL, CallingConvention = CallingConvention.Cdecl)]
		private static extern int SDL_SetHint(byte[] name, byte[] value);

		private static bool SDL_SetHint(string name, string value) => SDL_SetHint(name.ToBytes(), value.ToBytes()) == 1;

		[DllImportAttribute(DLL_SDL, CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr SDL_CreateTexture(IntPtr renderer, uint format, SDL_TextureAccess access, int width, int height);

		[DllImportAttribute(DLL_SDL, CallingConvention = CallingConvention.Cdecl)]
		private static extern void SDL_DestroyTexture(IntPtr texture);

		[DllImportAttribute(DLL_SDL, CallingConvention = CallingConvention.Cdecl)]
		private static extern int SDL_SetTextureBlendMode(IntPtr texture, SDL_BlendMode blendMode);

		[DllImportAttribute(DLL_SDL, CallingConvention = CallingConvention.Cdecl)]
		private static extern int SDL_LockTexture(IntPtr texture, ref SDL_Rect rect, out IntPtr pixels, out int pitch);

		[DllImportAttribute(DLL_SDL, CallingConvention = CallingConvention.Cdecl)]
		private static extern void SDL_UnlockTexture(IntPtr texture);

		[DllImport(DLL_SDL, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SDL_CreateRGBSurfaceFrom(IntPtr pixels, int width, int height, int depth, int pitch, uint Rmask, uint Gmask, uint Bmask, uint Amask);

		[DllImport(DLL_SDL, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_FreeSurface(IntPtr surface);

		[DllImportAttribute(DLL_SDL, CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr SDL_Window();

		[DllImport(DLL_SDL, CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr SDL_RWFromFile(byte[] file, byte[] mode);

		[DllImportAttribute(DLL_SDL, CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr SDL_LoadWAV_RW(IntPtr source, int freeSource, ref SDL_AudioSpec specs, out IntPtr buffer, out uint length);

		private static IntPtr SDL_LoadWAV_RW(string filename, int freeSource, ref SDL_AudioSpec specs, out IntPtr buffer, out uint length) => SDL_LoadWAV_RW(SDL_RWFromFile(filename.ToBytes(), "rb".ToBytes()), freeSource, ref specs, out buffer, out length);

		// const char* SDL_GetError(void);
		[DllImportAttribute(DLL_SDL, CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr SDL_GetError();

		[DllImport(DLL_SDL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		private static extern IntPtr SDL_GetErrorMsg(
			StringBuilder errstr,
			int maxlen
		);


		public static string GetSdlErrorMessage()
		{
			const int bufferSize = 1024;
			StringBuilder buffer = new StringBuilder(bufferSize);
			SDL_GetErrorMsg(buffer, bufferSize);
			return buffer.ToString();
		}


		[DllImport(DLL_SDL, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_FreeWAV(IntPtr buffer);

		[DllImportAttribute(DLL_SDL, CallingConvention = CallingConvention.Cdecl)]
		private static extern uint SDL_GetQueuedAudioSize(uint device);

		[DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
		private static extern uint SDL_OpenAudioDevice(
			[MarshalAs(UnmanagedType.LPStr)] string device,
			int iscapture,
			ref SDL_AudioSpec desired,
			out SDL_AudioSpec obtained,
			int allowed_changes
		);

		[DllImport(DLL_SDL, CallingConvention = CallingConvention.Cdecl)]
		private static extern int SDL_OpenAudio(ref SDL_AudioSpec desired, out SDL_AudioSpec obtained);

		[DllImportAttribute(DLL_SDL, CallingConvention = CallingConvention.Cdecl)]
		private static extern int SDL_QueueAudio(uint deviceId, IntPtr data, uint length);

		[DllImport(DLL_SDL, CallingConvention = CallingConvention.Cdecl)]
		private static extern void SDL_PauseAudio(int pauseOn);

		[DllImport(DLL_SDL, CallingConvention = CallingConvention.Cdecl)]
		private static extern void SDL_CloseAudio();

		[DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
		private static extern void SDL_CloseAudioDevice(uint dev);

		[DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
		private static extern void SDL_PauseAudioDevice(uint dev, int pause_on);
	}
}