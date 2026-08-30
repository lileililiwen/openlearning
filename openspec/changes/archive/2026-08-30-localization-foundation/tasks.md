# Implementation Tasks

> Status: COMPLETE & ARCHIVED (2026-08-30). Implementation notes at the bottom.

## 1. Localization infrastructure

- [x] In `Program.cs`: `AddLocalization`, configure `RequestLocalizationOptions` (cultures `en` default + `zh`; cookie/query/accept-language providers), `UseRequestLocalization`.
- [x] Add a `SharedResources` class + `SharedResources.resx` / `.zh.resx` for chrome strings.

## 2. Migrate priority pages

- [x] `MyCourses.cshtml:18` (mixed warning) → resource strings.
- [x] `Courses/Lessons/View.cshtml:84` (Chinese empty state) → resource.
- [x] `Courses/Details.cshtml`, `Courses/Edit.cshtml:14` ("成果与掌握度") → resource strings (en + zh).
- [ ] Auth/account pages follow. — deferred (out of priority scope; foundation is in place for them).

> `Dashboard/Index.cshtml` and `Dashboard/Teacher.cshtml` were audited and are **already
> English-only** (no mixed-language literals), so they were intentionally left unchanged.

## 3. Inject and replace

- [x] Inject `IStringLocalizer<T>` into migrated page models; replace hardcoded text with `@Localizer["Key"]`.

## 4. lang hook

- [x] Ensure `_Layout.cshtml` `lang` derives from the active culture (consumed by `learner-accessibility-compliance`).

## 5. Verification

- [x] Test: `?culture=zh` renders Chinese; default renders English; no single view mixes languages.
- [x] Test: untranslated key returns English (fallback).
- [x] `dotnet format --verify-no-changes`, build zero warnings, OpenSpec validation passes.

## Implementation notes

- **Resource naming**: `IStringLocalizer<T>` resolves the resource by the **page model
  class name**, not the `.cshtml` file name. The `.zh.resx` files are therefore named after
  the models: `MyCoursesModel.zh.resx`, `Courses/Lessons/ViewModel.zh.resx`,
  `Courses/DetailsModel.zh.resx`, `Courses/EditModel.zh.resx`, `SharedResources.zh.resx`
  (under `Resources/…` with `ResourcesPath = "Resources"`). The SDK embeds them as a `zh`
  satellite assembly (`bin/…/zh/OpenLearning.Web.resources.dll`).
- **New files**: `LocalizationModuleExtensions.AddLocalizationFoundation()` (service
  registration + `RequestLocalizationOptions` config), `SharedResources` marker class
  (with `[SuppressMessage("SonarAnalyzer","S2094")]` since it is intentionally empty).
- **Tests** (`tests/OpenLearning.UnitTests/Localization/LocalizationFoundationTests.cs`, 7
  facts, all green): en renders the English key, zh renders the Chinese value for each
  migrated model, an untranslated key falls back to English (`ResourceNotFound = true`),
  and `RequestLocalizationOptions` is registered with `en` default + `zh` supported + the
  three providers. The options type is resolved reflectively from the registered
  `IConfigureOptions<>` descriptor to avoid a hard compile-time dependency on the ASP.NET
  shared framework in the test project.
- **Verification (green)**: `dotnet build OpenLearning.sln` → 0 warnings / 0 errors;
  `dotnet test tests/OpenLearning.UnitTests` → 481 passed (incl. the 7 localization tests);
  `dotnet format --verify-no-changes` → clean.
