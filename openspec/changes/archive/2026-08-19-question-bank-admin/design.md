# Question Bank Admin — Design

## Context

Questions live only inside quizzes/exams. A central bank enables reuse and consistent quality.

## Goals

- Admins maintain a searchable central question bank.
- Instructors import bank questions into their quizzes/exams.
- Imported questions are snapshots (later bank edits don't alter in-use quizzes).

## Non-Goals

- No shared "live" questions (edits propagate) — snapshots keep quiz integrity.
- No public student-facing bank browsing.
- No bulk import/export in MVP.

## Decisions

### D1: Bank questions reuse `Question`
`Question` gains `bool IsBank`, `string? BankTopic` (free topic/tag), `DateTime? ArchivedAt`. Bank rows are ordinary rows with `IsBank=true` and no quiz association. Import copies the row (options copied too) into the quiz — snapshot semantics.

### D2: `QuestionBankService`
Admin ops: `CreateAsync`, `UpdateAsync`, `ArchiveAsync`, `SearchAsync(topic, text, page)`, `GetByIdAsync`. Instructor op: `ImportAsync(bankQuestionId, quizId)` validates ownership of the quiz, clones the question + options, appends to the quiz. Admins can also import/see all banks.

### D3: UI
`/Admin/QuestionBank` lists/search banks with create/edit/archive. Quiz and exam editors gain an "Import from bank" modal that searches and adds selected questions. Imported questions remain editable locally (snapshot).

## Risks / Trade-offs

- **Snapshot drift** → Bank edits never touch in-use copies; documented as intentional.
- **Duplication** → Import copies grow storage; acceptable, keeps quiz integrity.

## Migration Plan

One migration adds `IsBank`, `BankTopic`, `ArchivedAt` to `Questions`.

## Open Questions

- Should instructors be able to add to the bank? MVP: admin-only; instructors only import.
