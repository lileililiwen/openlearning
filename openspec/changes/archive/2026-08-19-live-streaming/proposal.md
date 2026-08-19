## Why

Course chat exists but there are no live sessions. The reference system's Infrastructure lists live streaming (if live classes are offered): live rooms, stream push/pull, replays, live chat, co-hosting, and live check-ins. Live classes are a common premium feature.

## What Changes

- Live sessions on a course: instructor creates a scheduled live event with a start/end time.
- Live room page: embedded player (HLS pull) for enrolled students; instructor gets a push/stream key.
- Live chat reuses the course chat hub per-session; co-hosting (multiple presenters) supported via invite.
- Live check-in: instructor opens a check-in during the session; enrolled attendees mark presence.
- Replay: the session's recording is stored (`file-storage`) and becomes a lesson or a replay view.

## Capabilities

### New Capabilities
- `live-streaming`: scheduled live sessions with chat, co-hosting, check-ins, and replays.

### Modified Capabilities

- `live-chat`: chat rooms gain a per-session mode.
- `file-storage`: live recordings stored as video assets.

## Impact

- New `OpenLearning.Live` module: `LiveSession { Id, CourseId, InstructorId, Title, StartsAt, EndsAt, StreamKey, Status, RecordingFileId? }`, `LiveCoHost { Id, SessionId, UserId }`, `LiveCheckIn { Id, SessionId, UserId, CheckedInAt }`.
- `LiveService` (CRUD owner-gated, check-in, status transitions, replay attach); SignalR hub extension for live chat + check-in counts.
- Pages under `Pages/Courses/Live/` (schedule, room, replay).
