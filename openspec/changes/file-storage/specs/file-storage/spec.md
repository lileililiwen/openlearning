## ADDED Requirements

### Requirement: Files are stored with metadata

The system SHALL store uploaded files through a storage layer that records metadata and serves them by key.

#### Scenario: Upload
- **WHEN** a user uploads a file under an allowed size and type
- **THEN** the file is stored, its metadata recorded, and a key is returned

#### Scenario: Serve by key
- **WHEN** a file URL is requested
- **THEN** the file is streamed with its content type

#### Scenario: Reject invalid
- **WHEN** a file exceeds the size limit or has a disallowed type
- **THEN** the upload is rejected

### Requirement: Private files are access-controlled

The system SHALL restrict access to files whose purpose is private.

#### Scenario: Private file denied
- **WHEN** a user who is not the owner (or authorized) requests a private file
- **THEN** access is denied

### Requirement: Videos are transcoded to renditions

The system SHALL process uploaded videos into rendition URLs for the player.

#### Scenario: Transcode
- **WHEN** a video is uploaded
- **THEN** renditions are generated and reported ready, or the upload is marked failed

#### Scenario: Ready renditions
- **WHEN** renditions are ready
- **THEN** the player can offer multiple resolutions
