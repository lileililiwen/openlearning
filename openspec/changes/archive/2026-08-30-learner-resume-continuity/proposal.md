# Proposal: Learner Resume & Continuity

## Problem

The platform's primary learner action, **"Continue learning"**, does not continue anything.
In `src/OpenLearning.Web/Pages/Courses/Details.cshtml:416` the button links to
`/Courses/Details` with the *same* `course.Id`, so it reloads the course page the learner
is already on. There is no stored "last position" anywhere, so a learner who leaves a course
and returns must manually re-find their place every time. This is the single most damaging
usability defect in the learner journey: it breaks the *resume* loop that keeps motivation
alive (the "goal gradient" — progress feels closer as you return to where you left off).

Open edX ("Resume course" deep-links to the last viewed position) and Moodle ("Resume where
you left off" button) both treat resume-to-last-position as a core feature. OpenLearning has
the data to do this (lesson views, progress completions) but never records or uses a resume target.

## Change

Introduce a per-enrollment **resume target** (last-viewed lesson) and make every "Continue
learning" / "Resume" entry point navigate to it instead of the course details page. When no
progress exists, fall back to the first lesson.

## Non-goals

- Changing the course authoring model or lesson ordering (see `lesson-sequencing-navigation`).
- Building a full learning-history timeline.
- Automatic "resume on login" (out of scope; this change only fixes explicit CTAs).

## Impact

- New/updated: `OpenLearning.Enrollment` (or `OpenLearning.Progress`) stores `ResumeLessonId`;
  new `IResumeService.GetResumeTargetAsync(userId, courseId)`; updated `Details.cshtml:416`,
  the Dashboard continue/resume card, and the lesson `View.cshtml` GET handler (records the view).
- Relations: complements the unimplemented `learner-experience-quality` proposal and feeds
  `lesson-sequencing-navigation` (next/prev) and `ui-state-and-feedback-contract`.
- Adds ADDED Requirements only (no modification of existing base specs), so it is safe to validate.
