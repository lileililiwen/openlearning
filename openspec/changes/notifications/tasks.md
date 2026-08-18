# Notifications — Tasks

## 1. Module Setup

- [ ] 1.1 Create `src/OpenLearning.Notifications` class library and add it to the solution
- [ ] 1.2 Add project references (Auth, CourseManagement, Enrollment, EF Core)
- [ ] 1.3 Add `Notification` + `CourseAnnouncement` entities + configs
- [ ] 1.4 Implement `NotificationService` (create, recent, mark read, unread count) and `AnnouncementService` (post owner-only, list)
- [ ] 1.5 Register assembly scanning in `ApplicationDbContext` and `AddNotificationsModule` in `Program.cs`

## 2. UI

- [ ] 2.1 Notifications bell with unread count in the layout + `/Notifications` page
- [ ] 2.2 Announcement composer on the course edit page
- [ ] 2.3 Raise notifications from Web call sites (lesson published, quiz score, certificate, application outcome, announcement)

## 3. Optional Email

- [ ] 3.1 Add `IEmailSender` abstraction with a no-op default and SMTP implementation behind `Email:Enabled`

## 4. Migration & Verification

- [ ] 4.1 Create EF Core migration
- [ ] 4.2 Run `dotnet build` and start the app
- [ ] 4.3 Verify event → notification, mark-read, unread badge, announcements reach enrolled students, non-owner denied
