# Graphics Folder - Code Review

**Review Date:** 2026-05-24  
**Scope:** `src/Graphics/` - Complete analysis of graphics rendering, font management, sprite caching, and resource handling.

---

## Summary

The Graphics folder contains 30 files handling rendering, fonts, sprites, and resource management. **12 significant issues** identified across severity levels:

- **KRITISCH:** 2 (Race Conditions, Thread-Safety)
- **WICHTIG:** 4 (Memory Leaks, Cache Corruption, Null Risks)
- **MEDIUM:** 6 (Logic Errors, Inefficiencies, Edge Cases)

---

## Critical Issues

### 1. 🔴 KRITISCH: Race Conditions in Icons.cs

**File:** [src/Graphics/Icons.cs](src/Graphics/Icons.cs)  
**Type:** Race Condition / Thread-Safety  
**Severity:** KRITISCH

**Problem:**
Static properties with lazy initialization without synchronization. Multiple threads can race to initialize `_food`, `_shield`, etc., creating multiple instances and wasting memory.

```csharp
private static IBitmap _food;
public static IBitmap Food
{
    get
    {
        if (_food == null)  // ← Race condition: multiple threads can enter here
        {
            if (RuntimeHandler.Runtime.Settings.Free || !Resources.Exists("SP257"))
            {
                _food = new Picture(Free.Instance.Food, Common.GetPalette256);
            }
            else
            {
                _food = Resources["SP257"][128, 32, 8, 8].ColourReplace(3, 0)...
            }
        }
        return _food;
    }
}
```

**Solution:**
Use `Lazy<T>` for thread-safe lazy initialization:

```csharp
private static readonly Lazy<IBitmap> _food = new Lazy<IBitmap>(() =>
{
    if (RuntimeHandler.Runtime.Settings.Free || !Resources.Exists("SP257"))
        return new Picture(Free.Instance.Food, Common.GetPalette256);
    return Resources["SP257"][128, 32, 8, 8].ColourReplace(3, 0)...;
});

public static IBitmap Food => _food.Value;
```

---

### 2. 🔴 KRITISCH: Race Condition in Resources.cs Cache Updates

**File:** [src/Graphics/Resources.cs](src/Graphics/Resources.cs#L1)  
**Type:** Race Condition  
**Severity:** KRITISCH

**Problem:**
Concurrent access to `_cache` and `_textCache` dictionaries without synchronization. The `GetText()` and `GetLetter()` methods check and add entries in non-atomic operations.

```csharp
private readonly Dictionary<string, Picture> _cache = new Dictionary<string, Picture>();
private readonly Dictionary<string, Bytemap> _textCache = new Dictionary<string, Bytemap>();

// In GetLetter():
string key = string.Format("letter{0}|{1}|{2}", colour, font, letter);
if (!_textCache.ContainsKey(key))  // ← Race: Entry can be added by another thread here
{
    _textCache.Add(key, Font(font).GetLetter(letter, colour));  // ← Exception if key exists
}
```

**Solution:**
Use `ConcurrentDictionary` or synchronization:

```csharp
private readonly ConcurrentDictionary<string, Bytemap> _textCache = 
    new ConcurrentDictionary<string, Bytemap>();

private Bytemap GetLetter(byte colour, int font, char letter)
{
    string key = string.Format("letter{0}|{1}|{2}", colour, font, letter);
    return _textCache.GetOrAdd(key, _ => Font(font).GetLetter(letter, colour));
}
```

---

## Important Issues

### 3. 🟠 WICHTIG: Memory Leak in CachedSpriteCollection.cs

**File:** [src/Graphics/Sprites/CachedSpriteCollection.cs](src/Graphics/Sprites/CachedSpriteCollection.cs)  
**Type:** Memory Leak / Incomplete Resource Cleanup  
**Severity:** WICHTIG

**Problem:**
The nested `Sprite` class has a finalizer but no `IDisposable` implementation. Bitmaps are allocated but might not be properly disposed. The `Clear()` method clears references but doesn't dispose individual sprites.

```csharp
private class Sprite : ISprite
{
    public Bytemap Bitmap { get; private set; }

    ~Sprite()
    {
        Bitmap?.Dispose();  // ← Only called when GC runs, unreliable
        Bitmap = null;
    }
}

public void Clear()
{
    _sprites.Clear();  // ← Just clears references, doesn't dispose
}
```

**Solution:**
Implement `IDisposable` and call Dispose explicitly:

```csharp
internal class CachedSpriteCollection<T> : ISpriteCollection<T>, IDisposable
{
    private class Sprite : ISprite, IDisposable
    {
        public Bytemap Bitmap { get; private set; }

        public void Dispose()
        {
            Bitmap?.Dispose();
            Bitmap = null;
        }
    }

    public void Clear()
    {
        foreach (var sprite in _sprites.Values.OfType<IDisposable>())
            sprite.Dispose();
        _sprites.Clear();
    }

    public void Dispose() => Clear();
}
```

---

### 4. 🟠 WICHTIG: Memory Leak in CachedSprite.cs

**File:** [src/Graphics/Sprites/CachedSprite.cs](src/Graphics/Sprites/CachedSprite.cs)  
**Type:** Memory Leak / Double-Dispose Risk  
**Severity:** WICHTIG

**Problem:**
Relying on finalizer for cleanup without `IDisposable`. The `Clear()` method doesn't properly handle concurrent access or multiple calls.

```csharp
public void Clear()
{
    _bitmap?.Dispose();
    _bitmap = null;  // ← But finalizer will also try to dispose
}

~CachedSprite()
{
    _bitmap?.Dispose();  // ← Potential double-dispose
    _bitmap = null;
}
```

**Solution:**
Implement `IDisposable` with proper disposal pattern:

```csharp
internal class CachedSprite : BaseInstance, ISprite, ICached, IDisposable
{
    private readonly Func<Bytemap> GetSprite;
    private Bytemap _bitmap;
    private bool _disposed;

    public Bytemap Bitmap
    {
        get
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(CachedSprite));
            return _bitmap ??= GetSprite();
        }
    }

    public void Clear()
    {
        _bitmap?.Dispose();
        _bitmap = null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        Clear();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    ~CachedSprite() => Clear();
}
```

---

### 5. 🟠 WICHTIG: NullReference Risk in Resources.cs - Font Loading

**File:** [src/Graphics/Resources.cs](src/Graphics/Resources.cs#L44)  
**Type:** NullReference-Risiko / Index Out of Range  
**Severity:** WICHTIG

**Problem:**
In `LoadFonts()`, index calculations don't validate file length. A truncated FONTS.CV file could cause `IndexOutOfRangeException`.

```csharp
List<ushort> fontOffsets = [];
int index = 0;
uint fontCount = BitConverter.ToUInt16(file, index);  // ← No length check
index += 2;

for (int i = 0; i < fontCount; i++)
{
    fontOffsets.Add(BitConverter.ToUInt16(file, index));  // ← Can throw
    index += 2;
}
```

**Solution:**
Add bounds validation:

```csharp
private void LoadFonts()
{
    byte[] file;
    string filename = Path.Combine(Settings.DataDirectory, "FONTS.CV");
    if (!File.Exists(filename))
    {
        Log("Font file not found, fallback to default font");
        return;
    }

    using (FileStream fs = new FileStream(filename, FileMode.Open))
    {
        file = new byte[fs.Length];
        fs.ReadExactly(file, 0, file.Length);
    }
    
    if (file.Length < 2)
    {
        Log("Font file too short");
        return;
    }

    List<ushort> fontOffsets = [];
    int index = 0;
    uint fontCount = BitConverter.ToUInt16(file, index);
    index += 2;

    // Validate that we have enough data for all offsets
    if (index + (fontCount * 2) > file.Length)
    {
        Log("Font file corrupted: insufficient data for font offsets");
        return;
    }

    for (int i = 0; i < fontCount; i++)
    {
        fontOffsets.Add(BitConverter.ToUInt16(file, index));
        index += 2;
    }
    
    // ... rest of method
}
```

---

### 6. 🟠 WICHTIG: Exceptions Swallowed in Resources.GetCivilopediaText()

**File:** [src/Graphics/Resources.cs](src/Graphics/Resources.cs#L99)  
**Type:** Fehlerhafte Exception-Behandlung / Logic Error  
**Severity:** WICHTIG

**Problem:**
Complex string manipulation with multiple `IndexOf()` calls and no error handling. If `TextFileFactory.Get()` returns null or text operations fail, silent failure occurs.

```csharp
internal string[] GetCivilopediaText(string name)
{
    List<string> textLines = [];
    string text = string.Join(" ", TextFileFactory.Get().GetGameText(name));  // ← Can throw NRE
    string t = "";
    while (text.Length > 0)
    {
        if (text.IndexOf(' ') == -1)  // ← Multiple IndexOf calls, inefficient
        {
            if (t.Length > 0 && GetTextSize(6, string.Join(" ", t, text)).Width < 294)
                text = string.Join(" ", t, text);
            else if (t.Length > 0)
                textLines.Add(t);
            t = text;
            text = "";
        }
        // ... logic continues
    }
    return textLines.ToArray();
}
```

**Solution:**
Add null checks and use `string.Split()`:

```csharp
internal string[] GetCivilopediaText(string name)
{
    try
    {
        var textFactory = TextFileFactory.Get();
        if (textFactory == null)
        {
            Log("TextFileFactory returned null");
            return [];
        }

        var gameText = textFactory.GetGameText(name);
        if (gameText == null)
        {
            Log("GetGameText returned null for: {0}", name);
            return [];
        }

        string text = string.Join(" ", gameText);
        if (string.IsNullOrEmpty(text))
            return [];

        List<string> textLines = [];
        string[] words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        StringBuilder line = new StringBuilder();

        foreach (string word in words)
        {
            string testLine = line.Length == 0 ? word : line + " " + word;
            if (GetTextSize(6, testLine).Width < 294)
            {
                if (line.Length > 0) line.Append(' ');
                line.Append(word);
            }
            else
            {
                if (line.Length > 0)
                    textLines.Add(line.ToString());
                line.Clear().Append(word);
            }
        }

        if (line.Length > 0)
            textLines.Add(line.ToString());

        return textLines.ToArray();
    }
    catch (Exception ex)
    {
        Log("Error in GetCivilopediaText: {0}", ex.Message);
        return [];
    }
}
```

---

## Medium Issues

### 7. 🟡 MEDIUM: Cache Key Allocation Inefficiency in Resources.cs

**File:** [src/Graphics/Resources.cs](src/Graphics/Resources.cs#L133)  
**Type:** Unnötige Allocations / Performance  
**Severity:** MEDIUM

**Problem:**
String keys are allocated repeatedly using `string.Format()`. High-frequency cache lookups waste memory.

```csharp
private Bytemap GetLetter(byte colour, int font, char letter)
{
    string key = string.Format("letter{0}|{1}|{2}", colour, font, letter);  // ← New string each time
    if (!_textCache.ContainsKey(key))
    {
        _textCache.Add(key, Font(font).GetLetter(letter, colour));
    }
    return _textCache[key];
}
```

**Solution:**
Use a value-type cache key or interpolation:

```csharp
private readonly Dictionary<(byte, int, char), Bytemap> _textCache = [];

private Bytemap GetLetter(byte colour, int font, char letter)
{
    var key = (colour, font, letter);
    if (!_textCache.TryGetValue(key, out var cached))
    {
        cached = Font(font).GetLetter(letter, colour);
        _textCache[key] = cached;
    }
    return cached;
}
```

---

### 8. 🟡 MEDIUM: Fragile FirstChar Detection in FontSetFactory.cs

**File:** [src/Graphics/FontSetFactory.cs](src/Graphics/FontSetFactory.cs#L25)  
**Type:** Logikfehler / Edge Case  
**Severity:** MEDIUM

**Problem:**
Font type detection relies on `firstChar == 32` (ASCII space). A malformed file with space as first character would be misidentified.

```csharp
internal static IFont Create(byte[] bytes, ushort offset)
{
    byte firstChar = bytes[offset - 8];  // ← No bounds checking

    return Settings.Instance.SimulateInternationalFont switch
    {
        SimulateInternationalFont.Yes => new InternationalSimulatedFontSet(bytes, offset),
        SimulateInternationalFont.No => new Fontset(bytes, offset),
        _ => firstChar == 32  // ← Fragile logic
                    ? new InternationalSimulatedFontSet(bytes, offset)
                    : new Fontset(bytes, offset),
    };
}
```

**Solution:**
Add validation and explicit documentation:

```csharp
/// <summary>
/// Creates a font instance with validation.
/// firstChar at offset-8 indicates font type (32=simulated, &lt;32=control chars).
/// </summary>
internal static IFont Create(byte[] bytes, ushort offset)
{
    // Validate offset
    if (offset < 8 || offset >= bytes.Length)
        throw new ArgumentOutOfRangeException(nameof(offset), "Offset out of bounds or insufficient header space");

    byte firstChar = bytes[offset - 8];

    if (Settings.Instance.SimulateInternationalFont == SimulateInternationalFont.Yes)
        return new InternationalSimulatedFontSet(bytes, offset);

    if (Settings.Instance.SimulateInternationalFont == SimulateInternationalFont.No)
        return new Fontset(bytes, offset);

    // Auto mode: detect based on firstChar convention
    // firstChar == 32 indicates standard English-only font
    return firstChar == 32
        ? new InternationalSimulatedFontSet(bytes, offset)
        : new Fontset(bytes, offset);
}
```

---

### 9. 🟡 MEDIUM: Inefficient Noise Generation in Free.cs

**File:** [src/Graphics/Free.cs](src/Graphics/Free.cs#L18)  
**Type:** Unnötige Allocations / Inefficient Pattern  
**Severity:** MEDIUM

**Problem:**
`GenerateNoise()` is an infinite enumerable using `while(true)` yield. Later truncated with `.Take()`. Wasteful and unclear.

```csharp
private IEnumerable<byte> GenerateNoise(params byte[] values)
{
    Random r = new Random(0x4701);
    while (true)  // ← Infinite loop
    {
        yield return values[r.Next(values.Length)];
    }
}

// Usage:
public Bytemap PanelGrey
{
    get
    {
        if (_panelGrey == null)
        {
            _panelGrey = new Bytemap(16, 16)
                .FromByteArray(GenerateNoise(7, 22).Take(16 * 16).ToArray());  // ← Take() needed
        }
        return _panelGrey;
    }
}
```

**Solution:**
Make size explicit:

```csharp
private byte[] GenerateNoise(int size, params byte[] values)
{
    byte[] result = new byte[size];
    Random r = new Random(0x4701);
    for (int i = 0; i < size; i++)
    {
        result[i] = values[r.Next(values.Length)];
    }
    return result;
}

public Bytemap PanelGrey
{
    get
    {
        if (_panelGrey == null)
        {
            _panelGrey = new Bytemap(16, 16)
                .FromByteArray(GenerateNoise(16 * 16, 7, 22));
        }
        return _panelGrey;
    }
}
```

---

### 10. 🟡 MEDIUM: Static Empty Palette Allocation in Picture.cs

**File:** [src/Graphics/Picture.cs](src/Graphics/Picture.cs#L35)  
**Type:** Unnötige Allocations  
**Severity:** MEDIUM

**Problem:**
`EmptyPalette` is allocated as a static array each time the class loads. Could be cached or lazy-loaded.

```csharp
private static Colour[] EmptyPalette = Enumerable.Range(0, 256)
    .Select(_ => new Colour()).ToArray();  // ← New array at class load time
```

**Solution:**
Use a lazy-initialized readonly array:

```csharp
private static readonly Colour[] EmptyPalette = CreateEmptyPalette();

private static Colour[] CreateEmptyPalette() =>
    Enumerable.Range(0, 256).Select(_ => new Colour()).ToArray();
```

Or better, use a shared constant:

```csharp
public Picture(int width, int height) : this(width, height, CreateDefaultPalette())
{
}

private static Palette CreateDefaultPalette()
{
    var palette = new Palette(256);
    // Initialize with transparent colors if needed
    return palette;
}
```

---

### 11. 🟡 MEDIUM: Missing Null Check in PalaceResourcesDelegate.cs

**File:** [src/Graphics/PalaceResourcesDelegate.cs](src/Graphics/PalaceResourcesDelegate.cs#L16)  
**Type:** NullReference-Risiko  
**Severity:** MEDIUM

**Problem:**
The `getPictureByName` delegate is checked in constructor, but returned pictures are never validated. If the delegate returns null, methods will throw `NullReferenceException`.

```csharp
public sealed class PalaceResourcesDelegate(
    Func<string, Picture> getPictureByName,
    IPalaceSpriteLayout palaceSpriteLayout = null)
{
    private readonly Func<string, Picture> _getPictureByName = 
        getPictureByName ?? throw new ArgumentNullException(nameof(getPictureByName));

    private Picture GetCastleSourceImage(int level) => 
        _getPictureByName($"CASTLE{level}");  // ← Could return null

    private Picture GetCastleSourcePartImage(
        int level,
        PalacePart part,
        PalaceStyle style,
        PalacePictureLayout layout,
        int offsetX = 0)
    {
        PalacePartSourceRect sourceRect = _palaceSpriteLayout.GetPartSourceRect(part, style, layout);
        return GetCastleSourceImage(level)[sourceRect.X + offsetX, ...]  // ← NRE if null
    }
}
```

**Solution:**
Add defensive null checks:

```csharp
private Picture GetCastleSourceImage(int level)
{
    var picture = _getPictureByName($"CASTLE{level}");
    if (picture == null)
        throw new InvalidOperationException($"Castle image for level {level} not found");
    return picture;
}
```

---

### 12. 🟡 MEDIUM: Off-by-One Risk in DrawRectangle3D (BitmapExtensions.cs)

**File:** [src/Graphics/BitmapExtensions.cs](src/Graphics/BitmapExtensions.cs#L80)  
**Type:** Off-by-one Fehler / Logic Error  
**Severity:** MEDIUM

**Problem:**
The loop logic is confusing with mixed boundary conditions. The `ww` and `hh` calculations use `-1`, creating potential off-by-one errors.

```csharp
public static IBitmap DrawRectangle3D(...)
{
    if (width < 0) width = bitmap.Width() - left;
    if (height < 0) height = bitmap.Height() - top;
    int ww = (left + width - 1), hh = (top + height - 1);  // ← Confusing
    for (int yy = top; yy <= hh; yy++)  // ← Includes hh
    {
        if (yy >= bitmap.Height()) break;  // ← Redundant with bounds check
        if (bitmap.OutBoundY(yy)) continue;  // ← Already checked
        for (int xx = left; xx <= ww; xx++)
        {
            if (xx >= bitmap.Width()) break;
            if (bitmap.OutBoundX(xx)) continue;  // ← Redundant
            if (yy == top || xx == ww)
                bitmap.Bitmap[xx, yy] = colourDark;
            else if (yy == hh || xx == left)
                bitmap.Bitmap[xx, yy] = colourLight;
        }
    }
    return bitmap;
}
```

**Solution:**
Clarify with explicit end-exclusive logic:

```csharp
public static IBitmap DrawRectangle3D(...)
{
    if (width < 0) width = bitmap.Width() - left;
    if (height < 0) height = bitmap.Height() - top;

    // Clamp to bitmap bounds
    int endX = Math.Min(left + width, bitmap.Width());
    int endY = Math.Min(top + height, bitmap.Height());

    for (int y = top; y < endY; y++)
    {
        for (int x = left; x < endX; x++)
        {
            // Draw light edges
            if (y == top || x == left)
                bitmap.Bitmap[x, y] = colourLight;
            // Draw dark edges
            else if (y == endY - 1 || x == endX - 1)
                bitmap.Bitmap[x, y] = colourDark;
        }
    }
    return bitmap;
}
```

---

## Summary Table

| Issue | File | Type | Severity | Category |
|-------|------|------|----------|----------|
| Race condition in lazy init | Icons.cs | Race Condition | KRITISCH | Thread-Safety |
| Cache dictionary race | Resources.cs | Race Condition | KRITISCH | Thread-Safety |
| Memory leak in sprite collection | CachedSpriteCollection.cs | Memory Leak | WICHTIG | Resource Mgmt |
| Double-dispose risk | CachedSprite.cs | Memory Leak | WICHTIG | Resource Mgmt |
| Index out of range | Resources.cs | NullReference | WICHTIG | Validation |
| Exception swallowing | Resources.cs | Error Handling | WICHTIG | Robustness |
| String allocation | Resources.cs | Performance | MEDIUM | Allocation |
| Fragile font detection | FontSetFactory.cs | Logic Error | MEDIUM | Validation |
| Infinite enumerable | Free.cs | Performance | MEDIUM | Efficiency |
| Static palette allocation | Picture.cs | Performance | MEDIUM | Allocation |
| Missing null check | PalaceResourcesDelegate.cs | NullReference | MEDIUM | Validation |
| Off-by-one loop | BitmapExtensions.cs | Logic Error | MEDIUM | Correctness |

---

## Recommended Actions

### Immediate (This Sprint)

1. Fix race conditions in `Icons.cs` and `Resources.cs` — **HIGH PRIORITY**
2. Add null validation in `Resources.LoadFonts()` and `FontSetFactory.Create()`
3. Implement proper `IDisposable` in sprite cache classes

### Near-term (Next Sprint)

1. Refactor string-based caching to value-type tuples
2. Improve error handling in `GetCivilopediaText()`
3. Review thread-safety across all static caches

### Nice-to-have

1. Optimize `GenerateNoise()` pattern
2. Clarify `DrawRectangle3D()` boundary logic
3. Add unit tests for cache consistency under concurrent access

---

## Additional Performance Findings (Delta)

### 13. 🟠 WICHTIG: O(N) file scan on every resource lookup in PicFile.GetFilename

**File:** [src/Graphics/ImageFormats/PicFile.cs](src/Graphics/ImageFormats/PicFile.cs#L255)  
**Type:** Performance / I/O  
**Severity:** WICHTIG

**Problem:**
`GetFilename()` iterates all files in the data directory for each lookup to handle case-insensitive matching:

```csharp
foreach (string fileEntry in Directory.GetFiles(Settings.Instance.DataDirectory))
{
    if (Path.GetFileName(fileEntry).ToLower() != $"{filename.ToLower()}.pic") continue;
    return fileEntry;
}
```

This is expensive when many files exist and when resources are requested often.

**Possible solution:**
- Build a one-time dictionary cache (`Dictionary<string, string>`) from lowercase filename to full path.
- Rebuild only when data directory changes.
- Use `StringComparer.OrdinalIgnoreCase` instead of repeated `ToLower()` allocations.

### 14. 🟠 WICHTIG: Double copy on every cached resource read in Resources indexer

**File:** [src/Graphics/Resources.cs](src/Graphics/Resources.cs#L222)  
**Type:** Performance / Allocation Pressure  
**Severity:** WICHTIG

**Problem:**
`Resources[this[string filename]]` always returns a new `Picture` copy, even on cache hits, and also creates/copies once again after cache insert.

```csharp
if (_cache.ContainsKey(key))
{
    return new Picture(_cache[key].Bitmap, _cache[key].Palette);
}
...
if (!_cache.ContainsKey(key)) _cache.Add(key, output);
return new Picture(_cache[key].Bitmap, _cache[key].Palette);
```

For rendering-heavy screens, this creates sustained GC pressure.

**Possible solution:**
- Decide between immutable shared picture vs. copy-on-write behavior.
- If mutability is required, keep as-is for safety but add specialized APIs for read-only sprite access without copy.
- Replace `ContainsKey` + indexer with `TryGetValue` to avoid duplicate dictionary lookup.

### 15. 🟡 MEDIUM: Repeated expensive existence checks in tile generation

**File:** [src/Graphics/Sprites/MapTile.cs](src/Graphics/Sprites/MapTile.cs#L24)  
**Type:** Performance / Repeated I/O  
**Severity:** MEDIUM

**Problem:**
Many methods call `Resources.Exists(...)` repeatedly (`GetLandBase`, `GetOceanBase`, `GetOceanLayer`, `GetRiverLayer`, etc.). Depending on backend/cache behavior this can become noisy and redundant.

**Possible solution:**
- Cache existence flags for key files (`SP257`, `SPRITES`, `TER257`, `SP299`) once per graphics mode change.
- Centralize in a small helper or static readonly lazy flags invalidated when resources are reloaded.

### 16. 🟡 MEDIUM: Delegate pipeline allocates intermediate collections in text rendering

**File:** [src/Graphics/Resources.cs](src/Graphics/Resources.cs#L116)  
**Type:** Performance / Allocation  
**Severity:** MEDIUM

**Problem:**
`GetText` builds `List<Bytemap>`, then iterates again for size, then again for blit. Multiple passes and allocations per string.

**Possible solution:**
- Two-pass without list (first pass width/height, second pass draw), or
- Use pooled list/array for frequent short texts.
- Cache whole rendered strings where appropriate (e.g., UI labels), not only single letters.

### 17. 🟡 MEDIUM: Additional avoidable allocations in GIF parsing

**File:** [src/Graphics/ImageFormats/GifFile.cs](src/Graphics/ImageFormats/GifFile.cs#L206)  
**Type:** Performance / Allocation  
**Severity:** MEDIUM

**Problem:**
Decoder appends chunks into `List<byte>` and then calls `ToArray()` before decode:

```csharp
List<byte> lzwData = new List<byte>();
...
byte[] pixels = LZW.Decode(lzwData.ToArray(), true, false, minCode, 12);
```

This causes at least one extra full-buffer copy.

**Possible solution:**
- Pre-compute expected compressed block length and allocate once.
- Use `MemoryStream` + `GetBuffer`/`TryGetBuffer` style flow or pooled byte buffers.

### 18. 🟡 MEDIUM: Hot path includes repeated Unicode normalization

**File:** [src/Graphics/Resources.cs](src/Graphics/Resources.cs#L119)  
**Type:** Performance / CPU  
**Severity:** MEDIUM

**Problem:**
`GetText` normalizes every string with `Normalize(FormC)` even for plain ASCII text.

```csharp
text = text.Normalize(NormalizationForm.FormC);
```

For large volumes of UI text this adds avoidable CPU.

**Possible solution:**
- Fast-path ASCII strings (skip normalization when all chars `< 128`).
- Normalize only on cache miss for whole-string rendering cache.
