# Startup Wizard

## 1) What Is This Wizard?

The Startup Wizard is a DOS-style setup flow that can run before normal game startup.

It helps players finish required setup tasks, for example:

- choose a language
- locate original Civilization DOS data files
- toggle sound
- open project/community links

Main screen implementation: [WizardScreen.cs](WizardScreen.cs)

## 2) Why Do We Need It?

Without setup, players can hit missing files or wrong defaults.

The wizard solves this by guiding users through small pages in a fixed order.

Startup trigger logic: [RuntimeHandler.cs](../../RuntimeHandler.cs)

Current startup behavior:

- wizard opens when setup is needed (or data files are missing), except on OSX
- if wizard is not shown and data files are missing, fallback screen `MissingFiles` is used

## 3) Quick Mental Model (Beginner)

Think of the wizard as 4 small parts:

1. **State**: current step + user choices  
   File: [WizardEngine.cs](WizardEngine.cs)
2. **Page Builder**: creates page content from state  
   File: [WizardPageBuilder.cs](WizardPageBuilder.cs)
3. **Action Handler**: executes user actions (next, back, browse folder, finish)  
   File: [WizardActionHandler.cs](WizardActionHandler.cs)
4. **Screen + Rendering**: handles keyboard/mouse and draws DOS UI  
   Files: [WizardScreen.cs](WizardScreen.cs), [WizardRenderingDelegate.cs](WizardRenderingDelegate.cs), [WizardMouseMarkerDelegate.cs](WizardMouseMarkerDelegate.cs)

## 4) Data Structures

### Wizard state

`WizardState` stores:

- `PageIndex`
- `StatusMessage`
- `SelectedLanguagePostfix`
- `DataFolder`
- `SoundEnabled`

See: [WizardEngine.cs](WizardEngine.cs)

### Page model

`WizardPage` contains:

- title
- body lines
- selectable entries
- optional links
- optional entry Y offset

See: [WizardPage.cs](WizardPage.cs)

### Menu entry model

`WizardEntry` defines one selectable row.

Key properties:

- `Number` (display/index)
- `Hotkey` (optional)
- `Text`
- `Action` (`SelectLanguage`, `BrowseDataFolder`, `Continue`, `Back`, `ToggleSound`, `Finish`)
- `Enabled` (for validation gating)
- `Value` (optional payload, used by language selection)

See: [WizardEntry.cs](WizardEntry.cs)

## 5) Existing Features

Current features already implemented:

| Feature | Description | Where It's Used |
|---------|-------------|-----------------|
| **Menu items** | Numbered entries (1-9) users activate by pressing number keys or clicking | [WizardEntry.cs](WizardEntry.cs) defines `Number` property; [WizardScreen.cs](WizardScreen.cs) handles digit input in `KeyDown()` and mouse clicks in `MouseDown()` |
| **Hotkeys** | Single-character keyboard shortcuts (e.g. `c` = Continue, `b` = Back) | [WizardEntry.cs](WizardEntry.cs) defines `Hotkey` property; [WizardScreen.cs](WizardScreen.cs) matches hotkeys in `TryActivateHotkey()` |
| **Disabled entries** | Menu items that cannot be activated (grayed out) | [WizardEntry.cs](WizardEntry.cs) defines `Enabled` property; [WizardPageBuilder.cs](WizardPageBuilder.cs) sets `Enabled = false` on Continue button when data files missing; [WizardRenderingDelegate.cs](WizardRenderingDelegate.cs) renders disabled entries in muted color |
| **Open folder dialog** | Browse for DOS Civilization data directory | [WizardEntryAction.cs](WizardEntry.cs) enum value `BrowseDataFolder`; [WizardActionHandler.cs](WizardActionHandler.cs) method `HandleBrowseDataFolder()` calls injected `_browseFolder()` delegate; [WizardPageBuilder.cs](WizardPageBuilder.cs) builds data folder page |
| **Toggle behavior** | Switch options on/off (e.g. sound) and persist choice | [WizardEntryAction.cs](WizardEntry.cs) enum value `ToggleSound`; [WizardActionHandler.cs](WizardActionHandler.cs) `Execute()` toggles `engine.SoundEnabled` and calls `Settings.Instance.Sound = ...`; [WizardPageBuilder.cs](WizardPageBuilder.cs) displays current state |
| **Open URL links** | Clickable hyperlinks with browser fallback and clipboard copy | [WizardPage.cs](WizardPage.cs) field `Links` of type `(string Label, string Url)`; [WizardRenderingDelegate.cs](WizardRenderingDelegate.cs) renders links in blue color and tracks hit areas; [WizardScreen.cs](WizardScreen.cs) calls `_actionHandler.OpenUrl()` on click; [WizardActionHandler.cs](WizardActionHandler.cs) uses injected `IBrowserService` |
| **Mouse hit-testing** | Click entries and links with accurate rectangular hit areas | [WizardRenderingContext.cs](WizardRenderingContext.cs) collections `EntryHitAreas` and `LinkAreas`; [WizardRenderingDelegate.cs](WizardRenderingDelegate.cs) records rectangles during render; [WizardScreen.cs](WizardScreen.cs) checks clicks against areas in `MouseDown()` |
| **Status line** | Bottom-of-screen feedback messages for user actions | [WizardState](WizardEngine.cs) property `StatusMessage`; [WizardActionHandler.cs](WizardActionHandler.cs) assigns messages like `"Data files copied successfully."`; [WizardRenderingDelegate.cs](WizardRenderingDelegate.cs) renders at bottom row |
| **Resize-aware DOS rendering** | Scales 80x25 DOS grid to fill window, keeps aspect ratio | [WizardScreen.cs](WizardScreen.cs) attribute `[ScreenResizeable]`; method `RenderCurrentPage()` calculates scale and box; [WizardRenderingContext.cs](WizardRenderingContext.cs) stores `Scale`, `Box`, `Cols`, `Rows` |
| **Dependency injection** | Constructor injection of page builder and action handler for testability | [WizardScreen.cs](WizardScreen.cs) public constructor uses defaults, internal constructor accepts `IWizardPageBuilder` and `IWizardActionHandler`; [IWizardPageBuilder.cs](IWizardPageBuilder.cs) and [IWizardActionHandler.cs](IWizardActionHandler.cs) define contracts |

Key references:

- [IWizardPageBuilder.cs](IWizardPageBuilder.cs)
- [IWizardActionHandler.cs](IWizardActionHandler.cs)
- [WizardActionResult.cs](WizardActionResult.cs)

## 6) Input and Flow

User input supported:

- keyboard digits `1`..`9`
- numpad digits
- hotkeys from `WizardEntry.Hotkey`
- `Esc` for back/close behavior
- left mouse click on entries and links

Input handling code: [WizardScreen.cs](WizardScreen.cs)

Flow rule today:

- page order is controlled by `PageIndex`
- `MoveNext`/`MoveBack` changes page
- page content is rebuilt from current state every refresh

## 7) Validation: How Data Validity Is Checked

Validation is done in two layers.

### Layer A: UI-level gating (recommended for most cases)

Disable menu entries until data is valid.

Example from data-folder page (continue disabled until files exist):

```csharp
bool hasDataFiles = FileSystem.DataFilesExist();

new WizardEntry
{
    Number = 2,
    Text = ContinueText(),
    Action = WizardEntryAction.Continue,
    Enabled = hasDataFiles,
    Hotkey = HotkeyContinue
}
```

Reference: [WizardPageBuilder.cs](WizardPageBuilder.cs)

### Layer B: Action-level validation (defensive)

Action handler checks validity while executing actions.

Example from folder browse action:

```csharp
if (!FileSystem.CopyDataFiles(path) || !FileSystem.DataFilesExist())
{
    engine.StatusMessage = T("Copying data files failed.");
    return;
}
```

Reference: [WizardActionHandler.cs](WizardActionHandler.cs)

Recommended pattern:

- disable invalid actions in page builder
- keep safety checks in action handler
- write user feedback to `StatusMessage`

## 8) How To Add a New Page

Example: add a new page "Gameplay Preset" between Sound and Final page.

### Step 1: Extend state

Add new state fields in [WizardEngine.cs](WizardEngine.cs), for example:

```csharp
public string GameplayPreset { get; set; } = "Normal";
```

If you increase total page count, update:

- `LastPageIndex`

### Step 2: Add/extend actions

If needed, add action enum value in [WizardEntry.cs](WizardEntry.cs):

```csharp
SelectGameplayPreset,
```

Then implement handling in [WizardActionHandler.cs](WizardActionHandler.cs).

### Step 3: Add page builder method

In [WizardPageBuilder.cs](WizardPageBuilder.cs):

1. Add a new `Build...Page(...)` method.
2. Return entries/lines for that page.
3. Add switch mapping in `Build(...)`.

Minimal example:

```csharp
private WizardPage BuildGameplayPresetPage(WizardState engine)
{
    return new WizardPage
    {
        Title = T("Startup Wizard: Gameplay"),
        Lines =
        [
            T("Choose gameplay preset."),
            TF("Current: {0}", engine.GameplayPreset)
        ],
        Entries =
        [
            new WizardEntry { Number = 1, Text = T("Normal"), Action = WizardEntryAction.SelectGameplayPreset, Value = "Normal" },
            new WizardEntry { Number = 2, Text = T("Hard"), Action = WizardEntryAction.SelectGameplayPreset, Value = "Hard" },
            new WizardEntry { Number = 3, Text = ContinueText(), Action = WizardEntryAction.Continue, Hotkey = 'c' },
            new WizardEntry { Number = 4, Text = BackText(), Action = WizardEntryAction.Back, Hotkey = 'b' }
        ]
    };
}
```

### Step 4: Keep translation rules

Use translation methods already used in wizard files:

- `Translate`
- `TranslateFormatted`

In `WizardPageBuilder`, use existing wrappers (`T`, `TF`) with literal keys.

For detailed translation rules and setup, see [civtranslate/README.md](../../../civtranslate/README.md).

### Step 5: Preserve UX conventions

- Keep numbering simple and sequential
- Prefer `c`/`b` hotkeys for Continue/Back
- Show current value in page lines
- Use `StatusMessage` for operation feedback

## 9) "Checkbox" and Toggle-Like Behavior

There is no dedicated checkbox control type right now.

Use a normal menu entry that toggles a boolean value.

Current example:

- menu text: "Toggle sound"
- action: `ToggleSound`
- state field: `SoundEnabled`

References:

- [WizardPageBuilder.cs](WizardPageBuilder.cs)
- [WizardActionHandler.cs](WizardActionHandler.cs)

## 10) "Open Folder" Behavior

Folder selection path:

1. Entry action `BrowseDataFolder`
2. Action handler calls `Runtime.BrowseFolder(...)` via injected delegate
3. Selected folder copied into game data via `FileSystem.CopyDataFiles(...)`
4. Validation re-check with `FileSystem.DataFilesExist()`
5. status message shown to user

Reference: [WizardActionHandler.cs](WizardActionHandler.cs)

## 11) Rendering Notes

Rendering components:

- [WizardRenderingDelegate.cs](WizardRenderingDelegate.cs): box/text/menu/link drawing
- [WizardMouseMarkerDelegate.cs](WizardMouseMarkerDelegate.cs): mouse marker style
- [WizardRenderingContext.cs](WizardRenderingContext.cs): hit areas + scale/box state

Important details:

- scaling keeps an 80x25 DOS grid
- menu and link hit areas are tracked as rectangles
- links are clickable and can open browser
- resize handling is enabled on [WizardScreen.cs](WizardScreen.cs)

## 12) Dependency Injection and Testability

The wizard uses constructor-injected interfaces for core behavior:

- page creation: `IWizardPageBuilder`
- action execution: `IWizardActionHandler`

Internal constructor in [WizardScreen.cs](WizardScreen.cs) allows injecting test doubles.

## 13) Where To Start As New Contributor

Suggested order:

1. Read [WizardScreen.cs](WizardScreen.cs)
2. Read [WizardPageBuilder.cs](WizardPageBuilder.cs)
3. Read [WizardActionHandler.cs](WizardActionHandler.cs)
4. Read [WizardEntry.cs](WizardEntry.cs) and [WizardPage.cs](WizardPage.cs)
5. Then inspect rendering delegates

If you only want to add a new setup step, you usually need changes in:

- [WizardEngine.cs](WizardEngine.cs)
- [WizardEntry.cs](WizardEntry.cs) (optional)
- [WizardPageBuilder.cs](WizardPageBuilder.cs)
- [WizardActionHandler.cs](WizardActionHandler.cs)
