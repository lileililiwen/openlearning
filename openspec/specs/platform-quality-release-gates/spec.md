# platform-quality-release-gates Specification

## Purpose

The CI pipeline records, for every release, which evidence was checked, what passed, what was unavailable, and which commit produced the numbers. Capability changes declare their evidence (changed paths, incremental coverage threshold, test classes, migration impact) in a manifest at the repo root; the pipeline validates the manifest, runs the declared test classes, verifies migrations against an empty database, and runs the accessibility suite on learner pages. The quality dashboard distinguishes pass, fail, warning, and unavailable per gate, and preserves provenance (commit, run id, workflow, event, ref, actor, runner, tool versions) so a reader can tell a real pass from an absent gate.
## Requirements
### Requirement: Capability changes declare evidence

Every capability change SHALL declare its changed paths, incremental coverage threshold, applicable test classes, and migration impact before CI evaluates it.

#### Scenario: Change omits evidence manifest

- **WHEN** CI evaluates a capability change without the required manifest
- **THEN** the quality gate fails with the missing fields

#### Scenario: Change declares complete evidence

- **WHEN** the manifest identifies paths, threshold, tests, and migration impact
- **THEN** CI evaluates the declared gates and publishes provenance

### Requirement: Changed code is covered and behavior is tested

CI SHALL fail when changed executable lines are below the declared coverage threshold, required PostgreSQL behavior is untested, or restricted operations lack negative authorization coverage.

#### Scenario: Changed code lacks coverage

- **WHEN** changed executable lines fall below threshold
- **THEN** the pipeline fails and reports the affected paths

#### Scenario: Authorization negative is missing

- **WHEN** a restricted capability has only an authorized happy-path test
- **THEN** the pipeline fails the capability evidence gate

### Requirement: Database and UI quality are verified when applicable

Changes affecting persistence SHALL verify empty-database migration and upgrade migration behavior; changes affecting learner UI SHALL run accessibility and representative responsive smoke checks.

#### Scenario: Migration drift exists

- **WHEN** the model and migrations are inconsistent
- **THEN** the migration gate fails before release

#### Scenario: UI accessibility check fails

- **WHEN** a changed learner page violates a required accessibility check
- **THEN** the UI quality gate fails with the page and rule

### Requirement: Quality reports preserve provenance

The quality dashboard SHALL record commit, timestamp, tool versions, thresholds, evidence classes, and artifact references, and MUST distinguish unavailable evidence from passing evidence.

#### Scenario: Required source is unavailable

- **WHEN** a required quality source cannot be produced
- **THEN** the gate reports unavailable/failure rather than treating it as a pass

