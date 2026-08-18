## Why

Uploads are ad-hoc: SCORM packages and avatars reference local files, and `video-player`/`assignments` need a real home for media. The reference system's Infrastructure lists File Storage for videos, images, and courseware, plus video transcoding as critical.

## What Changes

- A storage abstraction: upload/read/delete blobs (local disk for MVP, S3-compatible adapter interface).
- Stored files get public/private URLs and a `StoredFile { Id, Key, OriginalName, ContentType, SizeBytes, OwnerId, CreatedAt }` record.
- Video transcoding: a background pipeline generates renditions (original + low/medium/high) when a video is uploaded; `video-player` consumes the rendition URLs.
- Size/type validation and path-traversal-safe key generation.

## Capabilities

### New Capabilities
- `file-storage`: blob storage with metadata, URL serving, and video rendition generation.

### Modified Capabilities

- `lesson-preview`/`video-player`/`assignments`/`study-tools`: file URLs come from this module.
- `scorm-content`: package upload reuses the storage layer.

## Impact

- New `OpenLearning.Storage` module: `StoredFile` entity; `IStorageProvider` (local/S3); `StorageService` (upload/read/delete/serve metadata); `MediaTranscoder` background worker producing renditions.
- `/files/{key}` serves public files (gated where needed); admin storage stats page optional.
