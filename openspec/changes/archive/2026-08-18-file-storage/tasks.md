# File Storage — Tasks

## 1. Module Setup

- [x] 1.1 Create `src/OpenLearning.Storage` class library, add to solution, add references (EF Core + ASP.NET Core framework)
- [x] 1.2 Add `StoredFile` + `MediaAsset` entities + configs
- [x] 1.3 Implement `IStorageProvider` (local) + `StorageService` (upload/open/delete, limits)
- [x] 1.4 Register assembly scanning + `AddStorageModule`; configure local root (`Storage:Root`, default `storage/` under content root)

## 2. Serving & Uploads

- [x] 2.1 `/files/{**key}` endpoint with content type + purpose ACL (catch-all route since keys contain slashes; private purposes return 403 to non-owners)
- [x] 2.2 Upload UI hook points (avatar, video, courseware, assignment/answer) — `/Files` page exercises all purposes

## 3. Transcoding

- [x] 3.1 `MediaTranscoder` background worker (FFmpeg renditions or passthrough) + `RenditionStatus`
- [x] 3.2 `MediaAsset` rendition URLs surfaced for `video-player` — `/files/{id}/renditions` endpoint + rendition blobs served by key

## 4. Migration & Verification

- [x] 4.1 Create EF Core migration
- [x] 4.2 Build, start app, verify: upload stores + serves, size/type limits enforced, path-safe keys, rendition status transitions, private files denied to others — HTTP smoke-tested: upload→serve 200, disallowed type & oversize rejected, real video → renditions ready (low/mid/high served 200 video/mp4), private answer 200 for owner / 403 for instructor
