# Live Booking and Calendar — Tasks

## 1. Domain

- [x] 1.1 Add booking, waitlist, and calendar-token models/configurations to the Live module
- [x] 1.2 Implement booking-window validation and concurrency-safe reserve/cancel/promote services
- [x] 1.3 Implement scoped calendar queries and revocable iCalendar feeds
- [x] 1.4 Add an EF Core migration and notification events/templates

## 2. UI

- [x] 2.1 Extend live-session management with booking window, capacity, roster, and waitlist controls
- [x] 2.2 Add Student reserve/cancel state and personal calendar views
- [x] 2.3 Add feed-create, rotate, and revoke controls

## 3. Verification

- [x] 3.1 Test concurrent final-seat booking, FIFO promotion, eligibility loss, time zones, and feed secrecy
- [x] 3.2 Build cleanly and exercise every scenario over HTTP
