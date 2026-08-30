# Design: UI State & Feedback Contract

## Decisions

- **Shared partials** (server-rendered, no JS required):
  - `_StateLoading` — spinner/skeleton with `aria-busy="true"` and a localized label.
  - `_StateEmpty` — icon + message + optional primary action (e.g. "Browse catalog").
  - `_StateError` — message + "Retry" that re-submits the last safe action/idempotent GET.
  - `_Toast` — success/info/warning/danger alert used for transient feedback.
- **Global toast**: `_Layout.cshtml` renders `_Toast` once, reading `TempData["Message"]` and
  `TempData["MessageType"]` (existing convention). This single change makes the 18 currently-silent
  pages show feedback without editing each. For pages that additionally need inline confirmations,
  keep the existing per-page `TempData["Message"]` block OR rely on the global one (pick global to avoid duplicates).
- **Confirmation helper**: a `confirm` tag helper / `data-confirm="..."` attribute that intercepts
  form submit / link click and shows a native `confirm()` (or a styled modal later) before proceeding.
  Apply to: `Unpublish` (`Details.cshtml:467`), `MarkAllRead` (`Notifications/Index.cshtml:14`),
  `Withdraw` (already has inline `confirm` — leave), `Delete`, and any bulk destructive action.
- **Input preservation**: ensure form models repopulate on validation failure (use bound model
  properties, not bare `name=`). Specifically fix `View.cshtml:132` note textarea and
  `Study/Index.cshtml:86` check-in note to bind to the model so a failed save keeps the text.
- **Progressive enhancement (3 actions)**: `mark-complete`, `save-note`, `withdraw` become `fetch`
  POSTs to the existing page handlers, returning a PartialView; `_Layout`/`site-chrome.js` swaps the
  partial and shows a toast. **No-JS fallback**: the original `<form method="post">` remains so the
  action still works without JS (per `learner-experience-quality` design: preserve server-rendered behavior).
  Loading state: disable the button + show inline spinner during the request; error: restore button + toast.

## Failure handling

- Toast auto-dismisses (e.g. 5s) but remains in the DOM for screen readers until dismissed; `role="status"`.
- PE fetch failures show the toast error and leave the page in its prior state (no lost data).
- Confirmation cancel aborts the submit; nothing changes.

## Verification

- Add a partial-render view test for each state variant.
- Add a test asserting `TempData["Message"]` is surfaced by `_Layout` (global toast) for one silent page.
- Keyboard test: confirmation dialog is operable; focus returns to the trigger after cancel.
- `dotnet format` + build zero warnings; OpenSpec validation passes.
