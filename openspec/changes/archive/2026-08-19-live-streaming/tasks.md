# Live Streaming — Tasks

## 1. Module Setup

- [x] 1.1 Create `src/OpenLearning.Live` class library, add to solution, add references (Auth, CourseManagement, Enrollment, Chat, Storage, EF Core)
- [x] 1.2 Add `LiveSession`, `LiveCoHost`, `LiveCheckIn` entities + configs; add `SessionId` to `ChatMessage`
- [x] 1.3 Implement `LiveService` (CRUD owner-gated, start/end, check-in, co-host, list)
- [x] 1.4 Register assembly scanning + `AddLiveModule`

## 2. Room UI & Chat

- [x] 2.1 Live session list on course + create/edit for owner
- [x] 2.2 Live room: player (pull URL), status controls, stream key display (instructor/co-host)
- [x] 2.3 Per-session chat via hub (JoinLive/SendLiveMessage) with persistence
- [x] 2.4 Check-in button (one per user per session) + co-host badges

## 3. Replays

- [x] 3.1 Attach recording (`RecordingFileId`) on end; replay view for ended sessions

## 4. Migration & Verification

- [x] 4.1 Create EF Core migration
- [x] 4.2 Build, start app, verify: schedule → live → ended, chat scoped per session, check-in once, co-host invite, replay shows, non-enrolled denied
