# HANDOFF.md

> **Branch:** `main` | **Last updated:** 2026-08-31
>
> Most recent: **lesson-sequencing-navigation** — COMPLETE.
> Previous: `responsive-design-system`, `learner-accessibility-compliance`, `localization-foundation`, `extensibility-integration-contracts`, `ui-state-and-feedback-contract`, `learner-resume-continuity`, `outcomes-mastery-operations` — all ARCHIVED.

## Latest: Lesson Sequencing & Navigation (lesson-sequencing-navigation)

**Status:** COMPLETE (`openspec/changes/lesson-sequencing-navigation`)

### What was done
- **Sequencing service:** `LessonNavigatorService` with `GetOrderedLessonsAsync` (course-wide module→lesson outline with completion state) and `GetNavigatorAsync` (prev/next across module boundaries). 7 unit tests.
- **Curriculum sidebar:** `_CurriculumSidebar.cshtml` partial replacing the current-module-only list; shows all modules/lessons, completion checkmarks, current highlight, links.
- **Next/Prev controls:** Server-rendered `<a>` links in lesson footer, hidden when null. Cross-module navigation works.
- **Complete & Next:** `OnPostCompleteAndNextAsync` handler marks complete then redirects to next lesson (or course details at end). Existing `Complete`/`Uncomplete` handlers unchanged.
- **Assessment links:** Lesson view shows quiz or assignment link when present for the course.
- **Dashboard deep link:** "Due assignments" alert links to `/Courses/Assignments/Index` with course ID instead of generic `/MyCourses`.
- **Calendar month nav:** `Study/Index` accepts `month`/`year` query params with prev/next controls; defaults to current month.

### Files changed
- `src/OpenLearning.CourseManagement/Services/LessonNavigatorService.cs` (new)
- `src/OpenLearning.CourseManagement/CourseManagementModuleExtensions.cs`
- `src/OpenLearning.Web/Pages/Shared/_CurriculumSidebar.cshtml` (new)
- `src/OpenLearning.Web/Pages/Courses/Lessons/View.cshtml.cs`
- `src/OpenLearning.Web/Pages/Courses/Lessons/View.cshtml`
- `src/OpenLearning.Web/Pages/Dashboard/Index.cshtml.cs`
- `src/OpenLearning.Web/Pages/Dashboard/Index.cshtml`
- `src/OpenLearning.Web/Pages/Study/Index.cshtml.cs`
- `src/OpenLearning.Web/Pages/Study/Index.cshtml`
- `tests/OpenLearning.UnitTests/LessonNavigatorServiceTests.cs` (new)

### Verification
- `dotnet build` → 0 warnings, 0 errors.
- `dotnet format --verify-no-changes` → clean.
- 495 unit tests pass, 5 architecture tests pass.

---

## Previously archived

| Change | Key deliverables |
|--------|-----------------|
| **responsive-design-system** | CSS custom properties, contrast fix, responsive tables, CDN SRI, breakpoint tokens. |
| **learner-accessibility-compliance** | `<html lang>` dynamic, progress bar ARIA, skip-to-content, sidebar landmarks, mobile menu a11y, label sweep, 7 a11y tests. |
| **localization-foundation** | ASP.NET Core localization module, `<html lang>` dynamic, `.resx` resources for SharedResources + 4 pages (en/zh), 7 localization tests. |
| **extensibility-integration-contracts** | `IIntegrationAdapter` contract, Webhook/Lti/Scorm adapters, `IntegrationOperationsService`, EF migration, admin UI, 20 tests. |
| **ui-state-and-feedback-contract** | Shared loading/empty/error/toast partials, `TempData` toast, `ConfirmTagHelper`, input preservation, progressive enhancement. |
| **learner-resume-continuity** | `IResumeService` + `ResumeService` reusing `LessonAccess`, 6 service + 5 architecture tests. |
| **outcomes-mastery-operations** | `OpenLearning.Outcomes` module, EF migration, instructor outcomes UI, 19 tests. |

---

## Next steps

1. **Remaining active specs:** `learner-experience-quality`, `offline-sync-resilience`, `platform-quality-release-gates`.
2. Before pushing: run tests, `dotnet format`, apply any pending EF migrations.
