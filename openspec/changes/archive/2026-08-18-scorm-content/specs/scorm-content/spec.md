## ADDED Requirements

### Requirement: Instructor can attach a SCORM 1.2 package to a lesson

The system SHALL allow the Instructor who owns a lesson's course to upload a SCORM 1.2 package (zip) for that lesson and remove it. The package SHALL be unpacked and its manifest parsed to determine the launch entry point.

#### Scenario: Owner uploads a package
- **WHEN** the owning Instructor uploads a valid SCORM 1.2 zip for a lesson
- **THEN** the package is stored, the manifest is parsed, and the lesson gains a launch entry point

#### Scenario: Non-owner upload is denied
- **WHEN** an Instructor who does not own the course attempts to upload a package
- **THEN** the system SHALL deny access

### Requirement: Student can launch the SCORM content

The system SHALL allow an enrolled Student to launch the SCORM package attached to a lesson, running the SCO with a SCORM 1.2 runtime API.

#### Scenario: Launch package
- **WHEN** an enrolled Student opens the lesson's SCORM launch page
- **THEN** the SCO is loaded in a frame and the SCORM 1.2 API is available to it

#### Scenario: Non-enrolled student cannot launch
- **WHEN** a Student who is not enrolled attempts to launch the package
- **THEN** the system SHALL deny the request

### Requirement: SCORM runtime state is persisted per enrollment

The system SHALL persist the SCO's runtime state (`suspend_data`, `lesson_location`, `lesson_status`, `score.raw`, `session_time`) per enrollment, so state survives a page reload.

#### Scenario: State commit
- **WHEN** the SCO commits its state through the runtime API
- **THEN** the state is stored for that enrollment and package

#### Scenario: State restored on relaunch
- **WHEN** an enrolled Student relaunches the package
- **THEN** the previously committed state is returned through the runtime API

### Requirement: Completing SCORM content completes the lesson

The system SHALL mark the lesson complete for the enrolled Student when the SCO reports a completion status of `completed` or `passed`.

#### Scenario: SCO reports completed
- **WHEN** the SCO commits `lesson_status` = `completed`
- **THEN** the lesson is marked complete for that Student and the course progress is updated
