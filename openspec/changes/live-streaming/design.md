# Live Streaming — Design

## Context

Chat is real-time but sessions are not. Live classes add scheduled streaming with interaction.

## Goals

- Instructors schedule live sessions; students see an upcoming/ongoing live list per course.
- Live room shows the stream, chat, co-hosts, and check-in.
- Recorded sessions become replays.

## Non-Goals

- No SFU/WebRTC video (HLS pull from an external streaming platform via the stream key; WebRTC co-host transport deferred).
- No streaming server implementation — the platform manages sessions/keys and embeds the pull URL.
- No transcoding of live (external provider handles it).

## Decisions

### D1: New `OpenLearning.Live` module
`LiveSession { Id, CourseId, InstructorId, Title, StartsAt, EndsAt, StreamKey (secret, instructor-only), StreamUrl (pull URL template), Status (Scheduled/Live/Ended), RecordingFileId? }`. `LiveCoHost { Id, SessionId, UserId }` unique `(SessionId, UserId)`. `LiveCheckIn { Id, SessionId, UserId, CheckedInAt }` unique `(SessionId, UserId)`. `LiveService`: CRUD owner-gated, `StartAsync`/`EndAsync` (status + optional recording attach), check-in, co-host invite/remove, list per course.

### D2: Room page
`/Courses/Live/{id}`: enrolled students see the player (`StreamUrl` when Live), the session chat (a `LiveSessionId`-scoped hub group reusing `CourseChatHub` message model), a check-in button (one per user per session, active window), and co-host badges. Instructor sees the stream key + status controls. Ended sessions show the replay (`RecordingFileId` → `file-storage` video).

### D3: Chat scoping
Extend the chat hub with `JoinLive(sessionId)`/`SendLiveMessage`; messages stored with a `SessionId` on `ChatMessage` (nullable for course-wide chat). Live chat persists for replay context.

## Risks / Trade-offs

- **Stream key secrecy** → Only the instructor (and co-hosts) see the key; key rotates per session.
- **External dependency** → The pull URL is configured; the platform degrades to a "stream starting soon" screen when the provider is down.

## Migration Plan

One migration creates `LiveSessions`, `LiveCoHosts`, `LiveCheckIns`, and adds `SessionId` to `ChatMessage`.

## Open Questions

- Should live check-ins count toward attendance reports? MVP: stored; reports later.
