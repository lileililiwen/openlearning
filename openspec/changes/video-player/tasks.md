# Video Player — Tasks

## 1. Data & Persistence

- [ ] 1.1 Add `VideoUrl`, `VideoPosterUrl` to `Lesson` + config; extend `LessonService`
- [ ] 1.2 Add `PlaybackPositionSeconds` to `LessonAccess`; add `ProgressService.SavePositionAsync`/`GetPositionAsync`

## 2. Player UI

- [ ] 2.1 Render HTML5 player in lesson `View` when `VideoUrl` is set
- [ ] 2.2 `MediaPlayer.js`: speed, resolution (multi-source), subtitles track, cast button
- [ ] 2.3 Resume: periodic position save + restore on load
- [ ] 2.4 Danmu overlay + hub message type + persistence
- [ ] 2.5 Notes side panel (links to study-tools data model)

## 3. Playback Protection

- [ ] 3.1 `controlsList`, `disablePictureInPicture`, context-menu block, seek lock for enrolled/non-preview lessons

## 4. Migration & Verification

- [ ] 4.1 Create EF Core migration
- [ ] 4.2 Build, start app, verify: video lesson renders, speed/resume/danmu work, position persists and restores, protection flags present, non-enrolled gated
