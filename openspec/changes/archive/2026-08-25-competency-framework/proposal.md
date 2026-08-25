## Why

Skills/competency management is the top-stated priority in corporate LMS buying (>50% of organizations per Fosway 2026 research), and every comparable platform we surveyed (Moodle competencies, Canvas outcomes, 360Learning Skills) ships a framework layer that OpenLearning lacks. The platform already has credits, graduation, practical-training placements, and learning paths — but no way to declare what a learner can *do* or to show gaps against a required skill set.

## What Changes

- Add admin-managed competency frameworks: hierarchical competencies with descriptions and an achievement scale.
- Add competency mapping to courses and assignments so completing mapped activities produces competency evidence automatically.
- Add manual evidence submission with reviewer approval for skills demonstrated outside activity completion.
- Add learner competency profiles and manager/instructor gap analysis against a target framework.
- Version frameworks so later edits never rewrite already-earned achievements.

## Capabilities

### New Capabilities
- `competency-framework`: framework definition, activity-to-competency mapping, automatic and manual evidence, approval workflow, profiles, and gap analysis.

### Modified Capabilities
- None.

## Impact

- New `OpenLearning.Competency` domain module consuming trusted completion sources (progress-tracking, assignments) via existing event/completion data.
- New Admin pages for framework CRUD, instructor pages for mapping and approvals, student pages for profile and gaps.
- New EF Core migration; read-only consumption of existing completions — no changes to grades, credits, or graduation rules.
