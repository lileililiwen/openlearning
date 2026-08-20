# resource-center Specification

## Purpose
TBD - created by archiving change resource-center. Update Purpose after archive.
## Requirements
### Requirement: Resource library listing

The system SHALL provide a resource library at `/Resources` listing the
current user's uploads (and every upload, for Admins), filterable by type
(Image/Video/Document) and searchable by original file name, with URL copy and
media preview.

#### Scenario: User sees own uploads

- **WHEN** an Instructor opens `/Resources`
- **THEN** every file they uploaded is listed with name, type, size, date, and a copy-URL action

#### Scenario: Admin sees all uploads

- **WHEN** an Admin opens `/Resources`
- **THEN** every file from every user is listed, with the owner shown

#### Scenario: Filter and search

- **WHEN** the user filters by `Video` or types a search term
- **THEN** only matching resources are shown

### Requirement: Upload to the resource center

The system SHALL accept image, video, and document uploads through the
resource center and expose a server-generated URL for each.

#### Scenario: Upload an image

- **WHEN** an Instructor uploads a `.png` on `/Resources`
- **THEN** the image is stored, appears in the library, and a
  `/files/image/...` URL is available to copy

#### Scenario: Upload a video

- **WHEN** an Instructor uploads an `.mp4`
- **THEN** the video is stored and the library shows its rendition status
  (pending/ready) once transcoding is known

#### Scenario: Invalid file rejected

- **WHEN** the upload is a `.exe`
- **THEN** the upload is rejected with the purpose's extension/size error

### Requirement: Delete a resource

The system SHALL allow the owner (or any Admin) to delete a resource,
removing the blob and any video renditions.

#### Scenario: Owner deletes

- **WHEN** an Instructor deletes their own resource
- **THEN** the file and its renditions are removed and it disappears from the library

#### Scenario: Non-owner denied

- **WHEN** an Instructor attempts to delete another user's non-shared resource
- **THEN** access is denied

### Requirement: Admin sharing

The system SHALL let an Admin mark a resource as shared, making it visible and
reusable by every authenticated user.

#### Scenario: Admin shares

- **WHEN** an Admin toggles shared on a resource
- **THEN** the resource appears in every user's library under a shared badge

#### Scenario: Shared resources are never private

- **WHEN** an Admin tries to share a resource of a private purpose (e.g.
  `Answer`)
- **THEN** the action is rejected

### Requirement: Resource picker

The system SHALL provide a "choose from resources" picker that fills an
existing URL input with the selected resource's URL, available on lesson media
fields, the profile avatar field, and the admin banner image field.

#### Scenario: Pick a lesson video

- **WHEN** an Instructor clicks "从资源库选择" next to `VideoUrl` on the
  lesson edit page and selects a video
- **THEN** the input is filled with `/files/video/...`

#### Scenario: Picker filters by field type

- **WHEN** the picker is opened from a poster-image field
- **THEN** only image resources are offered

#### Scenario: Avatar picker

- **WHEN** a user picks an image for their profile avatar
- **THEN** `AvatarUrl` is set to the chosen `/files/image/...` URL

#### Scenario: Banner picker

- **WHEN** an Admin picks an image for a banner
- **THEN** `Banner.ImageUrl` is set to the chosen URL

### Requirement: Visibility and read ACL

The system SHALL restrict resource listing and serving to the visibility model:
own resources to the owner, shared resources to any authenticated user, all
resources to Admins; the existing `/files/{key}` proxy ACL continues to apply.

#### Scenario: Shared resource readable by any user

- **WHEN** a Student opens the URL of a shared resource
- **THEN** the file streams (shared resources are never private)

#### Scenario: Private resource not listed to others

- **WHEN** an Instructor who does not own a non-shared resource opens the
  resource center
- **THEN** that resource is not listed

