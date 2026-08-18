# Operations Config — Design

## Context

The homepage has no configurable marketing surface.

## Goals

- Admins manage carousel banners and featured content.
- Admins schedule pop-ups.
- Campaigns group banners/pop-ups with dates.

## Non-Goals

- No A/B testing.
- No analytics of banner clicks in MVP.
- No per-page layouts beyond the homepage.

## Decisions

### D1: New `OpenLearning.Operations` module
`Banner { Id, Title, ImageUrl, LinkUrl, OrderIndex, IsActive, CampaignId? }`, `Popup { Id, Title, Body, LinkUrl, StartsAt, EndsAt, IsActive }`, `Campaign { Id, Name, StartsAt, EndsAt, IsActive }`. Image URLs come from `file-storage`.

### D2: Homepage rendering
`Index` (catalog) page loads `OperationsService.GetActiveBannersAsync()` (active + campaign in window, ordered) into a Bootstrap carousel; featured categories/courses are chosen via a `HomepageFeature { Id, Category?, CourseId?, OrderIndex }` table or admin-picked course ids (decision: `HomepageFeature` rows). Pop-ups are served via a small `GET /ops/popup` returning the active popup JSON; the layout shows it once per session (localStorage key).

### D3: Admin UI
`/Admin/Operations` tabs: Banners (CRUD + order), Pop-ups (CRUD + schedule), Campaigns (CRUD + link banners), Homepage (pick featured categories/courses + order).

## Risks / Trade-offs

- **Stale content** → All queries filter by `IsActive` and campaign window; expired pop-ups auto-hide.
- **Session pop-up** → localStorage key prevents repeat; clearing storage re-shows (documented).

## Migration Plan

One migration creates `Banners`, `Popups`, `Campaigns`, `HomepageFeatures`.

## Open Questions

- Pop-up frequency (every session vs once ever)? MVP: once per session.
