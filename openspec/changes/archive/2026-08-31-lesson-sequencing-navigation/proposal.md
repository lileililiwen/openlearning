# Proposal: Lesson Sequencing & Navigation

## Problem

Inside a lesson (`Pages/Courses/Lessons/View.cshtml`) the learner is trapped. There is **no
next/previous lesson navigation** anywhere in the repo (grep for next/prev lesson = 0 hits), and
"Mark as completed" (`View.cshtml:119`) posts back to the same page without advancing. The right
sidebar (`View.cshtml:155`) lists **only the current module's lessons**, so the learner has no
bird's-eye view of the whole course and no way to move forward except climbing back to
`Details.cshtml`. Lessons and their assessments are disconnected: a quiz is reachable only from
`Details.cshtml:224`. The dashboard "assignments due" links to `/MyCourses` instead of the
assignment (`Dashboard/Index.cshtml:33`), and the study calendar has no month navigation
(`Study/Index.cshtml` renders `Model.Month` with no prev/next control).

This breaks the core learning *loop*: complete a lesson → move to the next → feel progress. Good
systems weave content and assessment into one navigable sequence (Canvas Modules, Open edX
subsection prev/next, Kolibri's "next" donut advance).

## Change

Add course-wide lesson sequencing and a full curriculum sidebar with completion state, plus
next/prev and "complete & next" affordances, lesson→assessment links, corrected dashboard deep
links, and study-calendar month navigation.

## Non-goals

- Reordering/gating lessons (authored order is preserved; sequencing is read-only navigation).
- Resume target storage (see `learner-resume-continuity`).
- Visual redesign of administrative pages.

## Impact

- New: `ILessonNavigator` / ordered-lesson projection (in `CourseManagement` or a shared
  learner-read service) computing `PrevLessonId`, `NextLessonId`, and an ordered course outline
  with per-lesson completion.
- New shared partial `Pages/Shared/_CurriculumSidebar.cshtml` replacing the current
  `ModuleLessons` block (`View.cshtml:153-168`).
- Updated: `View.cshtml` (sidebar + next/prev + complete-and-next + assessment link),
  `Dashboard/Index.cshtml:33`, `Study/Index.cshtml` (month nav).
- ADDED Requirements only; safe to validate alongside existing specs.
