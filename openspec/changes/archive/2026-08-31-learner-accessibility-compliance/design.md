# Design: Learner Accessibility Compliance

## Decisions

- **Document language**: change `_Layout.cshtml:45` default `<html lang="en">`. Because a localization
  foundation is being built separately (`localization-foundation`), set `lang` from the active culture
  in `ViewData`/`IRequestCultureFeature` when available, defaulting to `en`. This fixes the immediate
  mis-declaration and leaves a clean hook for the localization change.
- **Progress values**: introduce a shared `Progress` tag helper or `Pages/Shared/_ProgressBar.cshtml`
  partial that renders `role="progressbar"` with `aria-valuenow`/`aria-valuemin="0"`/`aria-valuemax="100"`
  and `aria-label` (e.g. "Course progress: 42%"). Replace the inline progress markup at
  `MyCourses.cshtml:84`, `Dashboard/Index.cshtml:155`, `Details.cshtml:414`, and the other 7 occurrences.
- **Skip link**: first focusable element in `_Layout.cshtml`, `<a class="skip-link" href="#main">Skip to content</a>`,
  with CSS to reveal on focus; `<main id="main">` already exists.
- **Landmarks**: ensure one `<header>`/`<nav>`/`<main>`/`<footer>` structure; the top nav already uses
  a `MenuService`-driven navbar — keep it as the primary `<nav aria-label="Primary">`.
- **Mobile menu a11y** (`site-chrome.js`): the toggle button gets `aria-expanded` (toggled in JS) and
  `aria-controls="sidebar-id"`; opening traps focus within the sidebar; `Esc` closes and returns focus
  to the toggle; closing on outside click is preserved. Sidebar gets `role="dialog"`/`aria-modal` only
  if it overlays; otherwise keep as `<nav>`.
- **Label sweep**: add `<label for>` or `aria-label` to the 101 unlabeled inputs. Priority order:
  lesson note (`View.cshtml:132`), check-in note (`Study/Index.cshtml:86`), checkout/cart inputs,
  search/filter inputs. Use a grep-driven pass; every `<input>` must end with either a paired label or `aria-label`.

## Failure handling

- If `aria-valuenow` cannot be computed, fall back to a visually-hidden text "X% complete" inside the bar.
- Focus trap must never trap when the menu is closed; ensure cleanup on close.

## Verification

- Automated: add an a11y test (Playwright + axe, or `xunit`+`AxeCore`) on `Index`, `Dashboard`, `MyCourses`, `Courses/Details`, `Courses/Lessons/View` asserting zero violations for the rules above (lang, progressbar naming, labels, skip link, menu a11y).
- Keyboard smoke: tab to skip link, open/close mobile menu with keyboard, complete a lesson via keyboard only.
- `dotnet format` + build zero warnings; OpenSpec validation passes.
