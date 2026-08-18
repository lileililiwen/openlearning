## ADDED Requirements

### Requirement: Code style is enforced by configuration

The system SHALL ship a root `.editorconfig` that defines formatting, naming, and analyzer severities, and a `Directory.Build.props` that applies shared build properties to every project.

#### Scenario: Shared properties
- **WHEN** any project in the solution is built
- **THEN** it inherits nullable enablement, the latest analysis level, and warnings-as-errors from `Directory.Build.props`

#### Scenario: Style violations fail the build
- **WHEN** code violates a configured style or analyzer rule
- **THEN** the build fails with an error naming the violation

#### Scenario: Formatting is deterministic
- **WHEN** `dotnet format` is run
- **THEN** it produces a repeatable, minimal diff that matches the `.editorconfig`

### Requirement: Analyzer rules protect common defects

The system SHALL run the built-in .NET analyzers and `SonarAnalyzer.CSharp` on every build.

#### Scenario: Analyzer detection
- **WHEN** code introduces a defect the analyzers detect (e.g. null dereference, unused code, naming)
- **THEN** the build reports it as an error
