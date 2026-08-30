# TASK.md — Current Work & Status

> Companion to `HANDOFF.md`. Tracks the in-flight / recently completed spec and the next
> item in the pipeline. **Last updated: 2026-08-30.**

## Just completed

- **`localization-foundation`** — COMPLETE & ARCHIVED
  (`openspec/changes/archive/2026-08-30-localization-foundation`).
  - ASP.NET Core localization foundation: `AddLocalizationFoundation()` in `Program.cs`,
    `en` default + `zh` supported, cookie/query/accept-language providers, `UseRequestLocalization`.
  - `SharedResources` marker + `SharedResources.zh.resx` for chrome; `<html lang>` derived
    from `CurrentUICulture`.
  - Migrated mixed-language priority pages: `MyCourses`, `Courses/Lessons/View`,
    `Courses/Details`, `Courses/Edit`, and the global `_Layout` chrome.
  - Resources are named after the page **model class** (`MyCoursesModel.zh.resx`, etc.) and
    embedded as the `zh` satellite assembly.
  - 7 localization unit tests green; full suite 481 passed; `dotnet format` clean.

## Next spec (per HANDOFF suggested next steps)

- **`learner-accessibility-compliance`** — not started.
  - Consumes the `lang` hook and localization foundation delivered above.
  - See `openspec/changes/learner-accessibility-compliance/`.

## Backlog (active specs, in suggested order)

- `learner-experience-quality`
- `lesson-sequencing-navigation`
- `offline-sync-resilience`
- `platform-quality-release-gates`
- `responsive-design-system`

## Standing gates (must stay green)

- `dotnet build OpenLearning.sln` → 0 warnings / 0 errors (`TreatWarningsAsErrors=true`,
  Sonar, `EnforceCodeStyleInBuild=true`).
- `dotnet test tests/OpenLearning.UnitTests` → all green.
- `dotnet format --verify-no-changes` → clean for the whole solution.
