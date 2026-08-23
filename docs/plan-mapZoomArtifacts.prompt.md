## Plan: Map-Zoom-Artefakte durch Rendering in Zielauflösung elimininieren

**TL;DR**: Statt Tiles immer bei 16px/tile zu rendern und dann zu skalieren, berechne die Zielgröße (z.B. 8px bei 50% Zoom) und **rendere die Tiles direkt in dieser Größe**. Das elimininiert die schlimmsten Artefakte durch nur eine effiziente Skalierungsoperation pro Frame.

---

## **3 Bewertete Optionen**

| Ansatz | Aufwand | Qualität | Risiko | Bemerkung |
|--------|---------|----------|--------|-----------|
| **1. Bilinear-Skalierung** | 2-3h | ⭐⭐ | Niedrig | Reduziert Artefakte, eliminiert nicht |
| **2. Native Resolution** | 50-80h | ⭐⭐⭐⭐ | SEHR HOCH | UI-Katastrophe, müsste 100+ Dateien ändern |
| **3. Zielauflösung-Rendering** ✅ | 8-12h | ⭐⭐⭐⭐⭐ | Niedrig | EMPFOHLEN — beste Balance |

---

## **Empfohlener Plan: Approach 3 + Bilinear-Hybrid**

**Wie es funktioniert:**
- Aktuell: `Tiles.ToBitmap()` → always 16×16 per tile → `ScaleBitmap()` auf Canvas → GPU-Scale auf Screen
- **Neu**: `Tiles.ToBitmap(_tilePixelSize)` → renders direct at zoom-size → **eine** bilinare Skalierung

### **Implementierungs-Schritte**

1. **Bilinear ScaleBitmap implementieren** (2-3h)  
   - Datei: [src/Screens/GamePlayPanels/GameMap.cs](src/Screens/GamePlayPanels/GameMap.cs#L101-L120)
   - Ersetze nearest-neighbor durch Bilinear-Interpolation  
   - Alle 4 Aufrufer funktionieren unverändert (same signature)

2. **pixelSize-Parameter zu Tiles.ToBitmap() hinzufügen** (6-8h)
   - [src/Extensions/TileExtensions.cs#L299-L360](src/Extensions/TileExtensions.cs#L299-L360): Single-Tile `ToBitmap(pixelSize = 16)`
   - [src/Extensions/TileExtensions.cs#L226-L257](src/Extensions/TileExtensions.cs#L226-L257): Tile-Array `ToBitmap(pixelSize = 16)`  
   - Wenn pixelSize ≠ 16: Rendere bei 16px, skaliere dann bilinear auf Ziel  
   - *Oder besser*: Rendere direkt bei niedrigerem pixelSize (schneller, weniger Speicher)

3. **GameMap.cs Aufrufer updaten** (1-2h)
   - [GameMap.cs#L325](src/Screens/GamePlayPanels/GameMap.cs#L325): `Tiles.ToBitmap(_tilePixelSize, ...)`
   - [GameMap.cs#L339](src/Screens/GamePlayPanels/GameMap.cs#L339): Entferne `ScaleBitmap()` — nicht mehr nötig!  
   - [GameMap.cs#L365](src/Screens/GamePlayPanels/GameMap.cs#L365): Entferne `ScaleBitmap()` hier auch
   - Update: 3 Stellen nur

4. **Terrain-Editor-Overlays** (~1h)
   - [src/Screens/GamePlayPanels/GameTerrainEditorRenderDelegate.cs#L72-L80](src/Screens/GamePlayPanels/GameTerrainEditorRenderDelegate.cs#L72-L80): Unit/Pinsel-Preview  
   - Entferne dortige Skalierung

5. **Optional: Min Tile-Size** (<1h)
   - [src/Screens/GamePlayPanels/GameMapZoomDelegate.cs#L144-L147](src/Screens/GamePlayPanels/GameMapZoomDelegate.cs#L144-L147): Clampe auf min. 2×2px (keine 1px-Würfel)

### **Dateien die geändert werden**
- [src/Screens/GamePlayPanels/GameMap.cs](src/Screens/GamePlayPanels/GameMap.cs) — ScaleBitmap() + 3 Aufrufe  
- [src/Extensions/TileExtensions.cs](src/Extensions/TileExtensions.cs) — pixelSize-Parameter  
- [src/Screens/GamePlayPanels/GameTerrainEditorRenderDelegate.cs](src/Screens/GamePlayPanels/GameTerrainEditorRenderDelegate.cs) — Overlay-Rendering

### **Dateien die NICHT geändert werden**
- `BaseScreen.cs` — Canvas bleibt 320×200  
- `GameMapZoomDelegate.cs` — Zoom-Berechnungen bleiben gleich
- `GameWindow.Graphics.cs` — SDL-Rendering unverändert
- Alle UI-Screens — Menüs, Dialoge, etc. unverändert

### **Verifikation**
1. ✅ Build ohne Fehler (`build-strict-errors`)
2. ✅ Test Unit-Tests (`xunit/CivOne.UnitTests.csproj`)
3. ✅ Visuell: Zoom durch alle 10 Stufen (125%, 200%, ..., 1000%)  
4. ✅ Visuell: Bei jedem Zoom sollten Kanten **glatt** (bilinear) statt **pixelig** (nearest-neighbor) sein
5. ✅ Funktional: Mausklicks / Tile-Selection funktioniert noch (Koordinaten)
6. ✅ Performance: FPS nicht merklich gesunken

---

## **Risikoanalyse**

| Risiko | Wahrscheinlichkeit | Mitigation |
|--------|-------------------|-----------|
| Tile-Rendering bei 1px kaputtgehen | Mittel | Min. Tile-Size auf 2×2 clampen |
| Speicher-Overhead | Niedrig | Nur Tiles bei Zielgröße, nicht 16px |
| Koordinaten-Mapping beim Terrain-Editor | Mittel | GameTerrainEditorRenderDelegate teste sorgfältig |
| Performance-Regression | Niedrig | Bilinear ist negligible (~1-2ms), direktes Rendering schneller |

---

## **Zusätzliche Informationen**

### **Aktuelle Architektur**
- **Logische Canvas**: 320×200 Pixel (unabhängig von physischer Bildschirmgröße)
- **Base Tile Size**: 16×16 Pixel
- **Map Grid**: 80×50 Tiles
- **Rendering Pipeline**: Tiles → full resolution bitmap (1280×800) → CPU-skaliert auf Canvas → GPU-skaliert auf Screen

### **Artefakt-Root-Causes (aktuell)**
1. **Integer-Rounding bei CPU-Skalierung**: In `ScaleBitmap()` entstehen bei nicht-ganzzahligen Skalierungsfaktoren ungleichmäßige Pixelabbildungen
2. **Nearest-Neighbor Interpolation**: Keine Glättung, nur harte Pixelsampling
3. **Kaskadierte Downsampling**: 16px → 2-16px/tile (bei Zoom) → weitere GPU-Skalierung = Informationsverlust
4. **Keine Mipmaps**: Heruntergeskallte Informationen sind permanent verloren

### **Zentrale Komponenten**
- `MapZoomSettings.cs`: 10 fixe Zoom-Stufen (1000, 900, ..., 125 Basis Points)
- `GameMap.cs` L107: `ScaleBitmap()` - CPU-Skalierung
- `GameMapZoomDelegate.cs`: Zoom-Status, Viewport-Metriken
- `GameWindow.Graphics.cs`: SDL GPU-Rendering mit Texture-Cache
- `BaseScreen.cs`: Basis 320×200 Canvas

---

## **Nächste Schritte nach Plan-Approve**

1. Detaillierte Code-Analyse von `TileExtensions.cs` ToBitmap-Signaturen
2. Bilinear-Implementierung in `ScaleBitmap()` vorbereiten
3. pixelSize-Parameter durch Aufrufer-Kette tracken
4. Feature-Branch erstellen & implementieren
5. Tests + Visuelles QA

## **Plan-Update v2 (alle 6 Punkte bestätigt)**

### **Entscheidungen**

| Punkt | Entscheidung | Impact | Aufwand |
|------|--------------|--------|---------|
| 1. Standard + Fallback | ✅ Zielauflösung-Rendering als Standard, Legacy-Pfad per separatem Setting als Fallback (UI im AspectRatioMenu) | Niedrig | +0.5h |
| 2. Aspect Ratio (Expand etc.) | ✅ Keine Kollision erwartet (Map bleibt in 320x200 Canvas-Pipeline) | Keine | — |
| 3. Menüs/Dialoge/Overlays | ✅ Bleiben unverändert (nicht im Map-Tile-Pfad) | Keine | — |
| 4. DestroyUnit | ✅ Entscheidung: v1 bleibt Legacy/fixed Rendering, nicht an Map-Zoom koppeln | Niedrig | +0-0.5h |
| 5. Maus-/Tile-Mapping | ✅ Delegate-Klasse für einheitliches Mapping (statt verstreuter Divisionen) | Hoch | +3-4h |
| 6. MiniMap Viewport-Rect | ✅ Rechteck-Berechnung auf aktuelle sichtbare Tile-Dimensionen normalisieren | Mittel | +1h |

**Revidierter Aufwand gesamt**: 15-18h

---

## **Details zu den offenen Kernpunkten**

### **1) Standard + Fallback**

- Neuer Default: Zielauflösung-Rendering aktiv.
- Fallback: separates Setting (z.B. `ExpandWithNativeResolution`) statt neuer AspectRatio-Enum.
- Setup-UI: Menüpunkt im `AspectRatioMenu`, nur relevant wenn `AspectRatio = Expand`.
- Effektive Aktivierung: `Settings.AspectRatio == AspectRatio.Expand && Settings.ExpandWithNativeResolution`.
- Zweck: risikoarmer Rollback ohne Code-Revert.

### **4) DestroyUnit – was soll entschieden werden?**

**Empfehlung (für v1):** DestroyUnit bleibt auf Legacy/fixed Rendering.

Begründung:
- Screen ist separater Effekt-Screen, nicht Teil des normalen Map-Tile-Renderpfads.
- Entkopplung reduziert Risiko für Animations-Timing, Sprite-Offsets und Übergänge.
- Ziel dieses Changes bleibt Zoom-Artefakte im GameMap-Flow, nicht globale VFX-Neuinterpretation.

Optional v2:
- Später optionaler Follow-up-Task: DestroyUnit zoom-aware machen, falls visuelle Inkonsistenz auffällt.

### **5) Delegate-Klasse für Mapping (gemäß Instructions)**

Ja, sinnvoll und empfohlen.

Vorgeschlagene Klasse:
- `GameMapCoordinateMappingWrapper` (Delegate wird nun Wrapper genannt, da delegate ein C#-Keyword ist)

Verantwortung:
- `CanvasPixelToLocalTile(Point pixel, int tilePixelSize, int tilesX, int tilesY)`
- `LocalTileToCanvasPixel(Point localTile, int tilePixelSize)`
- `CanvasPixelToWorldTile(Point pixel, int tilePixelSize, int mapX, int mapY, int tilesX, int tilesY)`
- Einheitliche Clamp-/Rounding-Regeln zentral (kein hardcoded `/16` mehr in Callern)

Geplante Umstellung von Call-Sites:
- `GameMapZoomDelegate` (inkl. `GetWorldTileAtPixel`-Logik)
- `Goto.fromCanvas` (oder äquivalente Goto-Mapper)
- `GameTerrainEditorDelegate`
- weitere direkte `_tilePixelSize`-Divisionen im Input-Handling

### **6) DrawMiniMap Viewport-Rechteck**

- Rechteckgröße und -position aus den **aktuell sichtbaren World-Tiles** ableiten (`_mapX/_mapY/_tilesX/_tilesY`).
- Damit bleibt das weiße Rechteck korrekt bei allen Zoomstufen.

---

## **Aktualisierte Implementierungs-Schritte (v2)**

1. Bilinear `ScaleBitmap()` implementieren.
2. `Tiles.ToBitmap(pixelSize = 16)` erweitern und Zielauflösung-Pfad aktivieren.
3. GameMap-Aufrufer auf pixelSize-Rendering umstellen; doppelte Skalierung entfernen.
4. Terrain-Editor-Overlays auf neuen Renderpfad ausrichten.
5. **Neu:** `GameMapCoordinateMappingWrapper` einführen und Mapping-Callsites migrieren. (Delegate wird nun Wrapper genannt, da delegate ein C#-Keyword ist)
6. **Neu:** MiniMap-Viewport-Rechteck robust an sichtbare Tile-Fläche koppeln.
7. Separates Fallback-Setting einbauen (Setup/AspectRatioMenu) und dokumentieren.
8. Verifikation: Build, relevante Tests, visuelle Zoom- und Klick-Checks.

---

## **AI-Implementierungs-Prompt (detailliert, Punkt 1 Entscheidung)**

Nutze folgenden Prompt 1:1 für die Implementierung.

### **Ziel**

Implementiere einen Fallback-Schalter für das neue Zielauflösung-Rendering als **separates Setting**.
Wichtig: **Kein** neuer Wert in `AspectRatio` (also kein `ExpandNative`).

Aktivierungsregel:
- `nativeMapZoomRenderingEnabled = (Settings.AspectRatio == AspectRatio.Expand) && Settings.ExpandWithNativeResolution`

### **Technische Entscheidung (final)**

1. `AspectRatio` bleibt unverändert (`Auto`, `Fixed`, `Scaled`, `ScaledFixed`, `Expand`).
2. Neues bool-Setting in `Settings`: `ExpandWithNativeResolution`.
3. Setup-Menüpunkt in `AspectRatioMenu` zum Umschalten dieses bools.
4. Menüpunkt nur dann aktiviert/sichtbar sinnvoll, wenn `AspectRatio == Expand`.
5. Default-Wert für neues Setting: `true` (neuer Renderpfad standardmäßig aktiv, Legacy bleibt Fallback).

### **Zu ändernde Dateien**

1. [src/Settings.cs](src/Settings.cs)
- Neues Feld anlegen: `_expandWithNativeResolution = true;`
- Neues Property anlegen:
   - `internal bool ExpandWithNativeResolution { get; set; }`
   - Setter persistiert via `SetSetting("ExpandWithNativeResolution", value.YesNo())` oder konsistentes bool-Format gemäß bestehendem Pattern.
- Beim Laden aus Config: `GetSetting("ExpandWithNativeResolution", ref _expandWithNativeResolution)` ergänzen.
- Sicherstellen, dass fehlender Alt-Wert auf `true` fällt (Backward Compatibility).

2. [src/Screens/Setup.cs](src/Screens/Setup.cs)
- In `SettingsMenu` optionalen Hinweistext ergänzen, dass Native-Map-Rendering nur für `Expand` gilt.
- In `AspectRatioMenu` neuen Eintrag ergänzen, z.B.:
   - Label: `Expand with native map rendering: {Yes/No}`
   - Beschreibung:
      - Aktiv: Karte rendert direkt in Ziel-Tilegröße (bessere Zoom-Qualität).
      - Inaktiv: Legacy-Pfad (16px + Downscale) als Fallback.
- Verhalten:
   - `OnSelect`: `Settings.ExpandWithNativeResolution = !Settings.ExpandWithNativeResolution`
   - `SetEnabled(() => Settings.AspectRatio == AspectRatio.Expand)` oder direkte Disable-Variante für Nicht-Expand.
   - `SetActive(...)` so, dass aktueller Zustand klar sichtbar ist.

3. [src/Extensions.cs](src/Extensions.cs)
- Optional Helfertext für Bool-Label wiederverwenden (falls erforderlich).
- Keine Änderung an `AspectRatio.ToText()` für neue Enum-Werte, da es keinen neuen Enum-Wert gibt.

4. Map-Render-Callsites (im nächsten Implementierungsschritt)
- In [src/Screens/GamePlayPanels/GameMap.cs](src/Screens/GamePlayPanels/GameMap.cs) neuen bool auswerten:
   - Wenn aktiv: Zielauflösungspfad.
   - Wenn inaktiv: Legacy-Fallback.
- In [src/Screens/GamePlayPanels/GameMapZoomDelegate.cs](src/Screens/GamePlayPanels/GameMapZoomDelegate.cs) keine Enum-Änderung nötig.

### **Nicht ändern**

1. [src/Enums/AspectRatio.cs](src/Enums/AspectRatio.cs) (kein neuer Enum-Wert).
2. Runtime-AspectRatio-Handling in [runtime/sdl/src/GameWindow.Graphics.cs](runtime/sdl/src/GameWindow.Graphics.cs) nur wegen dieses Schalters nicht umbauen.

### **Akzeptanzkriterien**

1. Setup zeigt neuen Toggle im Aspect-Ratio-Kontext.
2. `AspectRatio` Enum unverändert.
3. Setting wird persistent gespeichert und geladen.
4. Bei `AspectRatio != Expand` hat Toggle keine funktionale Auswirkung.
5. Bei `AspectRatio == Expand` kann zwischen neuem und Legacy-Map-Rendering umgeschaltet werden.
6. Build bleibt grün.

### **Kurzbegründung für Reviewer**

- AspectRatio steuert Fenster-/Canvas-Skalierung.
- Native-Map-Rendering steuert internen Map-Renderpfad.
- Trennung verhindert Seiteneffekte in SDL-Aspect-Ratio-Switches und reduziert Regression-Risiko.

