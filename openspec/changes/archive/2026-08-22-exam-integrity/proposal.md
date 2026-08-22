## Why

Exams detect page switching and timeouts, but lack server-verifiable session evidence, risk policy, incident review, and accommodations. Facial recognition is deliberately excluded because it adds biometric risk without being necessary for a defensible first integrity layer.

## What Changes

- Add signed exam sessions, heartbeat/evidence events, copy/paste and connectivity signals.
- Add configurable risk rules, accommodations, incident review, and appeal records.
- Preserve answers during disconnects and prevent client signals from directly imposing final penalties.

## Capabilities

### New Capabilities
- `exam-integrity`: integrity sessions, evidence, risk review, accommodations, and appeals.

### Modified Capabilities
- None.

## Impact

- Extend the Exams module and operator/instructor review UI.
- No camera, microphone, biometric, or third-party proctoring collection in this change.
