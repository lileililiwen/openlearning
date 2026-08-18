# Live Streaming — Tasks

## 1. Module Setup

- [ ] 1.1 Create `src/OpenLearning.Live` class library, add to solution, add references (Auth, CourseManagement, Enrollment, Chat, Storage, EF Core)
- [ ] 1.2 Add `LiveSession`, `LiveCoHost`, `LiveCheckIn` entities + configs; add `SessionId` to `ChatMessage`
- [ ] 1.3 Implement `LiveService` (CRUD owner-gated, start/end, check-in, co-host, list)
- [ ] 1.4 Register assembly scanning + `AddLiveModule`

## 2. Room UI & Chat

- [ ] 2.1 Live session list on course + create/edit for owner
- [ ] 2.2 Live room: player (pull URL), status controls, stream key display (instructor/co-host)
- [ ] 2.3 Per-session chat via hub (JoinLive/SendLiveMessage) with persistence
- [ ] 2.4 Check-in button (one per user per session) + co-host badges

## 3. Replays

- [ ] 3.1 Attach recording (`RecordingFileId`) on end; replay view for ended sessions

## 4. Migration & Verification

- [ ] 4.1 Create EF Core migration
- [ ] 4.2 Build, start app, verify: schedule → live → ended, chat scoped per session, check-in once, co-host invite, replay shows, non-enrolled denied
