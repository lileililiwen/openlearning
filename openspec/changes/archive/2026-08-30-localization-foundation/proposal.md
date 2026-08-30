# Proposal: Localization Foundation

## Problem

The UI is **bilingually inconsistent**: 59 of 223 pages contain Chinese strings, often mixed within a
single English page (e.g. `MyCourses.cshtml:18` shows a Chinese warning above English cards;
`Courses/Edit.cshtml:14` shows "成果与掌握度"; lesson empty-state `View.cshtml:84` is Chinese). There is
**no localization infrastructure** — no `IStringLocalizer`, no `.resx`. This reads as unfinished to an
English-speaking learner and blocks non-Chinese adoption. It also blocks the accessibility fix that
needs a correct `lang` attribute.

Canvas, Moodle, and Chamilo are internationalized from the start; even single-language products keep
user strings externalized so they can be translated without code changes.

## Change

Establish an ASP.NET Core localization foundation and migrate the learner-facing Chinese/English literals
into resources (English default + Chinese), so no single page mixes languages and `<html lang>` can be set correctly.

## Non-goals

- Translating the entire product into many languages (only en + zh resources are seeded).
- Changing the architecture/enrollment/course models.

## Impact

- New: `Resources/` folder with `.resx` (default/en + `zh`), `Program.cs` `AddLocalization` +
  `RequestLocalizationOptions` (cookie/query/accept-language providers), `IStringLocalizer<T>` injected into pages.
- Migrated (priority): `MyCourses.cshtml`, `Courses/Details.cshtml`, `Courses/Lessons/View.cshtml`,
  `Dashboard/*`, `Courses/Edit.cshtml`, and auth/account flows.
- Relates to `learner-accessibility-compliance` (provides the culture → `lang` hook) and the
  `localization-foundation`-adjacent work in `learner-experience-quality`.
- ADDED Requirements only.
