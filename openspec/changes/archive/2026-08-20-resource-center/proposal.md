## Why

Uploads already funnel into a single `StoredFile` table and stream through the
`/files/{key}` endpoint, but there is **no browsable library**: instructors
hand-paste `/files/video/...` keys into the lesson video/poster/subtitle URL
fields, and the only generic upload surface is an unreferenced dev page. The
brief asks for a **resource center** — a place to upload images/videos
(documents too) once and reuse them anywhere a URL field exists.

## What Changes

- A **Resource Center**: upload, browse/search/filter, preview, copy URL,
  delete, and (admin) share resources across the platform.
- Two new generic purposes — `FilePurpose.Image` and `FilePurpose.Document` —
  so images and documents have a home (video already has `FilePurpose.Video`).
- A **"choose from resources" picker** integrated into every relevant URL
  field: lesson `VideoUrl`/`VideoPosterUrl`/`SubtitleUrl`, profile `AvatarUrl`,
  admin banner `ImageUrl`.
- Visibility: each user sees their own uploads; admins see everything and can
  mark a resource **shared** so any authenticated user can reuse it. Files
  referenced by public course content stay publicly readable through the
  existing `/files` proxy (the proxy ACL is unchanged).

## Capabilities

### New Capabilities

- `resource-center`: a per-user/admin resource library with upload, browse,
  preview, copy-URL, delete, and admin sharing.

### Modified Capabilities

- `storage`: `FilePurpose` gains `Image` and `Document`; `StoredFile` gains an
  admin-controlled `IsShared` flag.
- `course-structure`: lesson `VideoUrl`/`VideoPosterUrl`/`SubtitleUrl` inputs
  gain a "choose from resources" button.
- `account-settings`: profile avatar URL input gains a resource picker.
- `operations`: admin banner `ImageUrl` input gains a resource picker.

## Impact

- New `OpenLearning.ResourceCenter` module: `ResourceService`
  (`ListAsync`, `DeleteAsync`, `SetSharedAsync`) over `DbContext` +
  `StorageService`; no new blob tables — `StoredFile` stays the single source
  of truth. EF migration `AddResourceCenter` adds `StoredFile.IsShared`.
- Pages: `Pages/Resources/Index.cshtml(.cs)` (library grid + upload + delete +
  share + copy URL), `Pages/Resources/Picker.cshtml(.cs)` (server-rendered
  picker partial usable from any form), `Pages/Resources/Upload.cshtml(.cs)`
  (purpose-aware upload returning a selectable result).
- One-line DI: `builder.Services.AddResourceCenterModule();`.
- The picker is a reusable Razor partial + small JS that POSTs a search to the
  picker and fills a hidden/visible URL input on selection.
