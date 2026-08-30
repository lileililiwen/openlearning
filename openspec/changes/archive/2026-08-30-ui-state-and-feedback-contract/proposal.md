# Proposal: UI State & Feedback Contract

## Problem

The platform has **no shared language for state**. There are zero loading states across 223 pages;
fetches swallow errors silently (`View.cshtml:202-231`, `site-chrome.js:34` `.catch(){/* best-effort */}`).
Actions set `TempData["Message"]` on **18 pages** but never render it (e.g. `Courses/Quizzes/Take`,
`Courses/Checkout`, `Surveys/Take`, `LearningPaths/Index`, `Courses/Assignments/Detail`), so learners
get no confirmation that anything happened. Feedback is copy-pasted per page (`Study/Index.cshtml:14-20`).
Destructive actions are unconfirmed except a handful (`Unpublish` `Details.cshtml:467`, `MarkAllRead`
`Notifications/Index.cshtml:14`). Forms do full-page postbacks, so ticking a checkbox (`View.cshtml:119`)
reloads the whole lesson + video. On failure, entered text is lost (note textarea `View.cshtml:132`).

This violates the most basic usability principle: **every action needs a visible, honest reaction**.
The unimplemented `learner-experience-quality` proposal calls for explicit loading/empty/error/success
states; this change supplies the concrete primitives and wiring to realize it.

## Change

Introduce shared Razor partials and helpers for loading, empty, error, success/toast, confirmation,
and disabled states; render `TempData` messages globally via a toast; add a confirmation helper for
destructive actions; preserve input on failure; and add lightweight progressive enhancement for the
three highest-traffic in-lesson actions.

## Non-goals

- Rewriting every page (apply incrementally; new pages must use the primitives).
- A client-side SPA framework.

## Impact

- New: `Pages/Shared/_StateLoading.cshtml`, `_StateEmpty.cshtml`, `_StateError.cshtml`, `_Toast.cshtml`;
  a `confirm` tag helper or `data-confirm` JS; a `site-chrome.js` `postWithFeedback` helper.
- Updated: `_Layout.cshtml` (render `_Toast` from `TempData`); `site-chrome.js`; 18 silent pages; the
  3 lesson actions; destructive-action forms.
- Complements `learner-experience-quality` and `learner-accessibility-compliance`.
- ADDED Requirements only.
