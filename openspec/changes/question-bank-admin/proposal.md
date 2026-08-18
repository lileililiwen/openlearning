## Why

Questions are authored per quiz/exam by instructors. The reference system's Admin Backend requires centralized Question Bank Management: a shared question bank admins can maintain and instructors can reuse.

## What Changes

- A central question bank: questions tagged by category/topic, searchable, and maintainable.
- Admins can create, edit, archive, and review questions in the bank.
- Instructors can import bank questions into their quizzes/exams (reuse without editing the original).

## Capabilities

### New Capabilities
- `question-bank-admin`: a centralized, admin-maintained question bank with instructor import.

### Modified Capabilities

- `assessments`: quiz/exam question authoring supports importing bank questions.

## Impact

- `Question` gains a bank flag or a new `BankQuestion` entity. Decision: reuse `Question` with `IsBank` + `BankTopic` so imports are copies (snapshot semantics).
- `QuestionBankService` (admin CRUD, search by topic/text, import into quiz).
- Admin page `/Admin/QuestionBank`; quiz/exam editors add an "import from bank" picker.
