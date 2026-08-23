# SDL Source Review

Scope: `runtime/sdl/src`

## Critical

1. Cursor rendering can throw `NullReferenceException`.
   In [GameWindow.Graphics.cs](runtime/sdl/src/GameWindow.Graphics.cs#L59) and [GameWindow.Graphics.cs](runtime/sdl/src/GameWindow.Graphics.cs#L65), `CursorTexture?.Draw(...)` is combined with `CursorTexture.Width` and `CursorTexture.Height` in the same call.
   If `CursorTexture` is `null`, the null-conditional only protects the method call, not the property access in the argument list.
   Fix: assign `CursorTexture` to a local variable, return early when it is `null`, and use the local for both the null check and the width/height values.

2. Window initialization can leave the object in an invalid state.
   In [SDL/Window.cs](runtime/sdl/src/SDL/Window.cs#L324), the code creates the renderer before it verifies that `_handle` is valid.
   If `SDL_CreateWindow` fails, the constructor still continues with an invalid handle and may later run against a half-initialized window.
   Fix: check `_handle` immediately after `SDL_CreateWindow`, throw or fail fast, and only create the renderer after the window handle is known to be valid.

3. Logging can crash on a fresh install when the storage directory does not exist.
   In [Runtime.cs](runtime/sdl/src/Runtime.cs#L64) to [Runtime.cs](runtime/sdl/src/Runtime.cs#L77), `TryOpenWrite` only catches `IOException`.
   If the log folder is missing, `FileStream` throws `DirectoryNotFoundException`, which is not caught.
   The first log call happens very early in [Program.cs](runtime/sdl/src/Program.cs#L248), so startup can fail before the game even enters the main loop.
   Fix: create the storage directory before opening the log file and catch the expected file-system exceptions explicitly.

## Important

4. Command-line parsing has an unguarded seed parse and weak slot validation.
   In [Program.cs](runtime/sdl/src/Program.cs#L235), `ushort.Parse(args[++i])` can throw on missing or malformed input.
   The `--load-slot` parser in [Program.cs](runtime/sdl/src/Program.cs#L199) to [Program.cs](runtime/sdl/src/Program.cs#L210) also accepts `0` and `11..15` even though the help text says `1..10`.
   Fix: add an existence check before reading `--seed`, switch to `TryParse`, and tighten the load-slot regex or replace it with explicit range validation.

5. macOS file and folder chooser scripts are not escaped.
   In [Native.Mac.cs](runtime/sdl/src/Native.Mac.cs#L84) to [Native.Mac.cs](runtime/sdl/src/Native.Mac.cs#L93), `title` and `initialFileName` are interpolated directly into AppleScript text.
   A quote or other special character can break the script, and user-controlled values can potentially alter the generated command.
   Fix: escape AppleScript strings properly or avoid generating shell scripts and invoke the native API in a safer way.

6. The Windows folder browser does not restore cursor state on cancel.
   In [Native.Win32.cs](runtime/sdl/src/Native.Win32.cs#L78) to [Native.Win32.cs](runtime/sdl/src/Native.Win32.cs#L101), `ShowCursor()` is called before the dialog, but `HideCursor()` is skipped when the user cancels.
   That leaves the cursor visibility state inconsistent for the rest of the session.
   Fix: move the cursor reset into a `finally` block so the state is restored on every exit path.

7. Non-square window icons can be drawn incorrectly or out of bounds.
   In [SDL/Window.cs](runtime/sdl/src/SDL/Window.cs#L285) and [SDL/Window.cs](runtime/sdl/src/SDL/Window.cs#L286), both loops use `width`.
   The code never iterates over `height`, so non-square bitmaps are truncated or can read past the valid Y range.
   Fix: loop `yy < height` in the outer loop and keep `xx < width` in the inner loop.

## Medium

8. Resource lookup assumes the SDL assembly and window icon always exist.
   In [Resources.cs](runtime/sdl/src/Resources.cs#L22) and [Resources.cs](runtime/sdl/src/Resources.cs#L47) to [Resources.cs](runtime/sdl/src/Resources.cs#L50), the code uses `First(...)` to find the assembly and then dereferences `WindowIcon` without checking for `null`.
   If the assembly name changes or the embedded resource is missing, startup fails with an exception.
   Fix: use `FirstOrDefault`, validate the result, and return a fallback icon or a controlled error message.

9. Debounce callback failures are swallowed after logging.
   In [Services/DebounceService.cs](runtime/sdl/src/Services/DebounceService.cs#L122), any exception from a callback is caught and only sent to the logger.
   That prevents the caller from knowing that a scheduled persistence action failed, which can hide data-loss bugs.
   Fix: either rethrow after logging for critical callbacks or surface failures through a status/result channel.

## Notes

- I did not find any true async usage, so there is no direct `async`/`await` review surface here.
- I did not see N+1 query patterns in the SDL layer.
- The biggest runtime risks are the null cursor draw, the invalid window initialization path, and the startup log crash on a missing storage directory.