# Surveys & Polls — Tasks

## 1. Domain

- [x] 1.1 Add the Survey project with survey, question, response, and answer models plus configurations
- [x] 1.2 Implement scope/eligibility checks, window enforcement, one-response enforcement (identity or opaque token), and anonymity-at-write
- [x] 1.3 Implement aggregate result computation with live-results gating
- [x] 1.4 Add database registration and an EF Core migration

## 2. Workflows

- [x] 2.1 Add author pages: survey builder (four question types, windows, anonymity, live results), results dashboard
- [x] 2.2 Add respondent pages: course survey list, take-survey form, closed/thanked states

## 3. Verification

- [x] 3.1 Test duplicate rejection, window/enrollment denial, anonymous unlinking, live-results default, non-owner management denial, and academic-record separation
- [x] 3.2 Build cleanly and exercise every scenario over HTTP
