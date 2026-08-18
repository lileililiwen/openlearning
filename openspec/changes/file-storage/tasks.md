# File Storage — Tasks

## 1. Module Setup

- [ ] 1.1 Create `src/OpenLearning.Storage` class library, add to solution, add references (Auth, EF Core)
- [ ] 1.2 Add `StoredFile` + `MediaAsset` entities + configs
- [ ] 1.3 Implement `IStorageProvider` (local) + `StorageService` (upload/open/delete, limits)
- [ ] 1.4 Register assembly scanning + `AddStorageModule`; configure local root

## 2. Serving & Uploads

- [ ] 2.1 `/files/{key}` endpoint with content type + purpose ACL
- [ ] 2.2 Upload UI hook points (avatar, video, courseware, assignment/answer)

## 3. Transcoding

- [ ] 3.1 `MediaTranscoder` background worker (FFmpeg renditions or passthrough) + `RenditionStatus`
- [ ] 3.2 `MediaAsset` rendition URLs surfaced for `video-player`

## 4. Migration & Verification

- [ ] 4.1 Create EF Core migration
- [ ] 4.2 Build, start app, verify: upload stores + serves, size/type limits enforced, path-safe keys, rendition status transitions, private files denied to others
