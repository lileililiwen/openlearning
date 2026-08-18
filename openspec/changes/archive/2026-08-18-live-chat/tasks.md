# Live Chat — Tasks

## 1. Module Setup

- [x] 1.1 Create `src/OpenLearning.Chat` class library (with `FrameworkReference` Microsoft.AspNetCore.App) and add it to the solution
- [x] 1.2 Add project references (Auth, CourseManagement, Enrollment, EF Core)

## 2. Data Model & Services

- [x] 2.1 Add `ChatMessage` entity + `IEntityTypeConfiguration`
- [x] 2.2 Implement `ChatService`: recent messages, add message (membership check), is-participant check
- [x] 2.3 Register assembly scanning in `ApplicationDbContext` and `AddChatModule` in `Program.cs`

## 3. SignalR Hub

- [x] 3.1 Implement `CourseChatHub`: `JoinCourse` (group add), `SendMessage` (validate + persist + broadcast)
- [x] 3.2 Wire `AddSignalR()` and `MapHub<CourseChatHub>("/hubs/course-chat")` in `Program.cs`

## 4. UI

- [x] 4.1 Chat page (`Pages/Courses/Chat.cshtml`): recent messages + composer + SignalR JS client
- [x] 4.2 Link to chat from the course details page

## 5. Migration & Verification

- [x] 5.1 Create EF Core migration (`AddLiveChat`)
- [x] 5.2 Run `dotnet build` and start the app
- [x] 5.3 Verify with a .NET SignalR client: connect, join group, send/receive, confirm persistence and membership gating
