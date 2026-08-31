# Implementation Tasks

## 1. Document language

- [x] In `_Layout.cshtml:45`, set `lang` from the active request culture (default `en`). Add a `ViewData["HtmlLang"]` set in a base page/filter if needed.

## 2. Progress helper + rollout

- [x] Create `Pages/Shared/_ProgressBar.cshtml` (or a `Progress` tag helper) emitting `role="progressbar"`, `aria-valuenow/min/max`, and `aria-label`.
- [x] Replace the 10 inline progress bars (`MyCourses.cshtml:84`, `Dashboard/Index.cshtml:155`, `Details.cshtml:414`, plus the remaining 7 found via grep) with the helper.

## 3. Skip link & landmarks

- [x] Add skip link as the first focusable element in `_Layout.cshtml` with focus-reveal CSS; ensure `<main id="main">`.

## 4. Mobile menu a11y

- [x] Update `site-chrome.js` toggle: `aria-expanded`, `aria-controls`; focus trap on open; `Esc` closes + returns focus; cleanup on close.

## 5. Label sweep

- [x] Grep `<input` without associated label/`aria-label` across `Pages/`. Add labels/`aria-label`, prioritizing `View.cshtml:132`, `Study/Index.cshtml:86`, checkout/cart, search/filter.

## 6. Automated checks & verification

- [x] Add an a11y test (Playwright+axe or xunit+AxeCore) on `Index`, `Dashboard`, `MyCourses`, `Courses/Details`, `Courses/Lessons/View` asserting no violations for lang, progressbar naming, labels, skip link, menu a11y.
- [x] Keyboard smoke test: skip link, mobile menu Esc, keyboard lesson completion.
- [x] `dotnet format --verify-no-changes`, build zero warnings, OpenSpec validation passes.
