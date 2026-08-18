# Notifications — Tasks

## 1. Module Setup

- [x] 1.1 Create `src/OpenLearning.Notifications` class library and add it to the solution
- [x] 1.2 Add project references (Auth, CourseManagement, Enrollment, EF Core)
- [x] 1.3 Add `Notification` + `CourseAnnouncement` entities + configs
- [x] 1.4 Implement `NotificationService` (create, recent, mark read, unread count) and `AnnouncementService` (post owner-only, list)
- [x] 1.5 Register assembly scanning in `ApplicationDbContext` and `AddNotificationsModule` in `Program.cs`

## 2. UI

- [x] 2.1 Notifications bell with unread count in the layout + `/Notifications` page
- [x] 2.2 Announcement composer on the course edit page
- [x] 2.3 Raise notifications from Web call sites (lesson published, quiz score, certificate, application outcome, announcement)

## 3. Optional Email

- [x] 3.1 Add `IEmailSender` abstraction with a no-op default and SMTP implementation behind `Email:Enabled`

## 4. Migration & Verification

- [x] 4.1 Create EF Core migration
- [x] 4.2 Run `dotnet build` and start the app
- [x] 4.3 Verify event → notification, mark-read, unread badge, announcements reach enrolled students, non-owner denied
