# Startup Wizard Full Implementation Plan

## Goal
Build a DOS-style startup wizard for CivOne SDL that runs before normal startup when data files are missing or setup is forced.

## Status Snapshot
- Done: CP437 bitmap font renderer based on `ModernDos8x16`.
- Done file: `dosfont/ModernDosFontRenderer.cs`.
- Next: Wizard screen flow, page engine, and startup integration.

## Scope
- In scope:
  - DOS-style multi-page wizard UI.
  - Language selection.
  - Data folder selection and copy.
  - Sound toggle.
  - Keyboard and mouse interaction with static numbered menu entries.
  - Startup hook in runtime screen sequence.
- Out of scope (Phase 1):
  - Full macOS flow.
  - Refactoring existing screen system outside startup path.
  - Replacing normal in-game text rendering.

## Key Decisions
- Use `ModernDos8x16` for early startup text rendering when `FONTS.CV` is unavailable.
- Keep wizard implementation isolated under a dedicated startup wizard namespace.
- Reuse existing infrastructure:
  - `FileSystem.DataFilesExist()`
  - `FileSystem.CopyDataFiles(...)`
  - `Runtime.BrowseFolder(...)`
  - `TranslationServiceFactory` language APIs
- Draw frame geometry directly for stable DOS double borders.

## Target Architecture
- Renderer layer:
  - Draw DOS frame and text onto `Bytemap`.
  - Use CP437 glyph renderer with optional scaling.
- Engine layer:
  - Hold wizard state (selected language, selected data path, sound option, page index).
  - Handle input and navigation.
  - Trigger actions (copy files, apply language, finish).
- Page definition layer:
  - Declarative page models (title, prompt lines, entries).
  - Entry types: `Action`, `NavigateNext`, `NavigateBack`, `FolderPicker`, `Toggle`, `Finish`.

## Planned File Layout
- New:
  - `src/Screens/StartupWizard/WizardScreen.cs`
  - `src/Screens/StartupWizard/WizardEngine.cs`
  - `src/Screens/StartupWizard/WizardRenderer.cs`
  - `src/Screens/StartupWizard/WizardPage.cs`
  - `src/Screens/StartupWizard/WizardEntry.cs`
  - `src/Screens/StartupWizard/Pages/LanguagePage.cs`
  - `src/Screens/StartupWizard/Pages/DataFolderPage.cs`
  - `src/Screens/StartupWizard/Pages/SoundPage.cs`
- Modified:
  - `src/RuntimeHandler.cs` (startup hook)

## Page Design

### Page 1: Language
- Show available languages from translation storage.
- Numbered list entries.
- Selecting an entry applies language immediately.
- Continue entry goes to next page.

### Page 2: Data Folder
- Prompt for data folder selection.
- `BrowseFolder` action opens native picker.
- On success, call `FileSystem.CopyDataFiles(path)`.
- Validate with `FileSystem.DataFilesExist()`.
- Continue enabled only when validation passes.

### Page 3: Sound
- Toggle option for sound enable/disable.
- Save into settings model used by startup path.
- Finish exits wizard and returns to normal startup sequence.

## Visual Rules
- DOS-style frame with double-line border.
- Header block on top and numbered options below.
- No cursor list navigation in Phase 1.
- Supported input:
  - Numeric keys for entry activation.
  - Mouse click on numbered row.
  - Escape for cancel/back where valid.

## Integration Rules
- Show wizard when:
  - `!FileSystem.DataFilesExist()` OR setup is explicitly forced.
- Skip wizard on macOS in Phase 1.
- Keep existing startup screens intact unless wizard condition matches.

## Step-by-Step Implementation

## Phase 0 (Completed)
- Add CP437 font renderer:
  - `dosfont/ModernDosFontRenderer.cs`
- Implement APIs:
  - `DrawGlyph(byte cp437, ...)`
  - `DrawChar(char ch, ...)`
  - `DrawString(string text, ...)`
  - `GlyphWidth(scale)`, `GlyphHeight(scale)`, `StringWidth(text, scale)`

## Phase 1 (Core Wizard Infrastructure)
- Create page and entry models.
- Build engine state machine.
- Implement renderer for frame and text composition.
- Add deterministic hit testing for numbered rows.

## Phase 2 (Functional Pages)
- Implement language page.
- Implement data folder page and copy/validation loop.
- Implement sound page and finish action.

## Phase 3 (Runtime Hook)
- Wire wizard into `RuntimeHandler.StartupScreens`.
- Respect platform guard for macOS.
- Ensure normal flow remains unchanged when wizard is not required.

## Phase 4 (Verification)
- Build runtime SDL project.
- Run focused unit tests where logic is testable.
- Manual startup validation scenarios:
  - Missing data files path.
  - Valid data files path.
  - Forced setup path.
  - Language switch persistence.
  - Data copy success and failure.
  - Sound toggle persistence.

## Test Matrix
- Scenario: no data files, wizard appears.
- Scenario: user picks invalid folder, wizard stays on data page.
- Scenario: user picks valid folder, copy succeeds, continue enabled.
- Scenario: language switched before data copy, strings update.
- Scenario: finish completes and startup proceeds.
- Scenario: startup with existing valid data, wizard does not appear.

## Risks and Mitigations
- Risk: rendering mismatch on resize.
  - Mitigation: mark screen as resizeable and trigger redraw rebuild.
- Risk: partial copy failures.
  - Mitigation: clear error feedback and strict post-copy validation.
- Risk: character mapping gaps.
  - Mitigation: fallback to `?` glyph for unsupported characters.
- Risk: startup regression.
  - Mitigation: keep minimal runtime hook and preserve existing screen order.

## Definition of Done
- Wizard displays on required startup conditions.
- All three pages function end-to-end.
- Data copy and validation are reliable.
- Language apply behavior works during wizard.
- Sound option is persisted.
- Runtime starts normally after wizard completion.
- No regression in normal startup path when wizard is bypassed.

## Future Enhancements (Optional)
- Add additional setup pages (video mode, input profile).
- Add richer focus/cursor navigation.
- Add localization coverage checks for wizard-specific strings.
- Add optional preview panel for selected language and settings.
