## ADDED Requirements

### Requirement: Course owner can create class groups

The system SHALL allow the owner of a course to create one or more `ClassGroup` rows under it, each with a name, a start, an end, an optional capacity, and a status.

#### Scenario: Create class

- **WHEN** the owning Instructor creates a class group with a future start and end
- **THEN** the class is created with `Status = Upcoming` and appears on the course's class list

#### Scenario: Non-owner cannot create

- **WHEN** an Instructor who does not own the course attempts to create a class
- **THEN** the request is denied

#### Scenario: Past start auto-opens

- **WHEN** `UtcNow` reaches the class's `StartsAt` while the status is still `Upcoming`
- **THEN** the status transitions to `Open` (driven by a small scheduled check or on first read)

### Requirement: Class assignments bind TAs

The system SHALL allow the course owner to assign TAs (and additional Instructors / Observers) to a class; an assignment is unique per `(ClassGroupId, UserId, Role)`.

#### Scenario: Assign TA

- **WHEN** the course owner assigns a user with `TeachingAssistant` role to a class
- **THEN** the user appears in the class roster and the TA dashboard

#### Scenario: Re-assign / revoke

- **WHEN** the course owner revokes a TA assignment
- **THEN** the user loses access to the class's TA-only pages on the next request

#### Scenario: Duplicate assignment is rejected

- **WHEN** the owner tries to assign the same TA twice
- **THEN** the request is rejected with a validation error

### Requirement: Enrollment can attach to a class

The system SHALL allow an `Enrollment` to attach to a `ClassGroupId` (nullable). Existing enrollments without a class group continue to work.

#### Scenario: Enroll into a class

- **WHEN** an admin enrolls a student into a specific class group
- **THEN** `Enrollment.ClassGroupId` is set and the student appears in the class roster

#### Scenario: Enroll without a class

- **WHEN** an existing direct-enrollment flow is used
- **THEN** `Enrollment.ClassGroupId = null` and the student appears only in the course roster

### Requirement: TA views only assigned classes

The system SHALL restrict a TA's roster, progress, and announcements surfaces to the classes they are assigned to.

#### Scenario: TA sees assigned class

- **WHEN** a TA opens `/TA`
- **THEN** only their assigned classes are listed

#### Scenario: TA denied unassigned class

- **WHEN** a TA opens `/TA/Class/{id}` for a class they are not assigned to
- **THEN** access is denied

#### Scenario: TA cannot create classes

- **WHEN** a TA attempts to create a class under any course
- **THEN** the request is denied regardless of any assignment

### Requirement: Class-scoped Q&A

The system SHALL allow a class member (enrolled student, assigned TA, or course owner) to post in the class Q&A; non-class members SHALL NOT see class-scoped posts.

#### Scenario: Class member posts

- **WHEN** an enrolled student of a class posts in the class Q&A
- **THEN** the post is visible to class members only

#### Scenario: Non-member cannot see class Q&A

- **WHEN** an enrolled student of the course but NOT in the class tries to view the class Q&A
- **THEN** the page shows only course-wide Q&A, not class-scoped items

#### Scenario: Course-wide Q&A unchanged

- **WHEN** a member posts without a class tag
- **THEN** the post follows the existing course-wide visibility rules

### Requirement: Class-scoped announcements

The system SHALL allow the course owner or an assigned TA to post an announcement scoped to a class; only members of that class receive the notification.

#### Scenario: Class announcement reaches class only

- **WHEN** an Instructor posts a class-scoped announcement
- **THEN** notifications are sent to every enrolled student of that class

#### Scenario: TA can announce

- **WHEN** an assigned TA posts an announcement to their class
- **THEN** only that class's students receive the notification

#### Scenario: Course-wide announcement unchanged

- **WHEN** an announcement is posted without a class tag
- **THEN** it follows the existing `notifications` rules

### Requirement: Class roster and progress report

The system SHALL provide a class roster with each student's progress, last activity, and exam/assignment scores; the data SHALL be exportable as CSV.

#### Scenario: View class roster

- **WHEN** the course owner or an assigned TA opens a class's roster
- **THEN** the roster lists students with progress %, last activity, and outstanding assignments

#### Scenario: Export CSV

- **WHEN** the course owner or TA clicks Export on the class roster
- **THEN** a CSV with the same columns is downloaded

### Requirement: Class lifecycle

The system SHALL transition a class's status as time passes and SHALL prevent new enrollments when the class is `Closed`.

#### Scenario: Open to new enrollments

- **WHEN** a class's status is `Upcoming` or `Open` and capacity is not full
- **THEN** new enrollments are accepted

#### Scenario: Close the class

- **WHEN** the course owner marks a class as `Closed`
- **THEN** no new enrollments are accepted, and the class-scoped Q&A / announcements become read-only for students (TA/Instructor can still post)

#### Scenario: Auto-close on end date

- **WHEN** `UtcNow > ClassGroup.EndsAt`
- **THEN** the class becomes read-only for students; the owner can manually reopen it