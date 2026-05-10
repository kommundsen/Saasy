---
name: xunit-fact-theory-test
description: Design example-based xUnit tests for .NET code -- [Fact] for one specific case, [Theory] + [InlineData] for a small enumerated set. Use this skill when the user wants to write a unit test, add a regression test for a known bug, exercise a specific input/output pair, or asks "what should the [Fact] cover for X". Trigger on phrases like "write a test for", "test that X returns Y", "add a regression test", "cover the empty/null case", "[Fact]", "[Theory]", "[InlineData]". Do NOT use when the user wants to test a law, invariant, or "for all valid inputs" -- that's a property test, see the `conjecture-property-test` skill.
---

# xUnit Fact/Theory test design

An example-based test asserts that a *specific concrete input* produces a *specific concrete output*. The job is to pick the cases that document the contract and would catch the regressions you actually fear.

## Default: try a property test first

Before reaching for `[Fact]` or `[Theory]`, ask the property-test question for the System Under Test:

- Is there a **round-trip** (`decode(encode(x)) == x`, `parse(format(x)) == x`)?
- Is there an **oracle** -- a slow-but-obviously-correct reference?
- Are there **algebraic laws** -- commutativity, associativity, idempotence, anti-symmetry?
- Are there **invariants** the output must preserve relative to the input (length, ordering, set membership)?
- Are there **bounds** the output must satisfy (range, sortedness, subset of input)?

If *any* of those fit, route the work through the [conjecture-property-test](../conjecture-property-test/SKILL.MD) skill -- properties cover more inputs, shrink to minimal counterexamples, and document the actual contract. **Use this skill only after you've considered properties and concluded they don't apply.** Most of the SUTs that warrant a Theory with hand-picked rows are properties in disguise; the few that aren't (regressions, anchored examples, lookups, error-message-as-contract) are what this skill is for.

## Mindset: think in cases, not laws

Once you've ruled out properties, ask: **what specific input do I want to nail down, and why does that case matter?**

Good answers come from a few recurring intents. Most tests fit one of them:

1. **The happy path** -- one canonical case that reads as documentation. "Given a USD currency, `Money.Create(1.50m)` yields an amount of `1.50`."
2. **A boundary** -- the smallest valid input, the largest, exactly at a threshold. "Batch of 500 succeeds; batch of 501 returns 400."
3. **A specific failure mode** -- what does the *error path* look like for this exact bad input? Empty string, null, expired token, missing header. The error message and exception type are the assertion target.
4. **A known regression** -- a bug that bit you once. The test name encodes "this used to break; now it doesn't." A property test cannot replace this; only an anchored example is a faithful reproducer.
5. **An anchor for a property** -- a single concrete case that pins down magnitude when the rest of the suite is property-driven and only proves shape (see [conjecture-property-test](../conjecture-property-test/SKILL.MD)'s "constant 2" critique).

If none of those apply -- if you find yourself trying to enumerate "all inputs that should work" -- you wanted a property test, not a Theory.

## `[Fact]` vs `[Theory]` -- the small judgment

- **`[Fact]`** -- one case. Use when the input is genuinely unique: *the* happy path, *the* boundary, *the* error case.
- **`[Theory] + [InlineData]`** -- a *small, finite, enumerable* set of cases that share the same assertion shape. The classic shape:
  ```csharp
  [Theory]
  [InlineData("")]
  [InlineData("   ")]
  [InlineData(null)]
  public void Create_WithBlankName_Throws(string? name) { ... }
  ```
  Three rows because there are three meaningfully-distinct flavours of "blank." If the rows would balloon past ~6 -- or if the rows are picked samples from a continuous space (`-0.01`, `-1`, `-100`) -- you wanted a property test.

The decisive heuristic: **if the InlineData rows could be replaced by a `Where(...)` filter on a generator, the Theory is impersonating a property test.** Convert it.

## When NOT to write an example-based test

Skip it and write a property test instead when:

- **You're sampling a continuous space.** `[InlineData(-0.01)] [InlineData(-1)] [InlineData(-100)]` picks three negatives. `Strategy.Decimals(min: ..., max: -0.01m)` covers "any negative decimal" across many shrinkable inputs and finds the edges you didn't think to enumerate.
- **The cases share a *law*, not just a *shape*.** `[Theory]` rows that all assert the same equation (`f(a, b) == f(b, a)`) for hand-picked pairs are a property test waiting to be promoted.
- **The InlineData would grow unbounded as edge cases surface.** Round-trips, oracle comparisons, algebraic identities -- a generator finds them; a hand-curated list lags behind.
- **The same Theory exists already in a sibling property test.** Hand-picking four IANA timezones with `[InlineData]` is redundant when `Strategy.IanaZoneIds` samples the same set across more cases. Delete the Theory.

For the conversion playbook (which property family fits, how to write the strategy), see the [conjecture-property-test](../conjecture-property-test/SKILL.MD) skill.

## Test naming and structure

Saasy convention -- mirror it:

- **Method name:** `Subject_Condition_ExpectedOutcome`. Examples: `Create_WithValidArguments_ReturnsCustomer`, `MintApiKey_TwoCallsReturnDifferentPlaintexts`, `RevokeApiKey_WhenAlreadyRevoked_EmitsNoDomainEvent`. The shape is mechanical; just follow it.
- **Body:** Arrange / Act / Assert separated by blank lines, no `// arrange` comments. One assertion per test by default; multiple are fine when they describe one logical observation (e.g. an HTTP response's status code AND a header).
- **Prefer `[Fact]` over `[Theory]` with one row.** A Theory with one InlineData is a Fact with extra ceremony.
- **`Assert.Equal(expected, actual)` over `Assert.True(condition)`.** Equality assertions name the discrepancy in the failure output; truth assertions just say "false." If the predicate is genuinely boolean, lift it onto the domain type (`money.IsPositive`) so the failure reads cleanly.
- **Don't share state across tests.** xunit gives each test method a fresh class instance; rely on that. `IClassFixture<T>` is for *expensive* shared setup (a `WebApplicationFactory<Program>`, a containerised Postgres) -- not for "I don't want to call `Customer.Create(...)` twice."

## xunit.v3 idioms in this codebase

The whole repo runs xunit.v3. The handful of things that differ from v2 muscle memory:

- **`TestContext.Current.CancellationToken`** -- pass it to every cancellable async call (`HttpClient.GetAsync`, `DbContext.SaveChangesAsync`, anything taking `CancellationToken`) to satisfy the xUnit1051 analyzer and cooperate with test-runner cancellation. The `Saasy.Api.Tests` and `Saasy.Tenancy.Application.Tests` projects already do this -- match the pattern.
- **`IAsyncLifetime`** returns `ValueTask`, not `Task`. (v2 returned `Task`.) The interface name is the same; the method signatures aren't.
- **`Assert.Equal(double, double, int precision)`** is gone. Use `Assert.Equal(double expected, double actual, double tolerance)`.
- **`[Fact(Skip = "...")]`** still works. xunit.v3's conditional-skip overload (`SkipWhen` / `SkipUnless`) is preferred when the skip reason depends on environment -- `[Fact(Skip = "...", SkipUnless = nameof(IsLinux))]`.
- **`IClassFixture<T>`** -- API unchanged from v2.
- **Global `using Xunit`** is set in each test project's `.csproj` (`<Using Include="Xunit" />`), so individual `.cs` files don't need a `using Xunit;` line.
- **`xunit.runner.visualstudio`** is the v2 + v3 dual-runner; no swap needed when migrating.

## Output template

When asked to design a Fact/Theory test, produce:

1. **The intent** -- one sentence. "What specific case does this nail down, and why does that case matter?"
2. **`[Fact]` or `[Theory]`** -- and if Theory, the *small finite list* of rows with a one-line justification per row explaining why each is meaningfully distinct. If you're picking more than ~6 rows, push back: it's likely a property test.
3. **The C# code** -- `Subject_Condition_ExpectedOutcome` naming, AAA structure, single assertion intent. Use `TestContext.Current.CancellationToken` on async I/O.
4. **Cross-check** -- could this case be folded into an existing property as its anchor, or replaced by a property entirely? Mention it; the user chooses example-as-anchor, conversion, or "keep both."

Keep the response focused. The user usually wants the case selection first, code second; don't lead with a wall of code.

## Example walkthrough

User: *"Test that `Money.Create` rejects amounts with more decimal precision than the currency's minor units (e.g. `1.234m` for USD, which has 2 minor units)."*

Response shape:

> **Intent:** Pin down the precision-rejection contract for one canonical pairing -- USD's 2-minor-unit quantum vs a 3-decimal amount. Single concrete case; documents the rule in the test name; catches a regression that removes the precision check.
>
> **Shape:** `[Fact]`. Promote to `[Theory]` only if you also want to cover JPY (0 minor units, integer-only) and BHD (3 minor units) -- in that case three rows, one per currency family, is justified because each row exercises a different boundary.
>
> **Code:**
> ```csharp
> [Fact]
> public void Create_AmountWithMorePrecisionThanCurrency_Throws()
> {
>     Assert.Throws<ArgumentException>(() => Money.Create(1.234m, Usd));
> }
> ```
>
> **Cross-check:** if a property `money.Amount.Scale <= currency.MinorUnits` exists for `Money.Create`'s output, this `[Fact]` is its anchor -- keep both. If not, the example stands alone; consider whether the property is worth adding once you have two or three of these.
