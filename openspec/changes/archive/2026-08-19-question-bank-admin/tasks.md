# Question Bank Admin — Tasks

## 1. Data & Service

- [x] 1.1 Add `IsBank`, `BankTopic`, `ArchivedAt` to `Question` + config
- [x] 1.2 Implement `QuestionBankService` (admin CRUD/search/archive, instructor import with snapshot copy)

## 2. UI

- [x] 2.1 `/Admin/QuestionBank` page: search, create, edit, archive
- [x] 2.2 Quiz/exam editors: "Import from bank" picker (owner-gated)

## 3. Migration & Verification

- [x] 3.1 Create EF Core migration
- [x] 3.2 Build, start app, verify: bank CRUD, search, import copies into quiz, editing a bank question doesn't change the imported copy, non-admin import gated by quiz ownership
