## Summary

<!-- What does this change do, and why? Reference the OpenSpec change(s) it implements. -->

## OpenSpec change

<!-- e.g. openspec/changes/<name> (archives to openspec/specs/<cap>/spec.md) -->

## Test evidence

<!--
How did you verify this? Include both:
- Automated: what the CI workflow ran and its result
- Manual: the HTTP smoke tests you exercised (happy path AND negative cases), per Agents.md §4.3
-->

## Quality-gate checklist

- [ ] OpenSpec change exists with all four artifacts (proposal / spec / design / tasks) complete and validated
- [ ] `dotnet format --verify-no-changes` passes
- [ ] Build is clean: 0 warnings / 0 errors under `/warnaserror`
- [ ] Tests pass
- [ ] Spec scenarios verified end-to-end (happy path and negative cases)
- [ ] No stubs, `TODO`s, or `NotImplementedException`
- [ ] Server-side validation and authorization on every mutating page
- [ ] No changes outside the change's scope (composition-root one-liners excepted)
- [ ] Commit message is conventional and explains the *why*

## Notes for the reviewer

<!-- Anything the reviewer should pay attention to (trade-offs, follow-ups). -->
