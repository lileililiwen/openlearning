# Agents.md

> This document is the contract for AI agents (and humans) working on
> the OpenLearning codebase. It is **normative**: every principle here
> MUST be followed unless explicitly overridden by a written decision
> in an OpenSpec change.

---

## 1. What is OpenLearning?

OpenLearning is an open-source, MIT-licensed online learning management
system (LMS) built with C# / ASP.NET Core 8 (Razor Pages) and
PostgreSQL via EF Core (Npgsql). It covers the core learning
workflows: authentication with roles, course delivery, enrollment,
progress tracking, quizzes, paid courses, SCORM content, and course
chat.

**Status:** MVP shipped and archived (`initial-lms-mvp`) plus five
follow-on capabilities (assessments, ecommerce, scorm-content,
live-chat). Nine gap changes are spec'd and pending implementation —
see [§7 Roadmap](#7-current-state--roadmap).

**Actors:** the platform has three sides — **Student** (learner),
**Teacher** (course owner/instructor), and **Platform operator**
(Admin). Every feature should be considered from all three sides.

---

## 2. Architecture — Modular Monolith

OpenLearning is a .NET 8 solution organized as a **modular monolith**:
**one class library per business domain**. A new domain/capability
lives in a new package and MUST NOT require edits across unrelated
code.

```
src/
├── OpenLearning.Auth/              # Identity user, roles, policies, AccountService
├── OpenLearning.CourseManagement/  # Course / Module / Lesson aggregate + services
├── OpenLearning.Enrollment/        # enrollment + enroll/withdraw services
├── OpenLearning.Progress/          # lesson completions + progress calculation
├── OpenLearning.Assessments/       # quizzes, questions, attempts, scoring
├── OpenLearning.Ecommerce/         # course pricing, orders, checkout
├── OpenLearning.Scorm/             # SCORM 1.2 packages + runtime
├── OpenLearning.Chat/              # SignalR course chat
├── OpenLearning.Jobs/              # persistent cron job registry + scheduler + runs
├── OpenLearning.Data/              # central ApplicationDbContext, migrations, seeding
└── OpenLearning.Web/               # Razor Pages UI shell + DI composition root
```

### 2.1 Adding a new domain (the fixed pattern)

1. Create `src/OpenLearning.<Domain>` class library; add to the
   solution; reference the modules it needs (never `OpenLearning.Data`).
2. `Models/` — entities. `Configuration/` — one
   `IEntityTypeConfiguration<T>` per entity. `Services/` — services
   injecting the **base `DbContext`** and using `Set<T>()`.
   `<Domain>ModuleExtensions.cs` — an `AddXxxModule(IServiceCollection)`
   extension.
3. In `OpenLearning.Data/ApplicationDbContext.cs`: add `DbSet`s and
   ONE line `builder.ApplyConfigurationsFromAssembly(typeof(<Entity>).Assembly);`
   (zero other edits).
4. In `OpenLearning.Web/Program.cs`: one line
   `builder.Services.AddXxxModule();`.
5. Create an EF migration:
   `dotnet ef migrations add <Name> --project src/OpenLearning.Data --startup-project src/OpenLearning.Web`

**Dependency rules (enforced by the build):**
- Modules MUST NOT reference `OpenLearning.Data` (the Data project
  references all modules; this keeps the graph acyclic). Services
  depend on the base `Microsoft.EntityFrameworkCore.DbContext`.
- Cross-module navigation collections are avoided; queries go through
  module services.
- `ApplicationUser` carries no navigation collections to
  courses/enrollments.
- Gotcha: the `Enrollment` entity type collides with its own
  namespace. Use `using EnrollmentEntity = OpenLearning.Enrollment.Models.Enrollment;`
  in dependent projects.

---

## 3. Spec-First Development — the Fixed Workflow

**Every change to OpenLearning goes through OpenSpec BEFORE any code is
written.** This is a fixed workflow — there is no other path.

```
propose  →  validate  →  implement (apply)  →  archive  →  spec is source of truth
```

### 3.1 The OpenSpec workflow

| Phase | What happens | Output |
|---|---|---|
| `propose` | `openspec new change <name>` creates the change folder; then author the **four canonical docs** (below) | `openspec/changes/<name>/` |
| `validate` | `openspec status --change <name> --json` shows artifact status; every artifact must be `done` before implementation | pass / fail |
| `apply` | Implement `tasks.md` in order with serious, production-quality code; mark every checkbox complete | code |
| `archive` | `openspec archive <name> -y` | delta folded into `openspec/specs/<cap>/spec.md`; change moved to `openspec/changes/archive/<YYYY-MM-DD>-<name>/` |

### 3.2 The four canonical docs

Every change MUST contain exactly these artifacts:

```
openspec/changes/<name>/
├── proposal.md                 # WHY: motivation, what changes, capabilities, impact
├── specs/<cap>/spec.md         # WHAT: "## ADDED Requirements" with SHALL/MUST +
│                               #   "#### Scenario:" blocks (exactly 4 hashtags)
├── design.md                   # HOW: context, decisions with rationale, risks, migration
└── tasks.md                    # checkbox implementation list, grouped "## N." headings
```

- Use SHALL/MUST for normative requirements; every requirement needs
  at least one scenario with **WHEN / THEN**.
- If an existing capability's behavior changes, include a
  `specs/<existing-cap>/spec.md` with `## MODIFIED Requirements`
  (copy the FULL requirement block from `openspec/specs/<cap>/spec.md`
  and edit it).
- The **source of truth** for any capability is
  `openspec/specs/<cap>/spec.md`. Code that drifts from it is a bug.

### 3.3 Sequential processing rule

When **two or more changes are pending, implement them one at a time,
in the order listed in [§7 Roadmap](#7-current-state--roadmap).** A
change is finished only when it is implemented AND archived AND merged
to `main` via a reviewed pull request (`main` is protected; see
`CONTRIBUTING.md`). Only then may the next change be started. Never
interleave or partially complete multiple changes.

### 3.4 "Serious code" standard

Implementation is expected to be **production-quality, not stubs**:

- Real, working code with correct behavior per the spec scenarios —
  no `TODO`, no placeholder returns, no `NotImplementedException`.
- Follow existing conventions: the module pattern in §2, Razor Pages
  handlers, `TempData` messages, owner checks on every mutating page,
  server-side validation (never trust the client).
- Security care: authorization policies on every restricted page,
  ownership checks (`Forbid()`), antiforgery tokens (automatic with
  Razor Pages forms), no secrets in logs/URLs, safe zip/path handling
  (e.g. path-traversal guards), input length limits.
- Every feature MUST be exercised end-to-end before declaring done
  (see §4.3) — not just compiled.

---

## 4. Build, Run & Verify

### 4.1 Commands

```bash
dotnet build OpenLearning.sln                # MUST finish with 0 warnings / 0 errors
dotnet run --project src/OpenLearning.Web    # app on http://localhost:5096 (migrates + seeds on start)
dotnet ef migrations add <Name> --project src/OpenLearning.Data --startup-project src/OpenLearning.Web
dotnet ef database update --project src/OpenLearning.Data --startup-project src/OpenLearning.Web
```

> Environment note: the .NET SDK lives at `~/.dotnet`; if `dotnet` is
> not on PATH, prefix commands with
> `export PATH="$HOME/.dotnet:$PATH"` and
> `export PATH="$HOME/.dotnet/tools:$PATH"`.

### 4.2 Database

PostgreSQL runs locally (`pg_isready`). Dev DB: `openlearning`, user
`openlearning` (password `openlearning_dev`), superuser `postgres`
(password `postgres`). Connection string is in
`src/OpenLearning.Web/appsettings.json`. Migrations apply
automatically on startup; `DbSeeder` seeds roles + demo users when the
DB is empty.

### 4.3 Verification standard

- Build clean, app starts, migration applies.
- Exercise each spec scenario via HTTP against
  `http://localhost:5096` using curl with a cookie jar
  (`curl -c/-b <jar>`, extract `__RequestVerificationToken` from forms
  before POST). Demo accounts:
  `student@openlearning.dev` / `Student123!`,
  `instructor@openlearning.dev` / `Instructor123!`,
  `admin@openlearning.dev` / `Admin123!`.
- Verify role gating (each role sees/denies correctly), the happy path
  AND the negative scenarios in the spec (duplicates, non-owners,
  drafts, non-enrolled).

---

## 5. Agent Workflow Checklist

When asked to implement a feature or spec:

1. **Read** the change folder: `proposal.md`, `specs/<cap>/spec.md`,
   `design.md`, `tasks.md` — these are authoritative. Also load the
   relevant source-of-truth specs in `openspec/specs/` (the change is
   a *delta* on top of them).
2. **Check the roadmap order** (§7). Implement pending changes one at
   a time in listed order; never jump ahead.
3. **Explore & reuse** *(mandatory)* — before planning or coding,
   search the tree for existing entities, services, pages, and
   patterns that already satisfy the requirement (e.g. an existing
   `IEntityTypeConfiguration`, an existing ownership check, an
   existing page pattern). In `design.md`, name what you will reuse.
   Do not re-implement what already exists.
4. **Plan** by walking `tasks.md` top-to-bottom.
5. **Implement** in the module pattern (§2): entities → configs →
   services → DI registration → migration → UI pages.
6. **Smoke-test** every scenario at the HTTP layer (§4.3) before
   declaring done.
7. **Build** — `dotnet build OpenLearning.sln` MUST be 0 warnings /
   0 errors.
8. **Update** `tasks.md` — every box checked.
9. **Archive** — `openspec archive <name> -y` (folds deltas into
   `openspec/specs/`, moves the change to
   `openspec/changes/archive/`).
10. **Commit & land via PR** — commit with a conventional message (short
    title, blank line, detailed body explaining *why* and *what*), on a
    **feature branch** named after the change, then open a pull request
    that passes the required CI checks and one approving review before
    merging into `main` (see `CONTRIBUTING.md`). The author identity is
    `lileililiwen <lileililiwen@gmail.com>`. `main` is protected: direct
    pushes are rejected.

> **Global-view rule:** if you cannot point at the existing module or
> utility your change depends on, stop and explore before writing
> code. Confidently inventing an API that already exists elsewhere is
> the most expensive failure mode.

---

## 6. Anti-Patterns (do not do these)

- **Don't** write code before the spec change exists and is
  `validate`-ready. Spec-first is a fixed workflow.
- **Don't** implement two changes at once, or skip the roadmap order.
- **Don't** edit files outside your domain module unless the
  composition root needs a one-line change
  (`ApplicationDbContext` scanning line, `Program.cs` module line).
- **Don't** create a circular reference: modules never reference
  `OpenLearning.Data`; services inject the base `DbContext`.
- **Don't** leave stubs, `TODO`s, or unhandled error paths in shipped
  code.
- **Don't** trust client input — enforce ownership/roles server-side
  on every page, including forged POSTs.
- **Don't** skip the negative scenarios (duplicates, non-owners,
  drafts, non-enrolled) when verifying.
- **Don't** commit with a different author identity or a vague commit
  message.
- **Don't** archive a change whose spec scenarios are not verified by
  an HTTP smoke test.

---

## 7. Current State & Roadmap

### 7.1 Shipped & archived (implemented)

Source of truth in `openspec/specs/` — **49 capabilities** across the
core LMS, content, assessments, ecommerce, finance, notifications,
quality, and operations layers. Frozen change copies under
`openspec/changes/archive/2026-08-18-*` and `2026-08-19-*`.

The 10-capability MVP (`user-auth`, `course-management`,
`course-structure`, `enrollment`, `progress-tracking`, `lms-core`,
`assessments`, `ecommerce`, `scorm-content`, `live-chat`) plus the
follow-on capabilities (dashboards, user-management, instructor-onboarding,
teacher-roster, course-discovery, ratings-reviews, certificates,
notifications, platform-analytics, system-config, operations-config,
memberships, account-login-extras, account-settings, study-tools,
study-duration, lesson-preview, video-player, assignments,
finance-admin, instructor-revenue, commerce-extras) are all shipped.

### 7.2 Pending changes (implement in this order — "minor sequence")

The historical sequence (`dashboards`, `user-management`, `teacher-roster`,
`course-discovery`, `ratings-reviews`, `certificates`, `notifications`,
`user-profiles`, `platform-analytics`) is **shipped and archived** under
`openspec/changes/archive/2026-08-18-*` and `2026-08-19-*`. The current
pending backlog is grouped below by layer; within each group follow the
listed order.

#### A. Roles, navigation, lifecycle (3)

| Order | Change | Capabilities | One-line summary |
|---|---|---|---|
| 1 | `navigation-chrome` | navigation-chrome, menu-config (+ MODIFIED lms-core, notifications) | Sidebar + topbar + breadcrumb; admin-managed menu tree |
| 2 | `ta-and-finance-roles` | ta-and-finance-roles (+ MODIFIED user-management, lms-core, finance-admin) | Add TA and Finance roles, decouple from Admin |
| 3 | `class-groups` | class-groups (+ MODIFIED course-management, enrollment, teacher-roster, qa-community, notifications) | Class groups under a course; TA scoping; class Q&A + announcements |

#### B. Domain extensions already in flight (6)

| Order | Change | Capabilities | One-line summary |
|---|---|---|---|
| 4 | `question-types` | question-types (+ MODIFIED assessments) | Single / multiple / true-false / fill-blank / short-answer / file-upload |
| 5 | `question-bank-admin` | question-bank-admin (+ MODIFIED assessments) | Central bank; admin CRUD; instructor import |
| 6 | `exams` | exams (+ MODIFIED assessments) | Formal exams with timer, anti-switch, results, incorrect log |
| 7 | `incorrect-answer-log` | incorrect-answer-log (+ MODIFIED assessments, exams) | Persistent wrong-answer log + practice mode + bookmarks |
| 8 | `qa-community` | qa-community | Course Q&A + class-group posts with replies |
| 9 | `review-followups` | review-followups (+ MODIFIED ratings-reviews) | Threaded comments under a review |
| 10 | `content-review` | content-review (+ MODIFIED course-management, ratings-reviews, qa-community) | Course review workflow; report queue; violation handling |
| 11 | `live-streaming` | live-streaming (+ MODIFIED live-chat, file-storage) | Scheduled live sessions + chat + co-hosting + check-ins + replays |

#### C. Time, finance, billing (5)

| Order | Change | Capabilities | One-line summary |
|---|---|---|---|
| 12 | `job-scheduler` | job-scheduler (+ MODIFIED logging) | Cron substrate with persistent Job / JobRun, idempotency, lock, admin UI |
| 13 | `course-access-period` | course-access-period (+ MODIFIED enrollment, memberships, progress-tracking, certificates, course-management) | `Enrollment.AccessExpiresAt`, manual + scheduled revocation, re-enroll |
| 14 | `scheduled-business-jobs` | scheduled-business-jobs (+ MODIFIED ecommerce, commerce-extras, assignments, exams, study-duration, platform-analytics, logging) | 14 batch jobs (close-unpaid, refund-timeout, expiry, reminders, stats, settlement, coupon deactivation, log archive, IO cleanup) wired to `job-scheduler` |
| 15 | `affiliate-distribution` | affiliate-distribution (+ MODIFIED ecommerce, navigation-chrome) | Distributor role, share links, attribution, commission ledger, payout review |
| 16 | `invoice-management` | invoice-management (+ MODIFIED commerce-extras, finance-admin, ta-and-finance-roles) | Finance-side invoice issuance, void, red-letter, sequential numbering |

#### D. Bulk IO and reporting (6)

| Order | Change | Capabilities | One-line summary |
|---|---|---|---|
| 17 | `async-io-jobs` | async-io-jobs (+ MODIFIED notification-events-extensions) | Shared async IO substrate: storage, status, error file, retention, notifications |
| 18 | `notification-events-extensions` | notification-events-extensions (+ MODIFIED notifications, assignments, exams, class-groups, course-access-period, commerce-extras, account-settings, async-io-jobs, student-bulk-import) | All missing notification event types (assignment.graded, exam.starting-soon, due-soon / due-missed, class.starting-soon, expiry events, refund / order events, invoice lifecycle, IO events, account.welcome, enrollment.granted-bulk) and `Notification.ClassGroupId` |
| 19 | `question-import-export` | question-import-export (+ MODIFIED assessments, question-types, question-bank-admin, notification-events-extensions) | Excel import / export of questions; sync ≤200, async via `async-io-jobs`; Append + UpdateOrAppend modes; partial success; bank variant |
| 20 | `student-bulk-import` | student-bulk-import (+ MODIFIED user-management, enrollment, account-login-extras, notification-events-extensions) | Bulk student account creation + bulk enrollment; three row-action modes; welcome notification |
| 21 | `grade-export` | grade-export (+ MODIFIED assignments, assessments, exams, ta-and-finance-roles) | Streaming Excel export of submissions / attempts / rosters; sync ≤1000, async >1000; no import |
| 22 | `course-outline-import-export` | course-outline-import-export (+ MODIFIED course-structure) | Excel import / export of course modules + lessons; metadata only (no media); Append + Replace modes |
| 23 | `coupon-bulk-import` | coupon-bulk-import (+ MODIFIED commerce-extras, async-io-jobs) | Bulk coupon creation via Excel; append-only; unique-code enforcement |

#### Dependency summary

- Layer A is the chrome + roles; layer D depends on layer A (TA scope, finance pages, sidebar) and on layer B (questions / exams / assignments / Q&A exist).
- Layer B is largely independent; live-streaming depends on live-chat (already shipped).
- Layer C depends on layer A (TA scope for finance pages) and on layer B (exams / assessments exist for scheduled jobs).
- Layer D depends on layer C's `job-scheduler` (the IO substrate uses it for retries / locks) and on layer B for the entities being imported / exported.
- `notification-events-extensions` is centralised — it depends on everything else. Implement it last within its layer group so the templates and recipients are stable.

#### A note on §3.3

Section 3.3 still applies: implement one change at a time, in this list's
order; archive and land via PR before starting the next. The grouping
above is for *reading*, not for parallel work.

### 7.3 Deferred roadmap

- Blazor frontend (full UI rewrite; evaluate after the pending changes).
- Live video calling (WebRTC; `live-chat` design explicitly defers it).

---

## 8. References

- `openspec/specs/*/spec.md` — source-of-truth capability specs
- `openspec/changes/<name>/` — active changes (proposal/design/specs/tasks)
- `openspec/changes/archive/` — frozen history of shipped changes
- `README.md` — overview, setup, demo accounts, license
- `src/OpenLearning.Data/ApplicationDbContext.cs` — module registration
- `src/OpenLearning.Web/Program.cs` — composition root
- `.opencode/skills/openspec-*` — OpenSpec workflow skills
