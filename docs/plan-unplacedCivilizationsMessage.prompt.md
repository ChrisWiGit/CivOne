# Plan: Report civilizations that could not be placed at game start

**TL;DR**: A custom map can be so small or so full of ocean that a civilization gets no
starting position. That civilization is now destroyed during game creation, but silently.
This plan adds a popup that tells the player which civilizations were dropped — shown
**after** the game screen exists, not during game construction.

---

## Context

`PlaceStartingUnits` ([src/Game.NewGame.cs:411](../src/Game.NewGame.cs#L411)) assigns each
civilization a starting tile. On a normal generated map this always succeeds. On a
hand-made `*.comap` map it can fail: the map may have no free land tile left.

Previously such a player was left alive with zero units and zero cities — a "phantom"
civilization that never acts and never dies. That is now handled: the player is destroyed
outright during game creation.

What is still missing is **feedback**. The player starts a game against, say, 6 rivals and
silently only gets 5, with nothing but a log line explaining why.

## Current behaviour

[src/Game.NewGame.cs:142](../src/Game.NewGame.cs#L142) marks the player as destroyed and
writes the replay entry by hand:

```csharp
_players[player].HandleExtinction(invokeDestroyedEvent: false);

_replayData.Add(new ReplayData.CivilizationDestroyed(
    _gameTurn, _players[player].Civilization.PreferredPlayerNumber, 0)); // 0 = Barbarians
```

The destruction is attributed to the Barbarians because nobody actually defeated this
civilization — the map simply had no room for it. Barbarians are always player 0,
regardless of the barbarian-activity setting, so this works even when barbarians are off.

## Why the obvious approach does not work

The tempting shortcut is to let the normal destruction path do the work: call
`HandleExtinction()` with its default, let the `Destroyed` event fire, and let
`PlayerDestroyed` ([src/Game.cs:248](../src/Game.cs#L248)) show its usual
"X civilization destroyed by Y!" advisor message.

**This breaks, because `PlaceStartingUnits` runs inside the `Game` constructor.** Two
separate problems follow:

**1. The message appears over the intro screen.**
`Message.Advisor(...)` ([src/Tasks/Message.cs:53](../src/Tasks/Message.cs#L53)) builds an
`AdvisorMessage` screen *immediately* — government portrait, palette merge, font rendering
([src/Screens/Dialogs/AdvisorMessage.cs:52](../src/Screens/Dialogs/AdvisorMessage.cs#L52)).
That drags the graphics subsystem into game construction. It is then queued with
`GameTask.Insert`, and the runtime pops tasks on every tick
([src/RuntimeHandler.cs:121](../src/RuntimeHandler.cs#L121)). At that moment the new-game
intro screen is still up: `GamePlay` is not created until after the fade-out at
[src/Screens/NewGame.cs:289](../src/Screens/NewGame.cs#L289). The popup would land on top
of the intro, before the map exists.

**2. It causes re-entrant recursion.**
`PlayerDestroyed` respawns early-game civilizations and calls `PlaceStartingUnits` again —
from inside the loop that is still running. On a landless map that placement fails the
same way, so the cycle repeats until the replay log has seen the civilization die twice.

Suppressing the event with `invokeDestroyedEvent: false` removes both problems at once,
which is why the current code does that. The message therefore has to be raised somewhere
else.

## Proposed design

### 1. Collect the failures in `Game`

```csharp
private readonly List<ICivilization> _unplacedCivilizations = [];

/// <summary>
/// Civilizations that could not be placed on the map and were destroyed during game
/// creation. Read once by the new-game flow to inform the player.
/// Empty for any normal map.
/// </summary>
public IReadOnlyList<ICivilization> UnplacedCivilizations => _unplacedCivilizations;
```

`PlaceStartingUnits` adds to this list at the point where it currently only logs.

### 2. Show the message once `GamePlay` is on screen

[src/Screens/NewGame.cs:302](../src/Screens/NewGame.cs#L302) already establishes the
pattern — queue work right after `Common.AddScreen(gamePlay)`:

```csharp
if (Game.InstantAdvice)
{
    GameTask.Enqueue(Show.InterfaceHelp);
    GameTask.Enqueue(Message.Help(Translate("--- Civilization Note ---"), GetGameText("HELP/FIRSTMOVE")));
}
```

The new block goes just before it, so the problem report is shown first:

```csharp
if (Game.UnplacedCivilizations.Count > 0)
{
    string names = string.Join(", ", Game.UnplacedCivilizations.Select(c => c.NamePlural));
    GameTask.Enqueue(Message.Error(
        Translate("--- Map Problem ---"),
        TranslateFormattedArray("This map has no free land for {0}.\nRemoved from the game.", names)));
}
```

This is safe where the constructor was not: `GamePlay` is already on the screen stack, the
graphics subsystem is running anyway, and the popup lands over the map.

Use `GameTask.Enqueue` (appends to the queue), not `GameTask.Insert` (jumps to the front) —
see [src/GameTask.cs:60](../src/GameTask.cs#L60) and
[src/GameTask.cs:67](../src/GameTask.cs#L67).

### 3. Message type

`Message.Error` ([src/Tasks/Message.cs:67](../src/Tasks/Message.cs#L67)) is the right fit:
it renders a `PopupMessage` in the warning colour and plays a beep. Unlike
`Message.Advisor` it is constructed here, after the game is up, so none of the timing
problems above apply.

### 4. Translation rules

Per the project translation rules, the key must be a plain English string literal. The
civilization names are passed as the `{0}` argument, which is allowed — the restriction is
on building the *key*, not on passing values.

For singular versus plural, use `if`/`else` around two separate calls, each with its own
literal key. Do not use a conditional expression inside the translate call; the extraction
script has to see both literals.

After implementing, run `translate.sh` to refresh `translation/all.txt`, then move the new
entries into the language files such as `civ_german.txt`.

## Open decisions

**The human player.** If the *human* civilization cannot be placed, the game is
unwinnable — categorically worse than losing an AI rival.
[src/Screens/NewGame.cs:296](../src/Screens/NewGame.cs#L296) currently only half-handles
this by centering the view on the map middle when no human start unit is found. Two
options:

- a separate, more explicit message, leaving the player in a broken game; or
- abort game creation and return to the menu.

Aborting is the more honest behaviour, but it is a larger change and needs a decision.

**Alternative without new state.** Instead of the list, the new-game flow could evaluate
`Game.Players.Where(p => p != 0 && p.IsDestroyed)` directly after `CreateGame`. Immediately
after construction, every destroyed non-barbarian *is* a placement failure. This avoids new
state but is implicit, and it breaks as soon as players can be destroyed earlier for other
reasons. The explicit list is preferred.

## Files touched

| File | Change |
|------|--------|
| [src/Game.NewGame.cs](../src/Game.NewGame.cs) | Add `_unplacedCivilizations` list + `UnplacedCivilizations` property; record the failure in `PlaceStartingUnits` |
| [src/Screens/NewGame.cs](../src/Screens/NewGame.cs) | Queue the message after `Common.AddScreen(gamePlay)` |
| `translation/all.txt` | New translation keys via `translate.sh` |
