# File Storage — Design

## Context

Files are stored as ad-hoc local paths. A unified storage layer is the base for video, courseware, assignment uploads, and avatars.

## Goals

- Upload/read/delete blobs through one interface.
- Metadata records for every stored file.
- Video renditions generated in the background for the player.
- Path-safe, size-limited uploads.

## Non-Goals

- No CDN integration in MVP (direct serving).
- No virus scanning.
- No upload resumability in MVP.

## Decisions

### D1: New `OpenLearning.Storage` module
`StoredFile { Id, Key, OriginalName, ContentType, SizeBytes, OwnerId, Purpose (Avatar/Video/Courseware/Assignment/Answer), CreatedAt }` (unique `Key`). `IStorageProvider { SaveAsync(stream, key), OpenAsync(key), DeleteAsync(key) }` with `LocalStorageProvider` (root from config) and a documented `S3StorageProvider` adapter point.

### D2: Uploads
`StorageService.UploadAsync(ownerId, purpose, fileName, contentType, stream, maxBytes)` validates type/size against per-purpose limits (from `system-config`), generates a safe key (`{purpose}/{guid}{ext}` — never user input in the path), stores, and records `StoredFile`. Serving: `/files/{key}` streams with the stored content type; keys marked private are checked against the requesting user (purpose-based ACL).

### D3: Video transcoding
On upload with purpose `Video`, enqueue a work item: a background `MediaTranscoder` (using FFmpeg when available; otherwise single-source passthrough) writes `{key}.low.mp4`, `{key}.mid.mp4`, `{key}.high.mp4` and updates a `MediaAsset { StoredFileId, Renditions }` record. `video-player` reads renditions; `RenditionStatus` (Pending/Ready/Failed) drives the player's resolution dropdown.

## Risks / Trade-offs

- **Path traversal** → Keys are server-generated GUIDs + whitelisted extensions; never derived from user input.
- **Transcoder availability** → FFmpeg optional; passthrough mode keeps MVP runnable without it.
- **Storage growth** → Purpose-based limits and an admin stats/cleanup page (delete orphans) later.

## Migration Plan

One migration creates `StoredFiles` and `MediaAssets`.

## Open Questions

- Public vs private default per purpose: avatars/videos/courseware public; assignment answers private (ACL).
