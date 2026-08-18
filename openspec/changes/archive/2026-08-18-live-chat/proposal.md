## Why

Course discussion happens outside the platform today. A built-in, real-time course chat keeps learners and instructors together, which is a prerequisite for the "live video and chat" roadmap goal.

## What Changes

- New `live-chat` capability: each course gets a chat room.
- Enrolled students and the course owner can post messages; messages are broadcast in real time via SignalR and persisted so they survive reloads.
- New `OpenLearning.Chat` class library: `ChatMessage` entity, `ChatService`, and a SignalR hub (`/hubs/course-chat`). Uses the ASP.NET Core shared framework (SignalR) — no new packages.
- Chat UI is a page per course listing recent messages with a live composer.
- **Video is explicitly out of scope** for this change (deferred to a future WebRTC-based change).

## Capabilities

### New Capabilities
- `live-chat`: per-course real-time chat for enrolled students and the course owner, with persisted message history.

### Modified Capabilities

None.

## Impact

- New `src/OpenLearning.Chat` project referencing `OpenLearning.Auth`, `OpenLearning.CourseManagement`, and `OpenLearning.Enrollment`; `FrameworkReference` to `Microsoft.AspNetCore.App` for SignalR.
- New table `ChatMessages`; one EF Core migration.
- `Program.cs`: `AddSignalR()` + `MapHub<CourseChatHub>("/hubs/course-chat")`.
- New chat page under `Pages/Courses/` linked from the course details page; SignalR JS client loaded from CDN (consistent with existing Bootstrap CDN usage).
- No changes to existing capabilities.
