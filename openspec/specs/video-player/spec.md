# video-player Specification

## Purpose
TBD - created by archiving change video-player. Update Purpose after archive.
## Requirements
### Requirement: Instructor can attach a video to a lesson

The system SHALL allow an Instructor to set a video URL (and optional poster/subtitle URLs) on a lesson, causing the lesson to be delivered through a video player.

#### Scenario: Attach video
- **WHEN** an Instructor saves a lesson with a video URL
- **THEN** enrolled students see the video player when opening the lesson

#### Scenario: No video
- **WHEN** a lesson has no video URL
- **THEN** the existing text/SCORM rendering is used

### Requirement: Student can control playback

The system SHALL provide playback speed control, resolution selection when multiple sources exist, subtitle display when a track is present, and resume from the last position.

#### Scenario: Playback controls
- **WHEN** a Student plays a video lesson
- **THEN** they can change speed, switch resolution if available, and see subtitles if provided

#### Scenario: Resume playback
- **WHEN** a Student reopens a video lesson
- **THEN** playback resumes from the last saved position

### Requirement: Student can take notes and post bullet comments

The system SHALL let an enrolled Student save lesson notes and post bullet comments (danmu) that replay for others.

#### Scenario: Lesson notes
- **WHEN** a Student saves a note on a video lesson
- **THEN** the note is stored with the lesson and shown when reopened

#### Scenario: Bullet comments
- **WHEN** a Student posts a danmu message on a video lesson
- **THEN** the message overlays the player and is shown to other viewers

### Requirement: Playback protections are applied

The system SHALL apply anti-scrubbing and anti-recording protections to non-preview lessons for enrolled students.

#### Scenario: Seek restriction
- **WHEN** playback protection is enabled and a Student attempts to seek
- **THEN** the position is locked back to the original point

#### Scenario: Recording deterrents
- **WHEN** a Student plays a protected lesson
- **THEN** download/right-click/context-menu and picture-in-picture controls are disabled in the player

