---
description: 'Describe what this custom agent does and when to use it.'
applyTo: "xunit/src/**/*Tests.cs"
---

# Copilot Instructions – CivOne Unit Tests (C#)

## Purpose

Guide AI agents (Copilot) to generate consistent, high-quality unit tests for the CivOne project.

---

## Core Rules

* Follow **Arrange–Act–Assert (AAA)** strictly
* Test **exactly one behavior per test**
* Keep tests **small, deterministic, and isolated**
* Use **mocks and DTOs only**

## CivOne Test Constraints

- NEVER instantiate `Game`, `Reflect`, or `Map` using `new` or any other mechanism.
- These classes produce heavy side effects and are not safe for isolated tests.

- ONLY use these classes in test classes that inherit from `TestsBase` or `TestsBase2`.
- This is a strict requirement with no exceptions.

- In all other contexts:
  - DO NOT reference, instantiate, or interact with these classes.

- If a task requires using these classes outside of `TestsBase` or `TestsBase2`:
  - DO NOT generate code
  - STOP immediately
  - Explain that this violates the test constraints

- Some interfaces expose properties that reference real game classes (e.g. `IPlayer.PalaceData`).

- These properties MUST always be set to `null` in tests.

- NEVER instantiate or assign real game objects to these properties.

- When setting such properties to `null`, you MUST explicitly document this with a comment explaining why.

- Example:

  var player = new Mock<IPlayer>();
  player.Setup(p => p.PalaceData).Returns((PalaceData?)null); // Must be null: real game class would cause side effects
---

## Naming Conventions

* System under test: `_testee`
* Mocked interfaces: `_mockedIInterfaceName`
* Other mocks: `_mockedSomething`
* Use `expected` and `actual` variables clearly

---

## Test Structure

* Use `[Fact]` for single scenarios
* Use `[Theory]` + `[InlineData]` for multiple inputs
* Use constructor to initialize:

  * `_testee`
  * shared test data

---

## Data Management

* Reuse shared values via `readonly` fields
* Keep datasets minimal (e.g. 2–3 entities)
* For larger setups:

  * Initialize in constructor OR
  * Use helper methods (place at end of class)

---

## DTO & YAML Strategy (Preferred)

Use serialization roundtrip testing:

1. Create DTO
2. Serialize with `YamlWriter`
3. Deserialize with `YamlReader`
4. Compare original vs restored

### Rules

* Prefer DTO creation over YAML authoring
* Do NOT load YAML files as test input
* Be aware of custom converters (e.g. 2D arrays)

### Optional (Debugging)

* Save YAML to file
* Filename should include test method name

---

## Assertions & Safety

* Always check `actual` for null before usage
* For arrays:

  * Assert `Length`
  * Assert first and last elements are not null

---

## Mocking

* Reuse mocks from `CivOne.UnitTests` if available
* For new mocks:

  * Throw `NotImplementedException` for unused members

---

## Code Style

* Minimize comments

  * Only explain non-obvious behavior
* Use modern C# syntax:

  * Arrays: `[1, 2, 3]` instead of `new int[] { 1, 2, 3 }`

---

## Constraints

* No hidden dependencies between tests
* No shared mutable state
* No external I/O (except optional YAML debug output)
* Tests must be reproducible

---

## When Tests Become Complex

* Split into multiple tests OR
* Extract helper methods (place at end of class)

---

## Expected Output Quality

Copilot should generate:

* Clean AAA structure
* Proper naming conventions
* Minimal but sufficient test data
* Clear assertions
* Maintainable and readable tests

---

## Failure Handling Rule

If requirements conflict (e.g. real game class required):
→ Do NOT generate incorrect test
→ Instead explain why it cannot be done

---
