# lesson-sequencing-navigation Specification

## Purpose

The lesson-sequencing-navigation specification covers lessons are sequenced across the whole course and the related scenarios that govern this capability across the platform.
## Requirements
### Requirement: Lessons are sequenced across the whole course

The learner shell SHALL present lessons in course-wide authored order spanning all modules, so a learner can move forward without returning to the course page.

#### Scenario: Learner navigates from one lesson to the next

- **WHEN** a learner views a lesson that is not the last in the course
- **THEN** a "Next lesson" control is available that opens the next lesson in authored order, including the first lesson of the following module

#### Scenario: Learner is on the last lesson

- **WHEN** a learner views the final lesson of the course
- **THEN** no "Next lesson" control is shown

### Requirement: Completion advances the learner

Marking a lesson complete SHALL offer a path to continue to the next lesson without a full return to the course page.

#### Scenario: Learner completes and continues

- **WHEN** a learner uses "Complete & Next" on a lesson that has a following lesson
- **THEN** the current lesson is marked complete and they are taken to the next lesson

### Requirement: Curriculum is visible as a single outline

The lesson view SHALL show a course-wide curriculum sidebar listing every module and lesson with completion status and the current location.

#### Scenario: Learner views the outline

- **WHEN** a learner opens any lesson
- **THEN** the sidebar lists all modules and lessons in order, marks completed lessons, and highlights the current lesson

### Requirement: Lessons link to their assessments

When a lesson's course or module has an associated quiz or assignment, the lesson view SHALL surface a link to it.

#### Scenario: Lesson with an associated quiz

- **WHEN** a learner views a lesson whose course provides a quiz
- **THEN** a link to that quiz/assignment is shown on the lesson page

### Requirement: Dashboard and calendar links are precise

Learner dashboard "due" items and the study calendar SHALL deep-link to the specific item and support moving between months.

#### Scenario: Learner opens a due assignment

- **WHEN** a learner clicks a "due assignment" item on the dashboard
- **THEN** they are taken to that assignment, not a generic course list

#### Scenario: Learner changes calendar month

- **WHEN** a learner uses the calendar's previous/next month control
- **THEN** the calendar renders the selected month

