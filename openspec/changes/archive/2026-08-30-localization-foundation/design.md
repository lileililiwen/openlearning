# Design: Localization Foundation

## Decisions

- **Infrastructure**: in `Program.cs`, call `builder.Services.AddLocalization()` and configure
  `RequestLocalizationOptions` with supported cultures `en` (default) and `zh`, using
  `CookieRequestCultureProvider` + `QueryStringRequestCultureProvider` + `AcceptLanguage` fallback.
  Apply `app.UseRequestLocalization(...)` before routing.
- **Resources**: create `Resources/Pages/<Page>.resx` (English/default) and
  `Resources/Pages/<Page>.zh.resx` (Chinese) keyed by stable string names (not the full sentence where
  reusable). For shared UI chrome, use a `SharedResources` class + `SharedResources.resx` pair.
- **Injection**: inject `IStringLocalizer<ThePageModel>` into each migrated page and replace hardcoded
  text with `@Localizer["Key"]`. Keep markup/structure server-rendered.
- **Migration priority** (so the learner journey is consistent first): `MyCourses.cshtml:18` (mixed
  warning), `Courses/Lessons/View.cshtml:84` (empty-state Chinese), `Courses/Details.cshtml`,
  `Dashboard/Index.cshtml` + `Dashboard/Teacher.cshtml`, `Courses/Edit.cshtml:14` ("成果与掌握度"),
  then auth/account pages. The remaining pages can be migrated incrementally; the goal of this change
  is the *foundation* + the highest-traffic learner pages being language-consistent.
- **lang attribute**: combine with `learner-accessibility-compliance` — `_Layout.cshtml` reads the
  active culture to set `lang`. This change seeds the culture pipeline; the a11y change consumes it.

## Failure handling

- Missing translation falls back to the default (English) resource automatically (ASP.NET Core default behavior) — never throws.
- If a `Localizer["Key"]` is absent in both resources, the key itself is returned (visible during dev, so gaps are easy to spot).

## Verification

- Test: requesting `?culture=zh` renders the Chinese resource string; default renders English; no page mixes both on a single rendered view.
- Test: `IStringLocalizer` returns English for an untranslated key.
- `dotnet format` + build zero warnings; OpenSpec validation passes.
