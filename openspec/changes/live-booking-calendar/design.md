# Live Booking and Calendar — Design

## Context

Enrollment authorizes course access but does not reserve a limited live-session seat. Concurrent bookings must not exceed capacity.

## Goals

- Support optional booking windows, capacity, cancellation, and FIFO waitlists.
- Provide learner and instructor calendar views.
- Publish secure, revocable calendar feeds.

## Non-Goals

- Room/equipment scheduling or external calendar write-back.
- Payment specifically for a session.

## Decisions

### D1: Transactional seat allocation

Persist one `LiveBooking` per learner/session. Allocate seats atomically under concurrency; overflow becomes ordered waitlist entries.

### D2: Deterministic promotion

Cancellation promotes the earliest eligible waitlisted learner and emits a notification. Promotion is idempotent.

### D3: Tokenized iCalendar feeds

Calendar feed URLs contain revocable random tokens stored as hashes. Feeds contain minimum necessary details and no stream keys.

## Risks / Trade-offs

- Abandoned reservations waste capacity; automatic expiry can be added using the existing scheduler.
- Calendar feeds can be shared; revocation and minimal content reduce exposure.

## Migration Plan

Add booking, waitlist-order, and feed-token tables. Existing sessions default to booking disabled.
