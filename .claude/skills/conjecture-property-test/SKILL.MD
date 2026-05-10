---
name: conjecture-property-test
description: Design property-based tests for .NET code using Conjecture. Use this skill whenever the user wants to write a property test, convert an example-based test to a property test, evaluate whether a property test is good, or asks "what properties should I test for X". Also use when the user mentions Conjecture, Hypothesis-style testing, generative testing, fuzzing-as-tests, or property-based testing in C#/F#. Trigger even if they don't say "property test" explicitly — phrases like "test that X holds for all inputs", "find edge cases in", or "test invariants of" are signals. Do NOT use for simple example-based unit tests where one or two concrete cases suffice.
---

# Conjecture property-test design

A property test asserts a *law* that must hold across many generated inputs, not a single hand-picked input/output pair. The job is to find the laws — Conjecture handles the generation and shrinking.

## Mindset: think in laws, not examples

Before writing any code, ask: **what must always be true about the output, given any valid input?**

Good answers come from a few recurring families. Walk through them in order — most functions satisfy two or three:

1. **Round-trip (inverse)** — `decode(encode(x)) == x`, `parse(format(x)) == x`, `Add(Diff(a,b), a) == b`. This is the strongest property you can usually find: it pins down the actual value, not just shape.
2. **Oracle** — compare against a trivially-correct-but-slow reference implementation (naive O(n²) sort, brute-force search, an existing library).
3. **Algebraic laws** — commutativity, associativity, distributivity, identity, idempotence (`f(f(x)) == f(x)`), anti-symmetry (`f(a,b) == -f(b,a)`).
4. **Invariants / metamorphic** — properties relating outputs of related inputs. `sort(xs).Length == xs.Length`, `sort(xs ++ [y]).Contains(y)`, `Reverse(Reverse(xs)) == xs`.
5. **Bounds / postconditions** — `0 <= result <= 1`, `result.IsSubsetOf(input)`, `result is sorted`.

## The "constant 2" test (necessary vs sufficient)

The single most important critique to apply to your own properties:

> **Could a wrong implementation pass all of these?**

Try mentally substituting:
- The constant zero
- A constant non-zero value
- The identity function
- A nearly-correct version (off-by-one, wrong sign, swapped args)

If any survive, your property set is incomplete. **Shape properties** (anti-symmetry, additivity, idempotence) constrain *relationships* but not *magnitude*. Always pair them with at least one **anchoring property** — typically a round-trip or oracle — that ties output to a concrete expected value.

Example: for `HoursBetween(a, b)`:
- Anti-symmetry `Hours(a,b) == -Hours(b,a)` — passed by `≡ 0`. Shape only.
- Additivity `Hours(a,c) == Hours(a,b)+Hours(b,c)` — passed by `≡ 0` and by `b.Hour - a.Hour`. Shape only.
- Round-trip `Hours(a, a.AddHours(n)) == n` — anchors the magnitude. ✅

Rule of thumb: **if you can write a wrong impl that passes all your properties, add another property.**

## When NOT to write a property test

Property tests are not free. Skip them and write an example-based test when:

- **The function is essentially a lookup or mapping.** `GetCountryName("NO") == "Norway"` has no general law — it's just a fact. Properties would be circular.
- **Behavior is fundamentally about specific cases.** Error messages, specific log output, UI strings.
- **You can't articulate any law.** If after honest effort no property comes to mind, the test would just re-implement the function in the assertion. That's worse than an example test.
- **The cost of a generator exceeds the value.** One-off code, throwaway scripts, deeply stateful integration paths where a realistic generator would be a research project.
- **The bug is already known and concrete.** Add a regression example test. Property tests *find* unknown bugs; they're not a replacement for reproducers.

A reasonable heuristic: if you find yourself writing `if (input == ...) return ...;` inside the property, you wanted an example test.

For the case-selection playbook (when `[Fact]` vs `[Theory]`, naming, xunit.v3 idioms in this codebase), see the [xunit-fact-theory-test](../xunit-fact-theory-test/SKILL.md) skill. The two skills are paired -- almost every test in this repo is one or the other, and the boundary between them is the "is this a sample of a continuous space, or a specific anchored case?" question.

## Generator design

Properties are only as good as the inputs they see. Considerations:

- **Use the strategies from Conjecture's standard packages first** (`Time`, numeric, collections, etc.) before rolling your own.
- **Constrain to valid inputs** — if the function has a precondition, encode it in the strategy, don't `Assume` it away (which wastes generation budget).
- **Beware overflow and edge cases the generator should reach naturally**: `int.MinValue`, empty collections, single-element collections, unicode boundaries, DST transitions for `DateTime`, leap seconds, NaN/Infinity for floats.
- **For `DateTime` specifically** — `AddHours` and friends overflow near `DateTime.MinValue`/`MaxValue`. Bound your range explicitly.

## Consult the Conjecture MCP

This is the important one. **Always consult the Conjecture MCP** (if available in the current session) before finalizing a property test, for:

- The current names and signatures of strategies in `Conjecture.Time`, `Conjecture.Numeric`, etc. — the API is evolving and your training data is likely stale.
- The correct attribute syntax (`[Property]`, `[Arbitrary(...)]`) and current parameter conventions.
- Whether a strategy already exists for the type you're about to write a custom one for.
- Examples of idiomatic Conjecture tests in the codebase to match style.

If no Conjecture MCP is connected in this session but the user is working on Conjecture, mention it — they may want to enable it. Search for it via `search_mcp_registry` with keywords like `["conjecture", "property testing"]` if unsure whether it's available.

If the MCP genuinely isn't available, fall back to general Conjecture conventions:
- Public API: `Generate()` (not `Draw()` — that's internal engine).
- Plain `camelCase` private fields, no underscore prefix.
- XML `///` doc comments on public surface.

## Output template

When asked to design a property test, produce:

1. **A short list of candidate properties** grouped by family (round-trip, oracle, algebraic, invariant, bounds).
2. **The "constant 2" critique** — for each property, note what wrong impl it would catch and what wrong impl it would *miss*. Identify which property is the anchor.
3. **The C# code**, using Conjecture attributes and standard strategies. Keep it minimal — one assertion per property, descriptive names.
4. **Generator notes** — any bounding or edge cases the user should think about for their domain.

Keep the response focused. The user usually wants the properties first, code second; don't lead with a wall of code.

## Example walkthrough

User: *"Property test for `HoursBetween(DateTime a, DateTime b)`."*

Response shape:

> **Candidate properties**
> - Round-trip (anchor): `Hours(a, a.AddHours(n)) == n`
> - Anti-symmetry (shape): `Hours(a,b) == -Hours(b,a)`
> - Additivity (shape): `Hours(a,c) == Hours(a,b) + Hours(b,c)`
> - Bound (sanity): `|Hours(a,b)| <= TotalHoursInRange`
>
> **Critique:** anti-symmetry alone is passed by `≡ 0`. Additivity is passed by any linear function of the components. The round-trip kills both — it's the anchor. Keep all three: the shape laws make failures easier to localize, but round-trip is the one that would actually catch a wrong impl.
>
> **Code:** [...]
>
> **Generators:** bound `DateTime` to a sane range (e.g. 2000–2100) so `AddHours(n)` doesn't overflow. Watch for DST if the function is timezone-aware.