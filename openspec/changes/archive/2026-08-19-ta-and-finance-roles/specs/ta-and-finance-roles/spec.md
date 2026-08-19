## ADDED Requirements

### Requirement: Two additional roles exist

The system SHALL define the `Finance` and `TeachingAssistant` roles and SHALL add policies `RequireFinance`, `RequireTeachingAssistant`, and `RequireFinanceOrAdmin` that gate the appropriate surfaces.

#### Scenario: Roles seeded

- **WHEN** the application starts with an empty database
- **THEN** `Finance` and `TeachingAssistant` are seeded alongside `Admin`, `Instructor`, `Student`

#### Scenario: Policies recognised

- **WHEN** an action is annotated `[Authorize(Policy = Policies.RequireFinance)]`
- **THEN** only users in `Finance` (or in roles explicitly mapped by `RequireFinanceOrAdmin`) can access it

### Requirement: Finance role owns financial surfaces

The system SHALL restrict order list, refund review, reconciliation, withdrawal review, and invoice management surfaces to `Finance` and `Admin` users.

#### Scenario: Finance user accesses refund review

- **WHEN** a user in `Finance` opens `/Admin/Refunds`
- **THEN** access is granted

#### Scenario: TA is denied refund review

- **WHEN** a user in `TeachingAssistant` opens `/Admin/Refunds`
- **THEN** access is denied with a 403/redirect

#### Scenario: Admin still has access

- **WHEN** a user in `Admin` opens `/Admin/Refunds`
- **THEN** access is granted

### Requirement: TeachingAssistant role is scoped to assigned classes

The system SHALL restrict TA-only pages to TAs assigned to the relevant class group; TAs SHALL NOT edit the course itself, the modules, or the lessons.

#### Scenario: TA views assigned class roster

- **WHEN** a TA opens `/TA/{classId}/Roster` for a class they are assigned to
- **THEN** the roster is shown

#### Scenario: TA denied unassigned class

- **WHEN** a TA opens `/TA/{classId}/Roster` for a class they are NOT assigned to
- **THEN** access is denied

#### Scenario: TA denied course edit

- **WHEN** a TA opens `/Courses/{id}/Edit`
- **THEN** access is denied regardless of class assignment

#### Scenario: TA cannot publish a course

- **WHEN** a TA calls the publish endpoint
- **THEN** the request is denied with a 403/redirect

### Requirement: Admin can manage the new roles

The system SHALL allow an Admin to add or remove the `Finance` and `TeachingAssistant` roles for any user on the existing user detail page, taking effect immediately.

#### Scenario: Promote to Finance

- **WHEN** an Admin assigns the `Finance` role to a user
- **THEN** the user can access finance-only pages immediately

#### Scenario: Revoke TA

- **WHEN** an Admin removes the `TeachingAssistant` role from a user
- **THEN** the user loses TA-only access immediately

#### Scenario: Suspension applies to all roles

- **WHEN** a user holding multiple roles is suspended
- **THEN** access is blocked for every role the user holds