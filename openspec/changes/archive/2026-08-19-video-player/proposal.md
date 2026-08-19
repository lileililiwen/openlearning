## Why

Lesson content is text or SCORM; there is no video delivery. The reference system's Core Course module centers on video playback: player controls, speed, resolution, resume, notes, bullet comments (danmu), subtitles, screen casting, and anti-scrubbing/anti-recording restrictions. Video is the core medium for an online learning platform.

## What Changes

- Instructors upload a video to a lesson (MP4/WebM) via the `file-storage` capability; lessons with a video get an HTML5 `<video>` player.
- Player features: playback speed, resolution selection (when multiple renditions exist), resume from last position, lesson notes side panel, danmu overlay, subtitle tracks (WebVTT), screen cast (basic, via browser Media Session where supported).
- Anti-scrubbing: seek restricted in a content-gated mode; anti-recording: overlay + context-menu/`ContextMenu`-disabled flags and optional `disablePictureInPicture`.
- Study duration statistics for video lessons feed progress (`study-duration` change).

## Capabilities

### New Capabilities
- `video-player`: video lessons with a feature-rich HTML5 player and playback protection.

### Modified Capabilities

- `course-structure`: lessons gain a video attachment (URL to stored media).
- `progress-tracking`: resume position persists per enrollment/lesson.

## Impact

- `Lesson` gains `VideoUrl` (or a `LessonMedia` record) referencing `file-storage`; `LessonService` persists it.
- New `Pages/Courses/Lessons/Play.cshtml` (or reuses `View`) hosting the player; `MediaPlayer.js` implementing controls/danmu/resume.
- `LessonAccess` gains `PlaybackPositionSeconds`; `ProgressService` records/reads it.
- Danmu stored via the existing chat infrastructure as a `DanmuMessage`-typed message (or a small `DanmuMessage` table).
