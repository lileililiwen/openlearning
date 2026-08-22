# Exam Integrity — Design

## Context

Browser events are incomplete and spoofable. They are evidence for review, not proof of misconduct. Accessibility accommodations must override default timing and event thresholds explicitly.

## Goals

- Record tamper-evident, bounded integrity evidence.
- Apply versioned risk policies consistently.
- Support authorized review, appeals, and accommodations.

## Non-Goals

- Facial recognition, gaze tracking, room scans, or automatic misconduct verdicts.
- Preventing all cheating on an untrusted device.

## Decisions

### D1: Server-authoritative session

Issue a signed attempt-session nonce and monotonically sequence heartbeat/evidence batches. Server time controls availability and duration.

### D2: Evidence not verdict

Record allowlisted events and calculate an explainable risk level. Only an authorized reviewer can record a disposition; high risk does not alter a grade automatically.

### D3: Versioned accommodations

Snapshot extra time, allowed breaks, and relaxed event thresholds onto an attempt without exposing disability details.

### D4: Data minimization

Retain evidence for a configured period, encrypt sensitive payloads, audit access, and collect no audio/video/biometrics.

## Risks / Trade-offs

- Client evidence can be forged or absent; reports state this limitation.
- More controls can disadvantage unstable networks; reconnect/grace semantics and manual review mitigate this.

## Migration Plan

Add policy, accommodation snapshot, evidence, incident, disposition, and appeal tables. Existing attempts have no retrospective integrity evidence.
