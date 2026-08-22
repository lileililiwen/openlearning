# AI Learning Assistant — Design

## Context

Generative output can be wrong, leak data, or become an unreviewed grading authority. Course grounding and human accountability are required.

## Goals

- Answer from authorized, instructor-approved course sources with citations.
- Offer formative feedback and grading suggestions without autonomous final grades.
- Make provider use, retention, cost, and safety observable and configurable.

## Non-Goals

- General web search, autonomous instruction, plagiarism verdicts, or replacing instructors.
- Training provider models on user content.

## Decisions

### D0: Existing components reused

The implementation reuses active `Enrollment` records and `Course.InstructorId` for course access, `AssignmentSubmission` for draft inputs, and `AssignmentService.GradeAsync` as the only path that persists a confirmed assignment grade. Reporting follows the existing moderation audit pattern. The AI module owns only provider policy, approved-source metadata, generated drafts, citations, usage, and AI-specific reports.

### D1: Provider boundary and policy

Adapters accept a minimized request and return normalized output/usage. Configuration controls provider, model allowlist, per-feature enablement, quotas, and retention.

### D2: Authorized retrieval

Index only published, approved course materials. Retrieval applies course-access scope before content reaches a provider and returns source anchors.

### D3: Human-reviewed grading

AI may propose rubric evidence, comments, and a score. An authorized grader must edit/confirm before existing grading services persist a grade.

### D4: Safety and transparency

Label generated content, provide reporting, resist prompt-injection from indexed materials, redact secrets/personal data, and keep audit/usage metadata.

## Risks / Trade-offs

- Citations do not guarantee correctness; the UI communicates limitations and supports escalation.
- External providers create data-transfer obligations; administrators must explicitly enable them.

## Migration Plan

Add provider configuration references, indexed-source metadata, conversations, feedback drafts, usage, and audit tables. Default all AI features off.
