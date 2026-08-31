# Implementation Tasks

## 1. Tokens

- [x] Add `:root` token block to `site.css` (colors, spacing, radius). Replace hardcoded hexes in `site.css` + `site-chrome.css` with `var(--...)`.

## 2. Contrast

- [x] Raise `.nav-item-link` (and any sub-AA text) contrast to ≥ 4.5:1 via token choices; document computed ratios in a comment.

## 3. Tables

- [x] Grep `<table` across `Pages/`; wrap the 34 lacking `.table-responsive` (or apply scoped rule). Verify at 360px.

## 4. CDN safety

- [x] Add `integrity` + `crossorigin` to Bootstrap CDN `<link>`/`<script>` in `_Layout.cshtml`; add local fallback so the bootstrap-global popup script no longer races the bundle.

## 5. Breakpoints

- [x] Document content-fit breakpoints as named tokens; align the sidebar collapse point to the token.

## 6. Verification

- [x] Contrast check (assert nav/body AA). Responsive smoke at 360/768/1280px for key pages (no horizontal scroll on ordinary content).
- [x] `dotnet format --verify-no-changes`, build zero warnings, OpenSpec validation passes.
