## Why

Assignments support only instructor grading. Mature LMSs (Moodle Workshop, Open edX ORA2) treat structured peer assessment as a core pedagogical capability — it scales feedback, builds evaluation skills, and is a top request for cohort-based courses. Our research against comparable systems identified its absence as the largest single pedagogical gap.

## What Changes

- Add peer review configuration on assignments: number of reviews per student, anonymity mode, rubric questions, and phase dates (submission → review → closed).
- Add automatic, auditable reviewer allocation so every reviewed submission receives the configured number of assessments and no student reviews their own work.
- Add student assessment submission against the rubric, gated by enrollment and phase.
- Add configurable score combination (instructor-only, peer-average, weighted mix) with instructor override of any final result.
- Add release controls: peer results stay hidden until the Instructor publishes them.

## Capabilities

### New Capabilities
- `peer-assessment`: peer review configuration, reviewer allocation, rubric-based peer assessments, score combination, and release/anonymity controls.

### Modified Capabilities
- None.

## Impact

- New `OpenLearning.PeerAssessment` domain module consuming existing assignment submissions (`OpenLearning.Assignments`) and enrollment data.
- New Razor Pages under the Assignments feature area for configuration, allocation, assessment, and results.
- New EF Core migration; no changes to existing grading records — final grades remain owned by the assignments module until an override is applied there.
