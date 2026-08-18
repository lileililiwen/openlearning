# OpenLearning

[![CI](https://github.com/your-org/openlearning/actions/workflows/ci.yml/badge.svg)](https://github.com/your-org/openlearning/actions/workflows/ci.yml)

An open-source, MIT-licensed online learning management system (LMS) built with C# / ASP.NET Core 8 and PostgreSQL.

## Overview

OpenLearning is a modern, genuinely usable LMS MVP that covers the core learning workflows:

- **Authentication & Roles** — ASP.NET Core Identity with Student, Instructor, and Admin roles.
- **Course Management** — Instructors create, edit, publish, and unpublish courses.
- **Course Structure** — Hierarchical Course → Module → Lesson content with ordering.
- **Enrollment** — Students enroll in published courses (duplicates prevented) and can withdraw.
- **Progress Tracking** — Lesson completion marks and per-course progress percentage.

## Tech Stack

| Component   | Technology                              |
|-------------|-----------------------------------------|
| Backend     | ASP.NET Core 8, C#                      |
| Frontend    | Razor Pages (server-rendered)           |
| Database    | PostgreSQL via EF Core (Npgsql)         |
| Auth        | ASP.NET Core Identity + role policies   |
| License     | MIT                                     |

## Getting Started

### Prerequisites

- .NET SDK 8.0+
- PostgreSQL (local, Docker, or remote)

### Setup

```bash
git clone https://github.com/your-org/openlearning.git
cd openlearning
```

Create `src/OpenLearning.Web/appsettings.json` (or set env vars):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=openlearning;Username=postgres;Password=yourpassword"
  }
}
```

Run with auto-migration and seeding:

```bash
dotnet run --project src/OpenLearning.Web
```

The application will apply migrations and seed demo users on first startup.

### Seeded Demo Accounts

| Role       | Email                | Password     |
|------------|----------------------|--------------|
| Admin      | admin@openlearning.dev | Admin123!    |
| Instructor | instructor@openlearning.dev | Instructor123! |
| Student    | student@openlearning.dev | Student123!  |

> **Security note:** seeded credentials are for local development only. Change them before any production use.

## Contributing

Every change is verified automatically by the CI pipeline (`.github/workflows/ci.yml`): formatting is checked with `dotnet format --verify-no-changes`, the solution must build with zero warnings/errors under `/warnaserror`, and the unit test suite must pass. The pipeline runs on every push to `main` and on every pull request, and is a required check for merges.

## Project Structure

The solution is organized as a modular monolith — one package per business domain, so a new domain/spec lives in a new package without touching existing code:

```
src/
├── OpenLearning.Auth/              # Identity user, roles, policies, account services
├── OpenLearning.CourseManagement/  # Course / Module / Lesson aggregate + management services
├── OpenLearning.Enrollment/        # enrollment records and enroll/withdraw services
├── OpenLearning.Progress/          # lesson completions + per-course progress calculation
├── OpenLearning.Data/              # central ApplicationDbContext (entity configs discovered
│                                   #   from modules), migrations, startup seeding
└── OpenLearning.Web/               # Razor Pages UI shell (feature folders under Pages/) + DI composition

openspec/          # OpenSpec specifications and change proposals
```

## Roadmap

- SCORM 1.2/2004 content support (only active standards-compliant C# LMS gap)
- Blazor frontend
- Assessments / quizzes
- Ecommerce for paid courses
- Live video and chat

## License

MIT — see [LICENSE](LICENSE).

## Acknowledgments

This project's design and architecture are informed by, and inspired by, the following MIT-licensed open-source projects. We thank their authors for their contributions to the open-source community:

- **[CoreLMS](https://github.com/FitzyCodesThings/core-lms)** (MIT) by John DeLancey — layered ASP.NET Core architecture, repository pattern, and course delivery concepts.
- **[SmartLearning](https://github.com/divyeshio/SmartLearning)** (MIT) — ASP.NET Core learning platform concepts including role-based student/teacher workflows.
- **[LearnNest / Online-Learning-Platform](https://github.com/Eman288/Online-Learning-Platform)** (MIT) by Eman Tamam — course provider/learner enrollment and progress-tracking model.

We also acknowledge the broader open-source LMS ecosystem that motivated this project, including [SolenLMS](https://github.com/iliasHamdaoui/SolenLms) (clean-architecture ASP.NET Core LMS) as an architectural reference.

*This project is original code; the references above informed feature design and architecture. Any code derived directly from those projects retains their original MIT copyright notices.*