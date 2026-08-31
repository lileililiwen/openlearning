# Proposal: Learner Accessibility Compliance

## Problem

The UI fails its own stated target (the `learner-experience-quality` design requires WCAG 2.2 AA) on
several concrete points:

- `<html lang="zh">` (`_Layout.cshtml:45`) is hard-coded even though most content is English, so
  screen readers mispronounce everything.
- **10 progress bars have no `aria-valuenow/min/max`** (`MyCourses.cshtml:84`, `Dashboard/Index.cshtml:155`,
  `Details.cshtml:414`); the value is CSS-width-only and invisible to assistive tech.
- There is **no skip-to-content link**; `<main>` is the only landmark.
- The mobile sidebar toggle (`site-chrome.js:9-24`) has no `aria-expanded`/`aria-controls`, no focus
  trap, no Esc-to-close, and no focus return.
- **101 `<input name=`** elements have no associated `<label for>` (notably lesson/study/checkout inputs).

Accessibility is not cosmetic: it is the difference between the product being usable at all for a
large share of learners, and it is a hard requirement for institutional adoption.

## Change

Bring learner-facing pages to WCAG 2.2 AA: correct document language, expose progress values to AT,
add skip link and landmark structure, make the mobile menu keyboard-operable, and label all form controls.

## Non-goals

- A full third-party audit (this change establishes the automated baseline + fixes the concrete defects above).
- Visual theme redesign (see `responsive-design-system`).

## Impact

- Updated: `Pages/Shared/_Layout.cshtml` (lang, skip link, landmarks, mobile toggle attributes),
  `site-chrome.js` (menu a11y), progress markup (standardize via a `Progress` tag helper or partial),
  the 101 unlabeled inputs (priority: lesson/study/checkout).
- New: an automated accessibility check (e.g. `xunit` + `Axe`/`FluentA11y` or Playwright a11y snapshot)
  for representative learner pages.
- ADDED Requirements only.
