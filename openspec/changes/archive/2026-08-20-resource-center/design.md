## Context

The platform already stores every upload as a `StoredFile` row (owner, purpose,
original name, size, content type, server-generated key) and serves it through
`GET /files/{**key}` with an ACL that only locks down private purposes. The
missing piece is entirely user-facing: a library over those rows, a delete
surface (currently nothing deletes files except the storage module's internal
calls), and a way to pick a stored file into the many plain-URL fields.

We deliberately reuse `StoredFile` instead of inventing a parallel "resource"
table — the resource center IS the metadata we already record. The new
`Image`/`Document` purposes let the upload surface accept the file types the
existing purposes were too narrow for, and the `IsShared` flag implements the
"admin shares into a common library" model without a full ACL rework.

## Goals / Non-Goals

**Goals:**
- A library page where a user sees their own uploads and admins see all.
- Upload of images, videos, and documents with immediate URL copy.
- A picker that fills the existing URL fields (lesson media, avatar, banner).
- Admin marking of shared resources.

**Non-Goals:**
- Per-folder organization, tags, or nested collections (future capability).
- Modifying existing files/versioning (append-only resources; re-upload for a
  new version).
- Changing the storage backend — that is `storage-strategy`.
- Granular per-course or per-role read ACLs beyond owner/admin/shared.

## Decisions

- **Library = `StoredFile`**; `IsShared` bool (admin-only write) on
  `StoredFile`, default false. Shared resources are served to any authenticated
  user; they are never `IsPrivate`.
- **New purposes `Image`/`Document`** with their own size/extension limits
  (aligned with `storage-strategy`'s configurable limits; defaults: image 10 MB
  jpg/png/webp/gif/svg, document 100 MB pdf/doc/docx/ppt/pptx/xls/xlsx/zip).
- **Picker as a Razor partial** rendered from `/Resources/Picker`; it accepts a
  `purpose` filter (image/video/document), a search term, and returns a grid of
  selectable cards; the target form's input is filled by a tiny script.
- **Deletion is owner-or-admin** and cascades to rendition blobs via the
  existing `StorageService.DeleteAsync`; deleting a file referenced by a lesson
  is allowed and leaves the referencing field broken (surfaced as a
  confirmation warning on the delete button).

## Risks / Trade-offs

- [Risk: deleting a file that a published lesson references] → Mitigation:
  the delete flow shows the resource URL so the user can find references; the
  action is owner-scoped and reversible only by re-uploading.
- [Risk: the picker grows large] → Mitigation: server-side paging + search +
  type filter; renders 24 per page.
- [Risk: `IsShared` rows leak private content] → Mitigation: `SetSharedAsync`
  refuses to mark `IsPrivate` purposes (e.g. `Answer`) shared; the serving ACL
  already blocks private purposes for non-owners.

## Migration Plan

1. Add `FilePurpose.Image`/`Document` + `StoredFile.IsShared` + migration.
2. Add `OpenLearning.ResourceCenter` module + `ResourceService`.
3. Build the library + upload + picker pages.
4. Wire the picker into lesson/profile/banner URL fields.

## Open Questions

- Should students get a resource center? No — the library is instructor/admin
  facing; students only consume content.
- Should resources support folders later? Out of scope; the model keeps a
  `purpose` + `IsShared` seam a future folder feature can build on.
