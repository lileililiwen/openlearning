## Why

The homepage is static and there is no way to configure promotions. The reference system's Admin Backend requires Operations Configuration: carousel banners, homepage setup, pop-ups, and campaign configuration.

## What Changes

- Carousel banners: admin-defined image/link slides on the homepage.
- Homepage setup: admin picks featured categories/courses and banner order.
- Pop-ups: scheduled announcement pop-ups (e.g. campaign announcements) shown once per session/user.
- Campaigns: named promotions with start/end dates that surface banners/pop-ups and can link to catalog filters or coupons.

## Capabilities

### New Capabilities
- `operations-config`: homepage banners, pop-ups, and campaign configuration.

### Modified Capabilities

- `lms-core`: homepage renders configured banners/featured content.
- `commerce-extras`: coupons can be linked to a campaign.

## Impact

- New `OpenLearning.Operations` module: `Banner { Id, Title, ImageUrl, LinkUrl, OrderIndex, IsActive, CampaignId? }`, `Popup { Id, Title, Body, LinkUrl, StartsAt, EndsAt, IsActive }`, `Campaign { Id, Name, StartsAt, EndsAt, IsActive }`.
- `OperationsService` (CRUD admin, query active for homepage).
- Admin pages `/Admin/Operations` (banners, popups, campaigns); homepage partial renders banners.
