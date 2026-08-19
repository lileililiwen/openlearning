# Study Duration — Tasks

## 1. Data & Service

- [x] 1.1 Add `StudySession` entity + config in the Progress module
- [x] 1.2 Extend `ProgressService`: start/end session, heartbeat accumulation, daily/lesson/course/student duration queries
- [x] 1.3 Register assembly scanning

## 2. UI

- [x] 2.1 Lesson `View` session start/heartbeat/end JS
- [x] 2.2 Student: duration shown on lesson page and study report (via study-tools)
- [x] 2.3 Instructor: per-student study duration on roster/student view

## 3. Migration & Verification

- [x] 3.1 Create EF Core migration
- [x] 3.2 Build, start app, verify: session accumulates time, heartbeat gap excludes idle, per-day totals correct, roster shows duration, abuse cap applied
