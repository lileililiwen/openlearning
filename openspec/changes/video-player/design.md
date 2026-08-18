# Video Player — Design

## Context

Lessons currently render markdown text or SCORM. Video playback — the dominant medium in the reference system — is absent. This change adds an HTML5-based lesson player with learner features and playback protection.

## Goals

- Instructors attach a video to a lesson (URL produced by `file-storage`).
- Students get a player with speed, resolution, resume, notes, danmu, subtitles, and cast.
- A protected mode restricts seeking and recording.

## Non-Goals

- No DRM/widevine (only soft protections: seek lock, context-menu block, PiP disable, overlay).
- No server-side transcoding in MVP (`file-storage` may provide static renditions; adaptive HLS deferred).
- No video upload UX beyond a URL/upload field (upload mechanics live in `file-storage`).

## Decisions

### D1: Video on `Lesson`
`Lesson.VideoUrl` (nullable) plus optional `VideoPosterUrl`. When set, `View` renders the player; otherwise the existing text/SCORM rendering is used. `LessonService` persists the field.

### D2: Player features (client-side)
A single `MediaPlayer.js` module:
- Speed: `<video playbackRate>` 0.5–2.0.
- Resolution: dropdown over source list (e.g. `VideoUrl` and `VideoUrl?quality=hd` if provided) — MVP: single source unless the instructor supplies multiple.
- Resume: save `currentTime` every 5s to `/progress/position` (POST) and restore on load; persisted on `LessonAccess.PlaybackPositionSeconds`.
- Notes: side panel saving note text per lesson (reuses `study-tools` data model).
- Danmu: overlay reading/writing messages via a `/hubs/course-chat`-style hub with a `Danmu` message type; persisted for replay.
- Subtitles: `<track kind="subtitles">` when a WebVTT URL is provided.
- Cast: `navigator.mediaSession`/`documentPictureInPicture` best-effort button (hidden when unsupported).

### D3: Playback protection
When `lesson.IsPreview == false` and the student is enrolled: enable `controlsList="nodownload"`, `disablePictureInPicture`, `oncontextmenu` return false, and when a course-level "lockSeek" is on, jump the time back on seek events. Protections are client-side soft measures (documented).

## Risks / Trade-offs

- **Soft protection only** → Accepted for MVP; DRM/HLS is a future `file-storage` enhancement.
- **Client-side resume** → Works for one browser; cross-device resume works because position is saved server-side per enrollment.
- **Danmu persistence growth** → Reuse chat tables; old messages pruned by an admin job later.

## Migration Plan

One migration adds `VideoUrl`, `VideoPosterUrl` to `Lessons`, and `PlaybackPositionSeconds` to `LessonAccess`.

## Open Questions

- Adaptive streaming (HLS) and transcoding — deferred to `file-storage`.
