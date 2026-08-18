# Category & Tag Admin — Tasks

## 1. Data & Service

- [ ] 1.1 Add `Category` entity + config in CourseManagement; add `CategoryService` (CRUD admin, list active)
- [ ] 1.2 Extend `TagService`: rename, merge, retire
- [ ] 1.3 Course create/edit uses category dropdown; rename cascades to `Course.Category`; catalog reads active categories

## 2. Admin UI

- [ ] 2.1 `/Admin/Categories`: list/create/edit/deactivate (rename cascades)
- [ ] 2.2 `/Admin/Tags`: list with counts, rename, merge, retire

## 3. Migration & Verification

- [ ] 3.1 Create EF Core migration
- [ ] 3.2 Build, start app, verify: category create/rename cascades, tag rename/merge/retire updates courses/filters, non-admin denied
