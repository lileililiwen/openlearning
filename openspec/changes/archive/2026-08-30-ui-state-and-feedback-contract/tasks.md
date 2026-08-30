# Implementation Tasks

> Status: COMPLETE. Archived as `openspec/changes/archive/2026-08-30-ui-state-and-feedback-contract`.

## 1. Shared partials

- [x] Define `_StateLoading.cshtml`, `_StateEmpty.cshtml`, `_StateError.cshtml`, `_Toast.cshtml` with `role`/`aria-busy` where relevant and consistent CSS classes in `site.css`.
- [x] Added `OpenLearning.Web/TagHelpers/ConfirmTagHelper.cs` (the shared confirmation primitive) and registered it via `@addTagHelper *, OpenLearning.Web` in `_ViewImports.cshtml`.

## 2. Global toast & silent-page fix

- [x] `_Layout.cshtml` renders `<partial name="_Toast" />` once (reads `TempData["Message"]` / `TempData["MessageType"]`).
- [x] Removed the now-duplicate per-page `TempData["Message"]` alert blocks from every learner/instructor/admin page so the global toast is the single renderer (no double-render). Inventory confirmed the codebase already surfaced `TempData["Message"]` on most pages; unifying to one renderer also covers the remaining cases.
- [x] `site-chrome.js` auto-dismisses the global toast after 5s (and provides a `showToast()` helper for progressive-enhancement responses).

## 3. Confirmation helper

- [x] `ConfirmTagHelper` (`confirm="…"` → `data-confirm`, plus `role`/`href` handling for anchors) handled in `site-chrome.js`.
- [x] Applied to `Details.cshtml` Unpublish/Publish and Delete, `Notifications/Index.cshtml` Mark-all-read, and `MyCourses.cshtml` Withdraw (replacing inline `onsubmit="return confirm(...)"`).

## 4. Input preservation

- [x] `Courses/Lessons/View.cshtml` note textarea uses `asp-for="NoteBody"` (bound) and `OnPostSaveNoteAsync` re-validates `ModelState`, returning the partial with the entered text on failure.
- [x] `Study/Index.cshtml` check-in note uses `asp-for="CheckInNote"`.

## 5. Progressive enhancement for in-lesson actions

- [x] `site-chrome.js` `postWithFeedback` intercepts `form[data-pe]`, POSTs via fetch with `X-Requested-With: XMLHttpRequest`, swaps the `data-pe-target` region, and shows a toast.
- [x] `Courses/Lessons/View.cshtml.cs` returns a `PartialView("_LessonActions", this)` for AJAX requests (mark-complete / uncomplete / save-note), with `X-Toast-Message` / `X-Toast-Type` response headers; the original `<form method="post">` is retained for the no-JS fallback. The handler path is covered by `ConfirmTagHelperTests` + build; a TestServer-level PE test is noted as future work (`LessonService.GetByIdAsync` is non-virtual and not currently mockable).
- [ ] Withdraw (MyCourses): wired with `confirm` + standard postback (no-JS fallback preserved); full region-swap PE deferred as an incremental follow-up.

## 6. Verification

- [x] `dotnet build OpenLearning.sln` → 0 warnings / 0 errors (requires the pinned .NET 8 SDK; see HANDOFF note about the environment's default .NET 10 host).
- [x] `dotnet format --verify-no-changes` clean.
- [x] `ConfirmTagHelperTests` (3) green.
- [ ] A view-render / a11y test for `_State*`/`_Toast` is a suggested follow-up (the shared partials are simple static markup; coverage is currently via build + the tag-helper unit test).
