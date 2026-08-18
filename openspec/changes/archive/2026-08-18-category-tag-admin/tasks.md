# Category & Tag Admin — Tasks

## 1. Data & Service

- [x] 1.1 Add `Category` entity + config in CourseManagement; add `CategoryService` (CRUD admin, list active)
- [x] 1.2 Extend `TagService`: rename, merge, retire
- [x] 1.3 Course create/edit uses category dropdown; rename cascades to `Course.Category`; catalog reads active categories

## 2. Admin UI

- [x] 2.1 `/Admin/Categories`: list/create/edit/deactivate (rename cascades)
- [x] 2.2 `/Admin/Tags`: list with counts, rename, merge, retire

## 3. Migration & Verification

- [x] 3.1 Create EF Core migration
- [x] 3.2 Build, start app, verify: category create/rename cascades, tag rename/merge/retire updates courses/filters, non-admin denied
