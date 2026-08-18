# Live Chat — Design

## Context

The LMS is fully server-rendered Razor Pages; there is no real-time interaction. This change adds per-course chat using SignalR (part of the ASP.NET Core shared framework), giving students and instructors a place to converse live while keeping history persisted.

## Goals

- A chat room per course, accessible to enrolled students and the course owner.
- Real-time delivery of new messages via SignalR groups.
- Message history persisted in PostgreSQL and shown on page load.

## Non-Goals

- No video conferencing (WebRTC signaling + media are deferred to a future change).
- No message editing/deletion, attachments, or presence/typing indicators.
- No global/public chat; chat is always scoped to a course.
- No chat on free courses only — any course with the right membership can chat.

## Decisions

### D1: New `OpenLearning.Chat` module with a SignalR hub
`ChatMessage { Id, CourseId, UserId, Body, SentAt }` plus `ChatService` (recent messages, add message) and a `CourseChatHub` (SignalR). The module references Auth, CourseManagement, Enrollment, and `FrameworkReference Microsoft.AspNetCore.App` for the Hub base class. It does not reference `OpenLearning.Data`; services use the base `DbContext` + `Set<T>()` per the established pattern.

### D2: SignalR groups per course
Clients call `JoinCourse(courseId)` on connect; the hub adds them to `Group("course-" + courseId)`. `SendMessage(courseId, body)` validates the sender is enrolled or owns the course, persists the message via `ChatService`, then broadcasts `ReceiveMessage(userName, body, sentAt)` to the group. Rationale: group filtering is simpler and more efficient than per-client filtering.

### D3: Membership gating
The hub resolves the current user from `Context.User` (the auth cookie travels with the same-origin WebSocket). `JoinCourse` and `SendMessage` both reject users who are neither enrolled in the course nor its owner. The chat page itself also enforces the same rule server-side.

### D4: Message persistence and initial load
The chat page loads the 50 most recent messages server-side on GET; new messages append via the hub. Persisting to the DB (rather than an in-memory ring buffer) means history survives server restarts and reloads.

### D5: JS client via CDN
The page uses the `@microsoft/signalr` client from CDN (same pattern as Bootstrap). The hub path is `/hubs/course-chat`.

## Risks / Trade-offs

- **Unauthenticated/unauthorized hub access** → Every hub method re-validates membership server-side; the UI link is gated but the hub never trusts the client.
- **Message flood** → MVP accepts simple `Body` length limits (e.g., 2000 chars) at the service boundary; rate limiting deferred.
- **CDN dependency for SignalR client** → Consistent with existing Bootstrap CDN; a bundled client is a future hardening step.

## Migration Plan

One EF migration (`AddLiveChat`) creates `ChatMessages`. Applied on startup. Rollback: drop the migration and the table.

## Open Questions

- Should the owner be able to delete messages? Deferred.
- Video calling: a future change will add WebRTC signaling (likely reusing the same SignalR hub infrastructure).
