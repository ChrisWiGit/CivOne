# SDL Layer Analysis (`runtime/sdl/src/SDL`)

## 1. What this folder does

This folder is a thin SDL2 bridge for the CivOne runtime.
It has one core type (`SDL.Window`) and several helper types around it.

High-level responsibilities:

- Create and manage native SDL window/renderer.
- Run the main event loop (`Run`).
- Convert SDL keyboard/mouse/window events into CivOne events.
- Convert CivOne bitmap/palette data to SDL textures.
- Load and play WAV sounds through SDL audio device queue.
- Hold low-level interop types (`enum`, `struct`, `delegate`) for P/Invoke.

---

## 2. Beginner entry point (recommended reading order)

If you are new, start in this order:

1. [runtime/sdl/src/SDL/Window.cs](runtime/sdl/src/SDL/Window.cs)
   - Understand lifecycle: init, loop, draw, dispose.
2. [runtime/sdl/src/SDL/Window.KeyboardEvent.cs](runtime/sdl/src/SDL/Window.KeyboardEvent.cs)
   - Understand input normalization (`SDL` -> `CivOne.Events`).
3. [runtime/sdl/src/SDL/Texture.cs](runtime/sdl/src/SDL/Texture.cs)
   - Understand rendering data path (palette/bytemap -> GPU texture).
4. [runtime/sdl/src/SDL/Wave.cs](runtime/sdl/src/SDL/Wave.cs)
   - Understand one-shot sound playback.
5. [runtime/sdl/src/SDL/Extern.cs](runtime/sdl/src/SDL/Extern.cs)
   - Understand where all native function imports live.

You can safely ignore most files in `Enums`, `Flags`, `Structs`, `Delegates` until you need to change interop.

---

## 3. Quick architecture map

- `SDL` (`static partial`) is split across files.
- `SDL.Window` (`abstract partial`) is split across multiple files too.
- Core loop lives in `Window.cs` and delegates specialized behavior to partials:
  - keyboard: `Window.KeyboardEvent.cs`
  - mouse: `Window.MouseEvent.cs`
  - sound: `Window.Sound.cs`
  - window state events: `Window.WindowEvent.cs`

Runtime flow:

1. `Window` constructor calls `SDL_Init`, creates window, creates renderer.
2. `Run()` enters event loop.
3. Every tick:
   - poll SDL event
   - dispatch to handlers
   - call `OnUpdate` and `OnDraw`
   - process mouse and sound
   - present frame only if redraw requested
4. `Dispose()` tears down renderer/window and calls `SDL_Quit`.

---

## 4. Class/type overview table

| Type | Short description | Source |
| --- | --- | --- |
| `SDL` (partial) | Main SDL interop container (`DllImport`, helper methods, nested interop types). | [runtime/sdl/src/SDL/Extern.cs](runtime/sdl/src/SDL/Extern.cs), [runtime/sdl/src/SDL/Helper.cs](runtime/sdl/src/SDL/Helper.cs), [runtime/sdl/src/SDL/Window.cs](runtime/sdl/src/SDL/Window.cs), [runtime/sdl/src/SDL/Texture.cs](runtime/sdl/src/SDL/Texture.cs), [runtime/sdl/src/SDL/Wave.cs](runtime/sdl/src/SDL/Wave.cs) |
| `SDL.Window` (abstract partial) | Window lifecycle, event loop, drawing trigger, pause/fullscreen handling, event bridge. | [runtime/sdl/src/SDL/Window.cs](runtime/sdl/src/SDL/Window.cs), [runtime/sdl/src/SDL/Window.KeyboardEvent.cs](runtime/sdl/src/SDL/Window.KeyboardEvent.cs), [runtime/sdl/src/SDL/Window.MouseEvent.cs](runtime/sdl/src/SDL/Window.MouseEvent.cs), [runtime/sdl/src/SDL/Window.Sound.cs](runtime/sdl/src/SDL/Window.Sound.cs), [runtime/sdl/src/SDL/Window.WindowEvent.cs](runtime/sdl/src/SDL/Window.WindowEvent.cs) |
| `SDL.Texture` | Converts palette + bytemap into streaming SDL texture and draws it. | [runtime/sdl/src/SDL/Texture.cs](runtime/sdl/src/SDL/Texture.cs) |
| `TextureExtensions` | Null-safe extension wrapper for `Texture.Draw(...)`. | [runtime/sdl/src/SDL/TextureExtensions.cs](runtime/sdl/src/SDL/TextureExtensions.cs) |
| `SDL.Wave` | Loads WAV file, queues it to SDL audio device, tracks playback completion. | [runtime/sdl/src/SDL/Wave.cs](runtime/sdl/src/SDL/Wave.cs) |
| `SDL_AudioCallback` (`delegate`) | Callback signature for SDL audio API compatibility. | [runtime/sdl/src/SDL/Delegates/SDL_AudioCallback.cs](runtime/sdl/src/SDL/Delegates/SDL_AudioCallback.cs) |
| `SDL_AudioSpec` (`struct`) | Managed representation of SDL audio format/device configuration. | [runtime/sdl/src/SDL/Structs/SDL_AudioSpec.cs](runtime/sdl/src/SDL/Structs/SDL_AudioSpec.cs) |
| `SDL_Event`, `SDL_WindowEvent`, `SDL_KeyboardEvent` (`struct`) | Raw event payload layouts for event polling and casting. | [runtime/sdl/src/SDL/Structs/SDL_Event.cs](runtime/sdl/src/SDL/Structs/SDL_Event.cs) |
| `SDL_Keysym` (`struct`) | Keyboard symbol data (scancode, keycode, modifier). | [runtime/sdl/src/SDL/Structs/SDL_Keysym.cs](runtime/sdl/src/SDL/Structs/SDL_Keysym.cs) |
| `SDL_Rect` (`struct`) | Rectangle used by texture lock/copy operations. | [runtime/sdl/src/SDL/Structs/SDL_Rect.cs](runtime/sdl/src/SDL/Structs/SDL_Rect.cs) |
| `SDL_*` enums/flags | Constants for event types, key states, scancodes, pixel formats, window/renderer flags. | [runtime/sdl/src/SDL/Enums/SDL_EventType.cs](runtime/sdl/src/SDL/Enums/SDL_EventType.cs), [runtime/sdl/src/SDL/Enums/SDL_KeyState.cs](runtime/sdl/src/SDL/Enums/SDL_KeyState.cs), [runtime/sdl/src/SDL/Enums/SDL_Scancode.cs](runtime/sdl/src/SDL/Enums/SDL_Scancode.cs), [runtime/sdl/src/SDL/Enums/SDL_WindowEventID.cs](runtime/sdl/src/SDL/Enums/SDL_WindowEventID.cs), [runtime/sdl/src/SDL/Enums/SDL_TextureAccess.cs](runtime/sdl/src/SDL/Enums/SDL_TextureAccess.cs), [runtime/sdl/src/SDL/Enums/SDL_BlendMode.cs](runtime/sdl/src/SDL/Enums/SDL_BlendMode.cs), [runtime/sdl/src/SDL/Enums/SDL_PixelType.cs](runtime/sdl/src/SDL/Enums/SDL_PixelType.cs), [runtime/sdl/src/SDL/Enums/SDL_PixelOrder.cs](runtime/sdl/src/SDL/Enums/SDL_PixelOrder.cs), [runtime/sdl/src/SDL/Enums/SDL_PixelLayout.cs](runtime/sdl/src/SDL/Enums/SDL_PixelLayout.cs), [runtime/sdl/src/SDL/Enums/SDL_PixelFormatEnum.cs](runtime/sdl/src/SDL/Enums/SDL_PixelFormatEnum.cs), [runtime/sdl/src/SDL/Enums/SDL_Keycode.cs](runtime/sdl/src/SDL/Enums/SDL_Keycode.cs), [runtime/sdl/src/SDL/Enums/SDL_AudioFormat.cs](runtime/sdl/src/SDL/Enums/SDL_AudioFormat.cs), [runtime/sdl/src/SDL/Flags/SDL_INIT.cs](runtime/sdl/src/SDL/Flags/SDL_INIT.cs), [runtime/sdl/src/SDL/Flags/SDL_KMOD.cs](runtime/sdl/src/SDL/Flags/SDL_KMOD.cs), [runtime/sdl/src/SDL/Flags/SDL_WINDOW.cs](runtime/sdl/src/SDL/Flags/SDL_WINDOW.cs), [runtime/sdl/src/SDL/Flags/SDL_RENDERER_FLAGS.cs](runtime/sdl/src/SDL/Flags/SDL_RENDERER_FLAGS.cs) |

---

## 5. Detailed walkthrough

### 5.1 `Extern.cs`

Source: [runtime/sdl/src/SDL/Extern.cs](runtime/sdl/src/SDL/Extern.cs)

What it contains:

- Platform-specific SDL library name selection (`DLL_SDL`).
- UTF-8 string helper (`ToBytes`) for C APIs.
- Most `DllImport` declarations for SDL functions (window, renderer, texture, event, audio, utility).
- Thin wrappers for string APIs (`SDL_CreateWindow(string...)`, `SDL_SetWindowTitle(string...)`, `SDL_LoadWAV_RW(string...)`).
- Error retrieval helper: `GetSdlErrorMessage()`.
- Native handle bridge: `GetSDLWindowHandle(...)` for Windows `HWND` extraction.

Why it matters:

- Single place for native boundaries.
- If SDL signatures are wrong, app can crash here even when managed code looks correct.

### 5.2 `Window.cs`

Source: [runtime/sdl/src/SDL/Window.cs](runtime/sdl/src/SDL/Window.cs)

Main responsibilities:

- Owns `_handle` (window) and `_renderer` (render context).
- Constructor initializes SDL video/audio and creates window + renderer.
- Exposes state helpers (`Width`, `Height`, position, maximized, title, fullscreen).
- Implements `Run()` main loop.
- Calls extension hooks/events (`OnLoad`, `OnUpdate`, `OnDraw`), then mouse/sound handlers.
- Presents frame only when `_redraw` is true.

Key design note:

- Rendering is "dirty-frame" style: frame presentation happens only when something requested redraw (`Clear` sets `_redraw = true`).

### 5.3 `Window.WindowEvent.cs`

Source: [runtime/sdl/src/SDL/Window.WindowEvent.cs](runtime/sdl/src/SDL/Window.WindowEvent.cs)

Main responsibilities:

- Declares lifecycle events (`OnLoad`, `OnDraw`, `OnUpdate`, `OnClose`).
- Maps `SDL_WindowEventID` values to pause/resume/close behavior.
- Uses `Paused` mode to stop normal update/draw progression while minimized/hidden.

### 5.4 `Window.KeyboardEvent.cs`

Source: [runtime/sdl/src/SDL/Window.KeyboardEvent.cs](runtime/sdl/src/SDL/Window.KeyboardEvent.cs)

Main responsibilities:

- Converts SDL modifiers (`SDL_KMOD`) to CivOne `KeyModifier`.
- Converts SDL scancodes to CivOne `Key` enum.
- Handles tricky layout cases for digits:
  - top-row digit recovery by scancode (`30..39`)
  - `Ctrl` + symbol fallback to digits (for locale/OS variance)
- Emits normalized keyboard events (`OnKeyDown`, `OnKeyUp`).

Why this file is important for beginners:

- It shows how to turn raw platform input into stable game input semantics.

### 5.5 `Window.MouseEvent.cs`

Source: [runtime/sdl/src/SDL/Window.MouseEvent.cs](runtime/sdl/src/SDL/Window.MouseEvent.cs)

Main responsibilities:

- Polls mouse state via `SDL_GetMouseState`.
- Tracks mouse position and button transitions.
- Emits `OnMouseMove`, `OnMouseDown`, `OnMouseUp`.
- Controls cursor visibility through `SDL_ShowCursor`.

### 5.6 `Window.Sound.cs`

Source: [runtime/sdl/src/SDL/Window.Sound.cs](runtime/sdl/src/SDL/Window.Sound.cs)

Main responsibilities:

- Manages single active `Wave` sound (`_currentSound`).
- Starts playback (`PlaySound`) and lifecycle cleanup (`StopSound`).
- Polls sound completion each frame in `HandleSound()`.

Behavior model:

- One-shot playback, no concurrent mix management in this layer.

### 5.7 `Texture.cs`

Source: [runtime/sdl/src/SDL/Texture.cs](runtime/sdl/src/SDL/Texture.cs)

Main responsibilities:

- Converts CivOne indexed color data (`Palette` + `Bytemap`) to packed color int buffer.
- Builds streaming SDL texture (`SDL_TEXTUREACCESS_STREAMING`).
- Optionally enables blend mode if alpha detected.
- Copies pixel buffer during texture lock/unlock.
- Draws via `SDL_RenderCopy`.

Important detail:

- Pixel format is manually defined with `DefinePixelformat(...)` in [runtime/sdl/src/SDL/Helper.cs](runtime/sdl/src/SDL/Helper.cs).

### 5.8 `TextureExtensions.cs`

Source: [runtime/sdl/src/SDL/TextureExtensions.cs](runtime/sdl/src/SDL/TextureExtensions.cs)

Main responsibilities:

- Provides extension method `Draw(...)` that safely ignores `null`/empty texture.

### 5.9 `Wave.cs`

Source: [runtime/sdl/src/SDL/Wave.cs](runtime/sdl/src/SDL/Wave.cs)

Main responsibilities:

- Loads WAV data with `SDL_LoadWAV_RW`.
- Opens SDL audio device with desired format (`SDL_AudioSpec`).
- Queues full WAV buffer (`SDL_QueueAudio`) and starts device.
- Reports playing state through queued-audio size check.
- Frees audio device and WAV buffer in `Dispose()`.

Design implication:

- `Wave` instance is single-use (`Play()` throws if called twice).

### 5.10 Interop support types (`Enums`, `Flags`, `Structs`, `Delegates`)

Sources:

- Enums: [runtime/sdl/src/SDL/Enums/SDL_EventType.cs](runtime/sdl/src/SDL/Enums/SDL_EventType.cs), [runtime/sdl/src/SDL/Enums/SDL_KeyState.cs](runtime/sdl/src/SDL/Enums/SDL_KeyState.cs), [runtime/sdl/src/SDL/Enums/SDL_Scancode.cs](runtime/sdl/src/SDL/Enums/SDL_Scancode.cs), [runtime/sdl/src/SDL/Enums/SDL_WindowEventID.cs](runtime/sdl/src/SDL/Enums/SDL_WindowEventID.cs), [runtime/sdl/src/SDL/Enums/SDL_TextureAccess.cs](runtime/sdl/src/SDL/Enums/SDL_TextureAccess.cs), [runtime/sdl/src/SDL/Enums/SDL_BlendMode.cs](runtime/sdl/src/SDL/Enums/SDL_BlendMode.cs), [runtime/sdl/src/SDL/Enums/SDL_PixelType.cs](runtime/sdl/src/SDL/Enums/SDL_PixelType.cs), [runtime/sdl/src/SDL/Enums/SDL_PixelOrder.cs](runtime/sdl/src/SDL/Enums/SDL_PixelOrder.cs), [runtime/sdl/src/SDL/Enums/SDL_PixelLayout.cs](runtime/sdl/src/SDL/Enums/SDL_PixelLayout.cs), [runtime/sdl/src/SDL/Enums/SDL_PixelFormatEnum.cs](runtime/sdl/src/SDL/Enums/SDL_PixelFormatEnum.cs), [runtime/sdl/src/SDL/Enums/SDL_Keycode.cs](runtime/sdl/src/SDL/Enums/SDL_Keycode.cs), [runtime/sdl/src/SDL/Enums/SDL_AudioFormat.cs](runtime/sdl/src/SDL/Enums/SDL_AudioFormat.cs)
- Flags: [runtime/sdl/src/SDL/Flags/SDL_INIT.cs](runtime/sdl/src/SDL/Flags/SDL_INIT.cs), [runtime/sdl/src/SDL/Flags/SDL_KMOD.cs](runtime/sdl/src/SDL/Flags/SDL_KMOD.cs), [runtime/sdl/src/SDL/Flags/SDL_WINDOW.cs](runtime/sdl/src/SDL/Flags/SDL_WINDOW.cs), [runtime/sdl/src/SDL/Flags/SDL_RENDERER_FLAGS.cs](runtime/sdl/src/SDL/Flags/SDL_RENDERER_FLAGS.cs)
- Structs: [runtime/sdl/src/SDL/Structs/SDL_AudioSpec.cs](runtime/sdl/src/SDL/Structs/SDL_AudioSpec.cs), [runtime/sdl/src/SDL/Structs/SDL_Event.cs](runtime/sdl/src/SDL/Structs/SDL_Event.cs), [runtime/sdl/src/SDL/Structs/SDL_Keysym.cs](runtime/sdl/src/SDL/Structs/SDL_Keysym.cs), [runtime/sdl/src/SDL/Structs/SDL_Rect.cs](runtime/sdl/src/SDL/Structs/SDL_Rect.cs)
- Delegate: [runtime/sdl/src/SDL/Delegates/SDL_AudioCallback.cs](runtime/sdl/src/SDL/Delegates/SDL_AudioCallback.cs)

Why they exist:

- They are C# mirrors of C SDL constants/memory layouts.
- Without accurate value/layout mapping, native calls return wrong data or fail.

---

## 6. Quick check: notable observations

This is not a full bug audit, but these points are worth checking:

1. In [runtime/sdl/src/SDL/Window.cs](runtime/sdl/src/SDL/Window.cs), `Icon` setter loops `yy < width` and `xx < width`.
   - Usually this should be `yy < height` for non-square icons.
2. In [runtime/sdl/src/SDL/Window.cs](runtime/sdl/src/SDL/Window.cs), `CastToStruct<T>(object source)` allocates/frees unmanaged memory per event.
   - Correct, but potentially expensive in hot event paths.
3. In [runtime/sdl/src/SDL/Window.cs](runtime/sdl/src/SDL/Window.cs), `Run()` calls update/draw continuously and only delays `1ms` when no redraw.
   - Expected for low-latency loops, but may keep CPU usage relatively high.
4. In [runtime/sdl/src/SDL/Window.Sound.cs](runtime/sdl/src/SDL/Window.Sound.cs), sound model is single active clip.
   - Fine for simple SFX, limited for layered/mixed audio.

---

## 7. Where to start implementing changes

If you want to implement something as a beginner, pick one of these safe starting tasks:

- Add a new keyboard mapping in [runtime/sdl/src/SDL/Window.KeyboardEvent.cs](runtime/sdl/src/SDL/Window.KeyboardEvent.cs) (`ConvertKey` switch).
- Add a mouse feature in [runtime/sdl/src/SDL/Window.MouseEvent.cs](runtime/sdl/src/SDL/Window.MouseEvent.cs) (button or movement handling).
- Add diagnostic logging around sound load/play in [runtime/sdl/src/SDL/Wave.cs](runtime/sdl/src/SDL/Wave.cs).

Good first deep-dive path:

`Window.Run()` -> `HandleEvent(...)` -> `HandleEventKeyboard(...)` / `HandleMouse()` / `HandleSound()` -> `Texture.Draw(...)`

---

## 8. Additional checks requested

### 8.1 Are there TODOs?

Checked scope: `runtime/sdl/src/**/*.cs`.

Result:

- No `TODO`, `FIXME`, `HACK`, or `XXX` markers found.

### 8.2 Potentially dead or never-executed code

These are strong candidates for dead/legacy code paths:

1. Legacy audio API declarations appear unused:
   - `SDL_OpenAudio` in [runtime/sdl/src/SDL/Extern.cs](runtime/sdl/src/SDL/Extern.cs#L189)
   - `SDL_PauseAudio` in [runtime/sdl/src/SDL/Extern.cs](runtime/sdl/src/SDL/Extern.cs#L195)
   - `SDL_CloseAudio` in [runtime/sdl/src/SDL/Extern.cs](runtime/sdl/src/SDL/Extern.cs#L198)
   Current code uses `SDL_OpenAudioDevice` / `SDL_QueueAudio` instead.

2. Suspicious SDL declaration without usage:
   - `SDL_Window()` in [runtime/sdl/src/SDL/Extern.cs](runtime/sdl/src/SDL/Extern.cs#L143)

3. Likely unused interop enums:
   - `SDL_AudioFormat` in [runtime/sdl/src/SDL/Enums/SDL_AudioFormat.cs](runtime/sdl/src/SDL/Enums/SDL_AudioFormat.cs#L14)
   - `SDL_PixelFormatEnum` in [runtime/sdl/src/SDL/Enums/SDL_PixelFormatEnum.cs](runtime/sdl/src/SDL/Enums/SDL_PixelFormatEnum.cs#L14)
   These appear to be defined for completeness, but not referenced by active runtime logic.

### 8.3 macOS status: is it underdeveloped?

Your intuition is partly correct: macOS support exists, but looks less exercised.

Evidence:

1. Build symbol for `MACOS` is conditional on config suffix in [runtime/sdl/CivOne.SDL.csproj](runtime/sdl/CivOne.SDL.csproj#L49).
   If CI/dev mostly build `*Windows`, macOS path gets less coverage.

2. macOS-specific resolver logic exists and is actively called:
   - candidate library list at [runtime/sdl/src/Program.cs](runtime/sdl/src/Program.cs#L22)
   - resolver registration at [runtime/sdl/src/Program.cs](runtime/sdl/src/Program.cs#L35)
   - invocation in `Main` at [runtime/sdl/src/Program.cs](runtime/sdl/src/Program.cs#L65)

3. macOS native integration exists in [runtime/sdl/src/Native.Mac.cs](runtime/sdl/src/Native.Mac.cs), but shows maintenance smell:
   - typo in shebang string [runtime/sdl/src/Native.Mac.cs](runtime/sdl/src/Native.Mac.cs#L23)
   - unused `filter` parameter in `MacFileChooser` [runtime/sdl/src/Native.Mac.cs](runtime/sdl/src/Native.Mac.cs#L48)

### 8.4 What else is useful to know?

Recommended next checks (high value):

1. Add a macOS build in CI (at least compile-only) to continuously validate `MACOS` paths.
2. Add tiny smoke tests for native boundary behavior:
   - SDL load success
   - window create/destroy
   - WAV queue/play/finish
3. Clean interop surface:
   - remove or annotate legacy declarations that are intentionally kept
   - document ownership/lifetime rules for all native handles
4. Add perf counters around hot loop points:
   - event cast path
   - texture lock/unlock
   - frame present cadence
5. Define platform support policy in docs:
   - "fully supported", "best effort", or "experimental" per OS

---

## 9. Additional documentation topics (requested: 1, 2, 3, 4, 7)

### 9.1 Platform matrix

Document a compact matrix for Windows/Linux/macOS:

- build config name (`DebugWindows`, `DebugLinux`, `DebugMacOS`, etc.)
- compile symbol (`WINDOWS`, `LINUX`, `MACOS`) from [runtime/sdl/CivOne.SDL.csproj](runtime/sdl/CivOne.SDL.csproj)
- SDL library resolution path
- status (`supported`, `best effort`, `experimental`)

Useful references:

- [runtime/sdl/CivOne.SDL.csproj](runtime/sdl/CivOne.SDL.csproj)
- [runtime/sdl/src/Program.cs](runtime/sdl/src/Program.cs)
- [runtime/sdl/src/Native.cs](runtime/sdl/src/Native.cs)
- [runtime/sdl/src/SDL/Extern.cs](runtime/sdl/src/SDL/Extern.cs)

### 9.2 Lifecycle and ownership

Document ownership of every native handle and who frees it:

- window handle (`_handle`) and renderer (`_renderer`) lifecycle in [runtime/sdl/src/SDL/Window.cs](runtime/sdl/src/SDL/Window.cs)
- texture handle (`_handle`) lifecycle in [runtime/sdl/src/SDL/Texture.cs](runtime/sdl/src/SDL/Texture.cs)
- audio device id + wave buffer lifecycle in [runtime/sdl/src/SDL/Wave.cs](runtime/sdl/src/SDL/Wave.cs)

Include a strict sequence diagram:

1. `SDL_Init`
2. `SDL_CreateWindow` / `SDL_CreateRenderer`
3. `Run` loop
4. `SDL_DestroyRenderer` / `SDL_DestroyWindow`
5. `SDL_Quit`

### 9.3 Event model

Document complete event conversion pipeline:

- raw poll in [runtime/sdl/src/SDL/Window.cs](runtime/sdl/src/SDL/Window.cs)
- window events in [runtime/sdl/src/SDL/Window.WindowEvent.cs](runtime/sdl/src/SDL/Window.WindowEvent.cs)
- keyboard normalization in [runtime/sdl/src/SDL/Window.KeyboardEvent.cs](runtime/sdl/src/SDL/Window.KeyboardEvent.cs)
- mouse state polling and transitions in [runtime/sdl/src/SDL/Window.MouseEvent.cs](runtime/sdl/src/SDL/Window.MouseEvent.cs)

Also document:

- which SDL events are intentionally ignored
- pause behavior and input suppression while paused
- debug-only keyboard behavior (`F9`, `F10`, `F12`)

### 9.3.1 `Run()` deep dive: how events and screens are processed

`Run()` in [runtime/sdl/src/SDL/Window.cs](runtime/sdl/src/SDL/Window.cs) is the central game loop.
It does two jobs in one place:

1. process input/window/audio events
2. advance/update/draw current runtime state (screens/layers)

Main loop order (per iteration):

1. `SDL_PollEvent(...)` reads one SDL event (if available).
2. `HandleEvent(...)` dispatches it to:
    - `HandleEventWindow(...)` for window lifecycle events
    - `HandleEventKeyboard(...)` for key down/up events
3. pause gate: if `Paused == true`, loop sleeps and skips update/draw.
4. `OnUpdate` and `OnDraw` are invoked.
5. `HandleMouse()` and `HandleSound()` run every frame.
6. present gate: `SDL_RenderPresent(...)` only if `_redraw == true`.

ASCII control-flow:

```text
while (_running)
   |
   +-- SDL_PollEvent?
   |      |
   |      +-- yes -> HandleEventWindow / HandleEventKeyboard
   |
   +-- if (_paused) -> Wait(100) -> continue
   |
   +-- OnUpdate
   +-- OnDraw
   +-- HandleMouse
   +-- HandleSound
   |
   +-- if (!_redraw) -> Wait(1) -> continue
   |
   +-- SDL_RenderPresent
   +-- _redraw = false
```

Relevant code excerpt from [runtime/sdl/src/SDL/Window.cs](runtime/sdl/src/SDL/Window.cs):

```csharp
public void Run()
{
      OnLoad?.Invoke(this, EventArgs.Empty);

      while (_running)
      {
            if (SDL_PollEvent(out SDL_Event sdlEvent) == 1)
            {
                  HandleDebuggingEvents(sdlEvent);
                  HandleEvent(sdlEvent);
                  if (!_running) break;
                  TrapDebbugger(sdlEvent);
            }

            if (_paused)
            {
                  Wait(100);
                  continue;
            }

            OnUpdate?.Invoke(this, EventArgs.Empty);
            OnDraw?.Invoke(this, EventArgs.Empty);

            HandleMouse();
            HandleSound();

            if (!_redraw)
            {
                  Wait(1);
                  continue;
            }

            SDL_RenderPresent(_renderer);
            _redraw = false;
      }
}
```

How this connects to screen drawing in `GameWindow`:

- Event wiring happens in constructor of [runtime/sdl/src/GameWindow.cs](runtime/sdl/src/GameWindow.cs):
   - `OnUpdate += Update`
   - `OnDraw += Draw`
   - `OnKeyDown += KeyDown`, `OnKeyUp += KeyUp`
   - `OnMouseMove += MouseMove`, `OnMouseDown += MouseDown`, `OnMouseUp += MouseUp`

- `Update(...)` calls `_runtime.InvokeUpdate(...)`, which advances current game/screen state.
- `Draw(...)` calls `_runtime.InvokeDraw()` and then `Render()`.
- `Render()` in [runtime/sdl/src/GameWindow.Graphics.cs](runtime/sdl/src/GameWindow.Graphics.cs):
   - clears frame (`Clear(Color.Black)` => sets `_redraw = true`)
   - iterates `_runtime.Layers`
   - creates SDL texture from each layer (`CreateTexture(...)`)
   - draws each layer (`canvas.Draw(...)`)
   - draws cursor texture (scaled/aspect-aware)

Screen/event handoff summary:

1. SDL events enter at `Run()`.
2. They are normalized and forwarded via `OnKey*`/`OnMouse*` handlers in `GameWindow`.
3. `GameWindow` forwards to runtime via `_runtime.InvokeKeyboard*` and `_runtime.InvokeMouse*`.
4. Runtime updates active screens and produces drawable layers.
5. `OnDraw` path renders runtime layers to SDL backbuffer.
6. Loop presents frame only when redraw was requested.

Practical implication:

- `Run()` is not only "event loop"; it is the synchronization point where input processing and screen rendering stay deterministic in one thread.

### 9.3.2 How multiple screens are drawn (`Screens` list)

The important part: SDL does not directly know screen objects.
It only renders `Runtime.Layers`.
`Runtime.Layers` is built from the core screen stack `Common.Screens`.

Core stack model in [src/Common.cs](src/Common.cs):

- `Common.Screens` returns a snapshot array (bottom -> top order).
- `Common.AddScreen(...)` pushes a new screen on top.
- `Common.DestroyScreen(...)` removes and disposes a screen.
- `Common.TopScreen` is:
   - last modal screen, if any modal exists
   - otherwise last screen in stack

Where stack becomes drawable layers:

- In [src/RuntimeHandler.cs](src/RuntimeHandler.cs), `OnDraw(...)` decides the visible layers:
   - if top screen is `Modal`: `Runtime.Layers = new[] { topScreen.Bitmap }`
   - otherwise: `Runtime.Layers = Common.Screens.Select(x => x.Bitmap).ToArray()`

This means:

1. Non-modal flow: multiple screens are composited in stack order.
2. Modal flow: only top modal screen bitmap is rendered to runtime layers.

ASCII stack -> layer mapping:

```text
Common.Screens (bottom -> top)

[GamePlay] [Overlay] [Popup]
      |         |        |
      +---------+--------+----> Runtime.Layers[] (if no modal)

If [Popup] has [Modal]:

[GamePlay] [Overlay] [Popup(Modal)]
                                     |
                                     +----> Runtime.Layers = [ Popup.Bitmap ]
```

Input/event routing with multiple screens:

- `Run()` receives SDL events and triggers `OnKey*` / mouse events.
- In [runtime/sdl/src/GameWindow.cs](runtime/sdl/src/GameWindow.cs), handlers forward to runtime.
- In [src/RuntimeHandler.cs](src/RuntimeHandler.cs):
   - keyboard goes to `TopScreen?.KeyDown(args)`
   - mouse goes to `TopScreen?.MouseDown/MouseUp/MouseMove/MouseDrag`

So rendering and input both center around top/active screen semantics:

- render all screens only when no modal blocks view
- dispatch input to top screen only

Where `Run()` fits into this:

1. `Run()` triggers `OnUpdate` -> `RuntimeHandler.OnUpdate` updates screen stack members.
2. `Run()` triggers `OnDraw` -> `RuntimeHandler.OnDraw` converts screen stack to `Runtime.Layers`.
3. `GameWindow.Render()` draws every layer from `Runtime.Layers`.
4. `Run()` finally presents frame (`SDL_RenderPresent`) if redraw requested.

### 9.3.3 `GameTask.Enqueue(...)` and how it affects screens

In current codebase, queue API is `GameTask.Enqueue(...)` in [src/GameTask.cs](src/GameTask.cs), not `Game.Enqueue(...)`.

What `GameTask.Enqueue(...)` does:

1. takes a `GameTask` instance
2. subscribes to its `Done` event
3. appends it to internal task list (`_tasks`)

Where queue is consumed:

- `RuntimeHandler.Update()` calls `GameTask.Update()` in [src/RuntimeHandler.cs](src/RuntimeHandler.cs).
- `GameTask.Update()` in [src/GameTask.cs](src/GameTask.cs):
  - starts next queued task when no current task exists
  - calls `Run()` once at task start
  - calls `Step()` on following updates until task finishes

Task lifecycle (simplified):

```text
GameTask.Enqueue(task)
   -> _tasks.Add(task)

next Runtime update tick:
   GameTask.Update()
      -> NextTask()
      -> task.Run()

following ticks:
   GameTask.Update()
      -> task.Step() (optional, if overridden)

task completion:
   task.EndTask() -> Done event
   -> queue removes task
   -> next task starts automatically
```

Why this matters for screens:

- Many queued tasks are screen tasks (for example `Show` in [src/Tasks/Show.cs](src/Tasks/Show.cs)).
- `Show.Run()` does `Common.AddScreen(_screen)`.
- So enqueueing a `Show` task changes `Common.Screens` asynchronously during update ticks, not immediately at call site.

Concrete startup example:

- In [src/RuntimeHandler.cs](src/RuntimeHandler.cs), `OnInitialize(...)` enqueues `Show.Screens(StartupScreens)`.
- Each finished `Show` task inserts the next one (`GameTask.Insert(nextTask())`) inside [src/Tasks/Show.cs](src/Tasks/Show.cs).
- Result: startup screens appear as a sequence controlled by task completion.

Relationship to `Run()` loop:

1. `Run()` invokes `OnUpdate` in SDL layer.
2. `RuntimeHandler.OnUpdate` advances `GameTask` queue.
3. Queue may add/remove screens (`Common.AddScreen` / `Common.DestroyScreen`).
4. Same `Run()` cycle invokes `OnDraw`; `RuntimeHandler.OnDraw` snapshots screens into `Runtime.Layers`.
5. SDL render path draws those layers.

Key takeaway:

- `GameTask.Enqueue(...)` is command scheduling.
- `Run()` is execution driver.
- `Common.Screens` is visual state.
- `Runtime.Layers` is the rendered projection of that state.

### 9.4 Rendering pipeline

Rendering path in this SDL runtime is:

1. indexed image input (`Palette` + `Bytemap`)
2. packed pixel format selection (`ABGR8888`)
3. one-time texture upload into SDL texture memory
4. per-frame `SDL_RenderCopy`
5. conditional `SDL_RenderPresent` when `_redraw == true`

Primary code locations:

- [runtime/sdl/src/SDL/Texture.cs](runtime/sdl/src/SDL/Texture.cs)
- [runtime/sdl/src/SDL/Helper.cs](runtime/sdl/src/SDL/Helper.cs)
- [runtime/sdl/src/SDL/Window.cs](runtime/sdl/src/SDL/Window.cs)

ASCII flow diagram:

```text
     Game asset (indexed)
   +-----------------------+
   | Palette + Bytemap     |
   +----------+------------+
            |
            | PaletteArray + ToColourMap
            v
   +-----------------------+
   | int[] ABGR pixels     |
   +----------+------------+
            |
            | SDL_CreateTexture(...STREAMING...)
            | SDL_LockTexture
            | Marshal.Copy
            | SDL_UnlockTexture
            v
   +-----------------------+
   | SDL texture (GPU/SDL) |
   +----------+------------+
            |
            | each draw: SDL_RenderCopy
            v
   +-----------------------+
   | SDL backbuffer        |
   +----------+------------+
            |
            | if (_redraw) SDL_RenderPresent
            v
   +-----------------------+
   | visible frame         |
   +-----------------------+
```

Key implementation excerpts:

Pixel format composition in [runtime/sdl/src/SDL/Helper.cs](runtime/sdl/src/SDL/Helper.cs):

```csharp
private static uint DefinePixelformat(SDL_PixelType type, SDL_PixelOrder order, SDL_PixelLayout layout, byte bits, byte bytes)
{
   return (uint) (
      (1 << 28) |
      (((byte) type) << 24) |
      (((byte) order) << 20) |
      (((byte) layout) << 16) |
      (bits << 8) |
      (bytes)
   );
}
```

Texture creation + upload in [runtime/sdl/src/SDL/Texture.cs](runtime/sdl/src/SDL/Texture.cs):

```csharp
_handle = SDL_CreateTexture(renderer, SDL_PIXELFORMAT_ABGR8888,
   SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING, Width, Height);

int[] paletteData = PaletteArray(palette);
if (HasAlpha(palette))
   SDL_SetTextureBlendMode(_handle, SDL_BlendMode.SDL_BLENDMODE_BLEND);

if (SDL_LockTexture(_handle, ref rect, out IntPtr pixels, out int pitch) == 0)
{
   int[] src = bytemap.ToColourMap(paletteData);
   Marshal.Copy(src, 0, pixels, Width * Height);
   SDL_UnlockTexture(_handle);
}
```

Draw call in [runtime/sdl/src/SDL/Texture.cs](runtime/sdl/src/SDL/Texture.cs):

```csharp
SDL_RenderCopy(_renderer, _handle, ref _rect, ref dst);
```

Present gating in [runtime/sdl/src/SDL/Window.cs](runtime/sdl/src/SDL/Window.cs):

```csharp
if (!_redraw)
{
   Wait(1);
   continue;
}

SDL_RenderPresent(_renderer);
_redraw = false;
```

Redraw trigger in [runtime/sdl/src/SDL/Window.cs](runtime/sdl/src/SDL/Window.cs):

```csharp
protected void Clear(Color color)
{
   _redraw = true;
   SDL_SetRenderDrawColor(_renderer, color.R, color.G, color.B, color.A);
   SDL_RenderClear(_renderer);
}
```

Notes:

- Upload currently happens in `Texture` constructor, not lazily per frame.
- `pitch` from `SDL_LockTexture` is currently ignored; code assumes tightly packed `Width * Height * 4`.
- `src` rectangle variable exists in `Draw`, but current call uses `_rect` as source rectangle.

### 9.5 Performance characteristics

Document expected runtime behavior and hotspots:

- event loop cadence and `Wait(1)` behavior in [runtime/sdl/src/SDL/Window.cs](runtime/sdl/src/SDL/Window.cs)
- per-event marshal cast cost (`CastToStruct<T>`) in [runtime/sdl/src/SDL/Window.cs](runtime/sdl/src/SDL/Window.cs)
- texture lock/upload frequency in [runtime/sdl/src/SDL/Texture.cs](runtime/sdl/src/SDL/Texture.cs)

Add simple measurement checklist:

1. idle CPU when no redraw
2. CPU under continuous input
3. frame pacing stability
4. texture upload time for typical asset sizes
