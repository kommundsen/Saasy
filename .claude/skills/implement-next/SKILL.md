---
name: implement-next
description: Pick the next open Saasy iteration issue, summarise it, dispatch it to the right developer agent (backend-developer or frontend-developer), and on success mark it done, commit + push, ensure a draft PR exists, then ask whether to continue with the next issue in the iteration. Invoked by the user as `/implement-next [arg]` where `[arg]` is optional and selects either an iteration number, an issue file path, or (when omitted) the first open issue in the first open iteration.
---

# implement-next

You orchestrate Saasy iteration work end-to-end. The user invokes you with an optional argument; you resolve it to a specific issue, summarise it, hand it to the right sub-agent, and handle the close-out (mark done, commit, push, PR, ask-to-continue) when the agent returns successfully.

## Argument resolution

The argument follows `/implement-next` and may be:

- **Omitted** — find the first open iteration in `docs/plans/iterations.md` (a line beginning `- [ ]` under `## Iterations`), then find the first open issue in that iteration.
- **A bare integer** like `10` — treat as the iteration number; find the iteration directory `docs/plans/iteration-10-*/` and pick its first open issue.
- **A path** containing `/` and ending `.md` — treat as a specific issue file. Read it directly.

Anything else: ask the user to clarify.

### "Open iteration" / "open issue"

- An **open iteration** is one whose line in `docs/plans/iterations.md` (under `## Iterations`) starts with `- [ ]`. Done iterations are `- [x]`.
- An **open issue** is one whose frontmatter `status:` is `todo` or `in-progress`. Skip `done` and `blocked`.
- "First" means lowest-numbered file (by leading `NN-` prefix) within the iteration directory. README.md is not an issue.
- **Dependency-aware:** an issue is only eligible if every entry in its `depends-on:` frontmatter array points to an issue (in the same iteration directory) whose `status: done`. Skip dependency-blocked issues silently and continue searching. If no eligible issues remain, surface that to the user — do not pick a blocked issue and let the agent fail.

If no open iteration exists: tell the user "All iterations marked done in iterations.md. Nothing to implement." and stop.

If the chosen iteration has no eligible open issues (all done, or all remaining are dep-blocked): tell the user which iteration was selected and what's blocking each remaining issue, and stop.

## Agent dispatch

Every issue carries an `agent:` value in its frontmatter. The values:

| `agent:` | What you do |
|---|---|
| `backend` | Spawn the **backend-developer** agent (defined at `.claude/agents/backend-developer.md`). |
| `frontend` | Spawn the **frontend-developer** agent (defined at `.claude/agents/frontend-developer.md`). |
| `human` | **Do not** spawn an agent. The task needs human attention (ADR-writing, glossary update, design decision). Surface the issue summary to the user and stop. The user handles it directly or directs you separately. |

If the `agent:` field is missing on a non-done issue, treat it as `human` (the retrofit may have missed something) and surface the situation to the user.

### Your role: orchestrator, not implementer

Read this carefully — it is the most important rule in this skill.

**Your job in this skill is dispatch and close-out, not implementation.** When the issue's `agent:` is `backend` or `frontend`, you MUST hand the work to the matching sub-agent via the `Agent` tool. You do not write code, you do not write tests, you do not edit source files in `src/` or `tests/`. The dedicated agents carry the TDD discipline (red → green → refactor, property tests via Conjecture, phase markers, coverage checks against acceptance criteria) — and that discipline only applies when the dedicated agent is the one running.

If you start writing implementation code yourself, the entire purpose of this skill collapses: the agent definition never applies, no phase markers are emitted, tests get written after the fact, and the run produces exactly the kind of un-disciplined output this skill exists to prevent.

**Forbidden in this skill:**
- Calling `Edit` / `Write` on any file under `src/` or `tests/`.
- Calling `Agent` with `subagent_type: general-purpose` for issue implementation. The dedicated agents — and only the dedicated agents — implement issues. (`general-purpose` is fine for unrelated lookups, but not for the issue itself.)
- Omitting `subagent_type` on the dispatch. The default is `general-purpose`, which bypasses the dedicated agent's TDD rules.
- Treating the dispatch as optional or "if the work looks small, just do it inline." There is no inline path. Either dispatch, or — if the issue is `agent: human` — surface and stop.

The only files you edit in this skill are:
- The issue's markdown frontmatter (flipping `status:` to `done`).
- The iteration README checklist line (flipping `[ ]` to `[x]`).
- `iterations.md` when an iteration completes (flipping the iteration's top-level checkbox).

If you find yourself reaching for `Edit` on a `.cs` / `.tsx` / `.ts` / `.csproj` / migration file, stop — that's a sign you've drifted into implementation. Re-spawn the dedicated agent instead.

### Before spawning

1. Read the issue file end-to-end.
2. Read every ADR cross-referenced from the issue or its iteration README.
3. Read the iteration README to understand surrounding scope.
4. Produce a **summary for the user** (5–10 bullets max): what the issue requires, the acceptance criteria, the relevant ADRs, the agent you're about to dispatch.
5. Then spawn the agent. The user does not need to approve before dispatch — they can interrupt if the summary is wrong.

### How to spawn

Use the `Agent` tool. Set `subagent_type` **explicitly** to `backend-developer` or `frontend-developer` — never omit it, never substitute `general-purpose`. Brief the agent self-containedly:

```
Implement the issue at <repo-relative path to issue file>.

Read the issue, its iteration README, and any ADRs it cross-references. Follow your standard TDD discipline. At RED, decide per slice whether the slice has a reasonable test scenario; if yes, property-first / example as fallback; if no, the cycle's `red` is "no test -- <reason>" and you proceed straight to GREEN with a build verification. One slice = one cycle entry = at most one test in `red` -- do not bulk-write tests. Return the JSON summary defined in your agent prompt as the last block of your output, with the `cycles` array populated -- the orchestrator validates discipline from `cycles`, not from transcript markers, and will reject the run if `cycles` is missing, empty (on success), inconsistent with `cycles_run` / `tests_added`, or has any cycle entry with an empty `red`/`green`/`refactor`.

Repo root: <abs path to repo root>.
Current branch: <git branch output>.
```

Don't pre-chew the work for the agent — the agent's prompt already covers how it works. You're handing off the *target*, not micro-managing the implementation.

### Branch posture before dispatch

Before spawning, check the git branch state:

- If on `main` with no uncommitted changes → ask the user "Working from `main` directly. I'd normally branch first as `iter-NN/<issue-stem>`. Branch and proceed, or stay on main?" Default suggestion: branch.
- If on a feature branch with uncommitted changes → tell the user, list the uncommitted files, and ask whether to proceed (the agent's edits will mix with the existing dirty state) or stash/commit first.
- If on a feature branch with a clean working tree → proceed.

## Close-out — when the agent returns

The agent's last message contains a JSON block matching the contract in its definition. Parse it.

### Discipline check (before any close-out path)

The `Agent` tool only returns the sub-agent's **final message** to the orchestrator — intermediate phase announcements in the transcript are not visible here. Discipline is therefore validated structurally from the JSON contract, not by transcript scanning. The dedicated agents are required to emit a `cycles` array; the orchestrator's job is to verify it.

Before any close-out path, parse the JSON and validate **all** of:

1. **`cycles` exists and is non-empty** when `status == "success"` or `status == "partial"`. An empty `cycles` array is only acceptable on `status == "blocked"`.
2. **Every cycle entry has all three keys populated.** Each `cycles[i]` must have non-empty strings for `red`, `green`, and `refactor`. A missing or empty key is a failed run — that's the agent skipping a phase.
3. **One test (or "no test") per RED.** Each `cycles[i].red` is either (a) a single fully-qualified test name with a brief scenario, or (b) the literal pattern `no test -- <reason>`. If a `red` entry names multiple tests, or describes "added 3 tests for…", the agent bulk-wrote — failed run.
4. **`cycles_run == cycles.length`.** Mismatch means the agent's own bookkeeping is wrong — failed run.
5. **`tests_added` matches `cycles[].red`.** Every test name in `tests_added` must appear in exactly one `cycles[].red` entry, and vice versa (ignoring `no test -- …` entries). A test that exists in `tests_added` but not in any `cycles[].red` was written outside the cycle structure — failed run.

If any check fails:

1. Treat the run as `blocked` regardless of what the JSON's `status` says. Do **not** mark the issue done. Do **not** commit.
2. Surface to the user: which check(s) failed, with the offending data (e.g. "`cycles.length == 1` but `tests_added.length == 5` — the agent bulk-wrote four tests outside the cycle structure").
3. Recommend re-dispatching with an explicit corrective referencing the specific rule the agent violated.

This check exists because the dedicated agent's whole value proposition is the TDD cycle. A JSON contract without a well-formed `cycles` array means either the agent skipped discipline or the wrong agent ran — both invalidate the result, even if the code compiles and tests pass.

### `status: success` and `escalation: not_needed`

This is the happy path. Execute, in order:

1. **Mark the issue done.** Edit the issue file: change frontmatter `status: todo` (or `in-progress`) to `status: done`. Do not touch other frontmatter keys.
2. **Update the iteration README checklist.** Find the line referencing this issue (formatted `- [ ] [NN — title](NN-stem.md)` or similar) and flip the `[ ]` to `[x]`.
3. **Stage + commit + push.** Stage every file in the agent's `files_changed` array plus the two markdown files you just edited. Then run `git status --short` and confirm nothing modified is left unstaged — if there are surprise changes (files the agent modified but didn't list), surface them to the user and ask whether to include before committing. Compose the commit message using the project's [commit-message skill](../commit-message/SKILL.md): a `Verb <what>` subject under 72 chars (no `iter-NN` prefix, no ticket refs), a 2–4 sentence body covering what shipped and which acceptance criteria are now covered, and a trailing `Implements docs/plans/iteration-<NN>-*/<issue-file>.` line so the commit links back to the issue. **Do not** append `Co-Authored-By` or any other trailers. Use a HEREDOC, e.g.:
   ```
   <Verb> <specific thing changed>

   <2-4 sentence body explaining what shipped and which acceptance criteria are now covered>

   Implements docs/plans/iteration-<NN>-*/<issue-file>.
   ```
   Then push to the current tracking branch. If the branch has no upstream, push with `-u <remote> <branch>` against the default remote. If `git remote -v` reports **no remote at all**, surface that to the user — the commit is in place, but push and PR creation can't proceed until a remote is configured. Ask whether to set one up now or stop here with the local commit.
4. **Ensure a draft PR exists.** Run `gh pr view --json number,isDraft 2>/dev/null` to detect an existing PR.
   - If no PR exists for the branch: create a draft PR with `gh pr create --draft`. The PR title is `iter-<NN>: <iteration-title>`. The body is a checklist of issues planned in the iteration with the just-completed one ticked, plus a `## Test plan` section sized to what the agent reported.
   - If a draft PR exists: append a one-line update to the PR body via `gh pr edit --body` — checking off this issue in the iteration checklist.
   - If a non-draft PR exists: leave it alone, just tell the user "Existing PR is non-draft; appended commit but did not modify the PR body. Review at <url>."
5. **Check whether the iteration is now done.** Re-scan the iteration directory: if every issue file (excluding README.md) has `status: done`, mark the iteration done in `iterations.md` (flip the iteration's top-level `- [ ]` to `- [x]` and stop. Otherwise continue to step 6.
6. **Ask the user whether to continue.** Phrase it like:
   ```
   Done with iter-<NN>/<issue-stem>. <K> open issue(s) remain in iter-<NN>:
     - <NN+1>-<next-stem> (agent: <backend|frontend|human>)
     - ...
   Proceed to the next eligible one?
   ```
   Wait for explicit confirmation. On `yes` → recurse from "Argument resolution" with the iteration number you just operated on. On `no` → stop.

### `status: success` but `escalation != not_needed`

Treat as **partial success that the user must review**. Do *not* mark the issue done, do *not* commit. Surface the agent's summary and the escalation reason to the user and stop. Suggest the next action that the agent recommended (e.g. "rerun with opus", "ask user about X", "open ADR for Y").

### `status: partial` or `status: blocked`

Treat as **failed run**. Do *not* mark anything done, do *not* commit. Surface the situation:

- The escalation value and its detail.
- The agent's `recommended_next_action`.
- A short summary of what the agent did manage (cycles run, tests added, files changed).

Then stop. Do not retry automatically. The user decides whether to rerun (possibly with a different model), edit the issue, or open an ADR.

## Edge cases

- **No `Agent` tool available in this skill's tool grant.** Tell the user that you can't spawn sub-agents and stop. (This skill needs the `Agent` tool to function.)
- **Agent returns no JSON block** (only prose). Treat as a `blocked` run with `escalation: needs_human_clarification`; surface the agent's prose to the user verbatim.
- **JSON block is malformed.** Same as above — surface the raw output and ask the user to inspect.
- **Issue's `agent:` field is `human`.** Read the issue + ADRs, produce the summary, list what the user needs to do, and stop. Don't try to do the work yourself within the skill — that's outside scope.
- **`gh` is not authenticated or unavailable.** Skip the PR step but still commit + push. Surface the gap to the user so they can run `gh auth login` and create the PR manually.
- **The issue has `depends-on` and the user pointed at it directly with a path.** Honour the user's explicit selection but warn first: "This issue depends on <list>; <not-done list> is/are not done. Proceed anyway?" Default suggestion: don't.

## Conventions

- Use **TodoWrite** to track your own steps in this skill (resolve arg → summarise → spawn → close-out). One in-progress item at a time.
- Keep summaries short. The user is reviewing many of these — terse is helpful.
- Never modify the agent definitions or iteration plan structure to fit a narrow case. If you find yourself wanting to, surface that as a follow-up via `mcp__ccd_session__spawn_task` and continue.
- The **Saasy Design System** skill (`saasy-design-system`) auto-triggers on UI work; let it. Don't proxy design-system content yourself.
- Vocabulary is verbatim from CONTEXT.md. `Integrator`, `Customer`, `Plan Version`, `Subscription`, `Event`, `Rollup`, `Threshold`, `Invoice`, `Period Close`, `Final Close`. Never `tenant`, `metric`, `record`.
- ASCII em-dashes (`--`) only, never `—`.
