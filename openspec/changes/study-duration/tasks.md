# Study Duration — Tasks

## 1. Data & Service

- [ ] 1.1 Add `StudySession` entity + config in the Progress module
- [ ] 1.2 Extend `ProgressService`: start/end session, heartbeat accumulation, daily/lesson/course/student duration queries
- [ ] 1.3 Register assembly scanning

## 2. UI

- [ ] 2.1 Lesson `View` session start/heartbeat/end JS
- [ ] 2.2 Student: duration shown on lesson page and study report (via study-tools)
- [ ] 2.3 Instructor: per-student study duration on roster/student view

## 3. Migration & Verification

- [ ] 3.1 Create EF Core migration
- [ ] 3.2 Build, start app, verify: session accumulates time, heartbeat gap excludes idle, per-day totals correct, roster shows duration, abuse cap applied
