# True-colour layers

## Why this exists

PNG decoding now works, but every image still has to pass through the engine's palette model before
it reaches the screen: at most 256 colours, shared by everything visible at once. For modern artwork
that is the wrong constraint. The goal is that a PNG can be drawn as it is, in full colour, while the
existing palette-based pipeline keeps working untouched for the game's own graphics and for
animations that deliberately use their own palettes.

This replaces phase 3 of `png-image-service.plan.md` (building a palette with median cut). Drawing
without a palette is better than inventing one: the quantiser was only ever a way to make a
true-colour image survive a pipeline that cannot carry it.

## What the code does today

The important finding is that **the renderer is already true-colour**. The palette is resolved on the
CPU in the last step before the upload, not by the GPU:

```
IScreen.Bitmap (Bytemap, palette indices)
  -> RuntimeHandler.OnDrawCore: Runtime.Layers = every screen's bitmap   (src/RuntimeHandler.cs:296)
                                Runtime.Palette = the top screen's palette
  -> GameWindow: one SDL.Texture per layer                        (runtime/sdl/src/GameWindow.Graphics.cs:100)
  -> Texture.UpdateFrom: index -> colour -> packed ABGR8888       (runtime/sdl/src/SDL/Texture.cs:178)
  -> SDL_RenderCopy of each layer onto the same destination rect  (GameWindow.Graphics.cs:166)
  -> cursor texture and FPS overlay drawn on top
```

So the pieces a true-colour image needs mostly exist:

* The texture format is already `SDL_PIXELFORMAT_ABGR8888`, 32 bit with alpha
  (`Texture.cs:24`). Nothing about the GPU path is palette-based.
* A **layer compositor already exists**: `IRuntime.Layers` is an ordered `Bytemap[]`, each layer
  becomes its own texture, and they are drawn stacked. Cursor and FPS overlay prove that extra
  textures can be composited on top.
* Alpha blending is already used when a palette contains transparent entries
  (`SDL_SetTextureBlendMode`, `Texture.cs:105`).
* Textures are cached across frames and only rebuilt when a dirty flag says so
  (`RebuildLayerTextureCacheIfNeeded`, `GameWindow.Graphics.cs:81`).

## The one real constraint

`IRuntime` carries **one** `Palette` per frame (`src/IRuntime.cs:32`), and every layer is resolved
through it. That is what limits the whole screen — not each image — to 256 colours.

This also corrects an assumption worth stating plainly: an image that "brings its own palette" does
not escape that budget today. `Palette.Merge` exists to fold a sprite's palette into the single
screen palette, so its colours take slots away from everything else on screen. Per-image palettes
only become genuinely independent once an image is drawn as its own layer, which is exactly what this
plan introduces.

Every layer is also full-canvas: layers are `CanvasWidth x CanvasHeight` (320x200 by default) and are
drawn stretched over the whole border rectangle. There is no notion of a positioned layer yet.

## Proposed design

A second kind of layer that carries colours instead of indices, composited by the same renderer.

### The layer

New MIT interface, `src/Graphics/Layers/ITrueColourLayer.cs`:

```csharp
internal interface ITrueColourLayer
{
    /// Pixel size of the source image, independent of the canvas.
    int Width { get; }
    int Height { get; }
    ReadOnlySpan<Colour> Pixels { get; }

    /// Where to draw, in canvas coordinates (320x200 space), so the layer scales and
    /// positions with the game regardless of window size.
    Rectangle Target { get; }

    /// Whether the layer belongs behind or in front of the palette-based layers.
    TrueColourLayerOrder Order { get; }

    /// Nearest-neighbour for pixel art, linear for photographic artwork.
    bool Smooth { get; }

    /// Incremented whenever Pixels change, so the renderer knows when to re-upload.
    int Version { get; }
}
```

`TrueColourLayerOrder` is an enum with `Background` and `Foreground`. Those are the two cases that
actually occur; full interleaving with the indexed stack can be added later by turning this into an
integer position if a real need shows up.

A `DecodedImage` from the PNG decoder satisfies this almost directly, so the common implementation is
a small adapter holding a `DecodedImage` plus a target rectangle.

### Where layers come from

A screen opts in by implementing a second new interface, so no existing screen changes:

```csharp
internal interface ITrueColourLayerProvider
{
    IEnumerable<ITrueColourLayer> TrueColourLayers { get; }
}
```

`RuntimeHandler.OnDrawCore` collects them next to the bitmaps it already collects. Since
`RuntimeHandler.cs` is CC0, the collecting itself lives in a new MIT delegate
(`TrueColourLayerCollectorDelegate`) and the CC0 file keeps a single call, the same way
`Resources.cs` was reduced for the icon.

### Transport to the runtime

`IRuntime` (CC0) gains one property beside the existing `Layers`:

```csharp
IReadOnlyList<ITrueColourLayer>? ColourLayers { get; set; }
```

One line in a CC0 file, directly beside the member it belongs to. The alternative — a separate
capability interface that the SDL runtime implements and `RuntimeHandler` type-checks for — avoids
touching `IRuntime` at all but trades a declared contract for a cast, so it is only worth it if
touching `IRuntime` turns out to be contentious.

### The texture

New MIT file `runtime/sdl/src/SDL/ColourTexture.cs`, a second nested class in the existing
`partial class SDL`, so `Texture.cs` (CC0) is not grown. It mirrors `Texture` but fills from
`ReadOnlySpan<Colour>` instead of palette plus bytemap, using the same packing
(`(A << 24) | (B << 16) | (G << 8) | R`) and the same streaming-texture and reusable-buffer approach.
Per-layer filtering needs `SDL_SetTextureScaleMode`, which is not bound yet; the new file declares
that P/Invoke itself rather than extending the CC0 `Extern.cs`.

### Compositing and coordinates

New MIT delegate `ColourLayerRendererDelegate` owns the texture cache (keyed by layer reference,
re-uploading when `Version` changed, evicting layers that disappeared) and exposes
`Draw(order, borders)`. `GameWindow.Graphics.cs` (CC0) gains two calls around its existing loop:
background layers before it, foreground layers after it.

The target rectangle is mapped from canvas space to window space with the same borders the indexed
layers use:

```
scaleX = (x2 - x1) / CanvasWidth
scaleY = (y2 - y1) / CanvasHeight
destination = (x1 + Target.X * scaleX, y1 + Target.Y * scaleY, Target.Width * scaleX, Target.Height * scaleY)
```

This is where the actual modern gain appears: the source texture keeps its own resolution and SDL
samples it into the destination rectangle, so a 1024 px wide image drawn into a 320 px wide canvas
rectangle is rendered at full window resolution instead of being upscaled from 320x200.

## What this does not do

Stated plainly, because each of these is a reason to keep the palette pipeline rather than a defect:

* **No drawing into a true-colour layer.** It is blitted as a whole. The existing `Picture`
  operations (`AddLayer`, text, rectangles) work on palette indices and are not available. A screen
  that needs to compose colour content would need an RGBA drawing surface, which is deliberately out
  of scope.
* **No palette effects.** Colour cycling and palette fades change `IRuntime.Palette` and therefore
  cannot touch a true-colour layer. Animations that rely on this stay indexed — which is the
  intended division of labour. A layer-level opacity or tint would cover simple fades and is listed
  as a later step.
* **Screenshots would miss these layers.** `RuntimeHandler` composes Ctrl+F5 screenshots by adding
  the indexed layers into one indexed `Picture` and writing a GIF (`src/RuntimeHandler.cs:395-411`),
  and the MCP screenshot path writes an indexed PNG. Neither sees a true-colour layer. See the open
  questions.
* **No change to any existing screen.** Nothing in the game draws a true-colour layer until a screen
  or a plugin asks for one.

## Conventions

* New behaviour in new MIT files; the three CC0 files involved (`IRuntime.cs`, `RuntimeHandler.cs`,
  `GameWindow.Graphics.cs`) change by one property and three calls in total.
* The two pieces of behaviour are delegates (`TrueColourLayerCollectorDelegate`,
  `ColourLayerRendererDelegate`), instantiated with `new()`, with the usual `CA1822` suppression
  where a member does not touch instance state.
* No static singleton access in any new type; the renderer delegate is owned by `GameWindow`, which
  already holds the renderer handle.
* `ColourTexture` owns an unmanaged SDL handle and follows the dispose pattern of `Texture`.

## Scope

**Step 1 — the rendering path.** `ITrueColourLayer`, `TrueColourLayerOrder`, the `DecodedImage`
adapter, `ColourTexture`, `ColourLayerRendererDelegate`, the `IRuntime` property, the collector, and
the three calls in the CC0 files. Verified behind a developer-only toggle that draws a PNG as a
background and as a foreground layer, so no screen's appearance changes while the path is proven.

**Step 2 — opacity and tint** on a layer, so a screen that fades does not have to leave the layer at
full strength.

**Step 3 — an RGBA drawing surface**, only if a concrete screen or plugin needs to compose rather
than blit.

## Risks and notes

* **Per-frame cost.** A true-colour layer at window resolution is a much larger upload than a 320x200
  indexed layer. The cache must therefore only re-upload on a `Version` change; a layer that
  re-uploads every frame would be visible in the frame time immediately. This is the main thing to
  watch when measuring.
* **Memory.** `Colour[]` is four bytes per pixel, so a 1920x1080 layer is about 8 MB managed plus the
  same again on the GPU. Fine for a handful of layers, not for a sprite sheet per unit.
* **Filtering mismatch.** The game deliberately renders with nearest-neighbour scaling. A smooth
  true-colour layer next to hard-edged pixel art can look wrong; `Smooth` is per layer so the
  decision stays with whoever supplies the art.
* **`Rectangle` means `System.Drawing.Rectangle`**, which the codebase already uses (`Bytemap` works
  with `System.Drawing.Size`).

## Open questions

1. **Screenshots.** Accept that Ctrl+F5 and the MCP screenshot capture only the palette-based layers
   and document it, or compose screenshots in RGBA so they match what is on screen? The second is
   the correct result but turns the screenshot path into a second compositor.
2. **Canvas-space targets only?** The design positions layers in 320x200 canvas space so they move
   with the game. A layer that should be positioned in window pixels instead (a true full-window
   background, independent of the 320x200 aspect box) would need a second coordinate mode. Worth
   having from the start, or add when needed?
3. **First real consumer.** Step 1 is verified behind a developer toggle. Is there a screen that
   should actually use this first, or does it stay dormant until plugin support arrives?
