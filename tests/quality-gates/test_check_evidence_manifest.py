"""Tests for scripts/check_evidence_manifest.py.

Run with: python3 -m unittest tests.quality_gates.test_check_evidence_manifest
"""

from __future__ import annotations

import importlib.util
import os
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SCRIPT = ROOT / "scripts" / "check_evidence_manifest.py"


def _load_validator():
    spec = importlib.util.spec_from_file_location("check_evidence_manifest", SCRIPT)
    assert spec and spec.loader, f"could not load {SCRIPT}"
    module = importlib.util.module_from_spec(spec)
    sys.modules[spec.name] = module
    spec.loader.exec_module(module)
    return module


def _write_manifest(directory: Path, body: str) -> Path:
    path = directory / "quality-manifest.toml"
    path.write_text(body, encoding="utf-8")
    return path


def _has_origin_main() -> bool:
    try:
        subprocess.run(
            ["git", "diff", "--name-only", "origin/main...HEAD"],
            cwd=ROOT,
            check=True,
            capture_output=True,
        )
        return True
    except subprocess.CalledProcessError:
        return False


class LoadManifestTests(unittest.TestCase):
    def setUp(self) -> None:
        self.validator = _load_validator()
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.dir = Path(self.tmp.name)

    def test_loads_populated_manifest(self) -> None:
        path = _write_manifest(
            self.dir,
            """
[change]
paths = [
    "src/OpenLearning.Mobile/",
    "src/OpenLearning.Mobile.Tests/",
]
incremental_coverage_threshold = 0.85
test_classes = ["unit", "postgres"]
migration_impact = "schema"
notes = "adds batch sync"
""".strip(),
        )
        manifest = self.validator.load_manifest(str(path))
        assert manifest is not None
        self.assertEqual(
            manifest.paths,
            ["src/OpenLearning.Mobile/", "src/OpenLearning.Mobile.Tests/"],
        )
        self.assertEqual(manifest.incremental_coverage_threshold, 0.85)
        self.assertEqual(manifest.test_classes, ["unit", "postgres"])
        self.assertEqual(manifest.migration_impact, "schema")
        self.assertEqual(manifest.notes, "adds batch sync")

    def test_loads_manifest_with_comments_and_blank_lines(self) -> None:
        path = _write_manifest(
            self.dir,
            """
# top-level comment
[change]
# inline comment
paths = [
    # inline array comment
    "src/OpenLearning.Web/",
]

incremental_coverage_threshold = 0.80
test_classes = ["unit"]
migration_impact = "none"
""".strip(),
        )
        manifest = self.validator.load_manifest(str(path))
        assert manifest is not None
        self.assertEqual(manifest.paths, ["src/OpenLearning.Web/"])
        self.assertEqual(manifest.test_classes, ["unit"])

    def test_missing_file_returns_none(self) -> None:
        self.assertIsNone(self.validator.load_manifest(str(self.dir / "absent.toml")))

    def test_unparseable_line_raises(self) -> None:
        path = _write_manifest(self.dir, "this is not a valid line\n")
        with self.assertRaises(ValueError):
            self.validator.load_manifest(str(path))


class ValidateTests(unittest.TestCase):
    def setUp(self) -> None:
        self.validator = _load_validator()

    def _manifest(self, **overrides) -> object:
        manifest = self.validator.Manifest(
            paths=["src/OpenLearning.Mobile/"],
            incremental_coverage_threshold=0.8,
            test_classes=["unit"],
            migration_impact="none",
            notes="",
        )
        for key, value in overrides.items():
            setattr(manifest, key, value)
        return manifest

    def test_missing_manifest_yields_error(self) -> None:
        errors = self.validator.validate(None, ["src/OpenLearning.Web/Foo.cs"], strict=True)
        self.assertIn("missing manifest at 'quality-manifest.toml'", errors[0])

    def test_empty_paths_yields_error(self) -> None:
        manifest = self._manifest(paths=[])
        errors = self.validator.validate(manifest, ["src/OpenLearning.Mobile/Foo.cs"], strict=True)
        self.assertIn("change.paths must list at least one path", errors)

    def test_threshold_out_of_range_yields_error(self) -> None:
        manifest = self._manifest(incremental_coverage_threshold=1.5)
        errors = self.validator.validate(manifest, ["src/OpenLearning.Mobile/Foo.cs"], strict=True)
        self.assertTrue(any("between 0.0 and 1.0" in e for e in errors))

    def test_unknown_test_class_yields_error(self) -> None:
        manifest = self._manifest(test_classes=["unit", "made_up_class"])
        errors = self.validator.validate(manifest, ["src/OpenLearning.Mobile/Foo.cs"], strict=True)
        self.assertTrue(any("unknown values" in e for e in errors))

    def test_unknown_migration_impact_yields_error(self) -> None:
        manifest = self._manifest(migration_impact="unclear")
        errors = self.validator.validate(manifest, ["src/OpenLearning.Mobile/Foo.cs"], strict=True)
        self.assertTrue(any("must be one of" in e for e in errors))

    def test_long_notes_yields_error(self) -> None:
        manifest = self._manifest(notes="x" * 241)
        errors = self.validator.validate(manifest, ["src/OpenLearning.Mobile/Foo.cs"], strict=True)
        self.assertTrue(any("240 characters" in e for e in errors))

    def test_strict_path_mismatch_yields_error(self) -> None:
        manifest = self._manifest(paths=["src/OpenLearning.Web/"])
        errors = self.validator.validate(manifest, ["src/OpenLearning.Mobile/Foo.cs"], strict=True)
        self.assertTrue(any("no declared path matches" in e for e in errors))

    def test_strict_path_match_succeeds(self) -> None:
        manifest = self._manifest(paths=["src/OpenLearning.Mobile/"])
        errors = self.validator.validate(manifest, ["src/OpenLearning.Mobile/Foo.cs"], strict=True)
        self.assertEqual(errors, [])

    def test_relevant_paths_filters_non_source_changes(self) -> None:
        paths = ["README.md", "docs/quality/README.md", "src/OpenLearning.Web/Foo.cs"]
        relevant = self.validator.relevant_paths(paths)
        self.assertEqual(relevant, ["src/OpenLearning.Web/Foo.cs"])

    def test_matches_any_handles_directory_prefix(self) -> None:
        self.assertTrue(
            self.validator.matches_any(
                "src/OpenLearning.Mobile/Services/MobileSyncService.cs",
                ["src/OpenLearning.Mobile/"],
            )
        )
        self.assertTrue(
            self.validator.matches_any(
                "src/OpenLearning.Mobile/Services/MobileSyncService.cs",
                ["src/OpenLearning.Mobile/Services/*"],
            )
        )
        self.assertFalse(
            self.validator.matches_any(
                "src/OpenLearning.Web/Foo.cs",
                ["src/OpenLearning.Mobile/"],
            )
        )


class MainEntryTests(unittest.TestCase):
    def setUp(self) -> None:
        self.validator = _load_validator()
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.dir = Path(self.tmp.name)
        self._argv = sys.argv
        self._cwd = os.getcwd()
        # Run the validator from the repo root so its git diff
        # commands resolve origin/main regardless of where the test
        # was launched from.
        os.chdir(ROOT)

    def tearDown(self) -> None:
        sys.argv = self._argv
        os.chdir(self._cwd)

    def _run(self, *args: str) -> int:
        sys.argv = ["check_evidence_manifest.py", *args]
        try:
            return self.validator.main()
        except SystemExit as exit:
            return int(exit.code)

    def test_no_base_ref_skips_gate(self) -> None:
        # On a main-branch push the manifest gate is PR-only and must
        # not block a "no active change" state.
        exit_code = self._run(
            "--manifest", str(self.dir / "absent.toml"),
        )
        self.assertEqual(exit_code, 0)

    def test_base_ref_with_invalid_manifest_fails(self) -> None:
        # PRs against main with an empty manifest must fail the gate.
        # The integration check requires an origin/main ref; in
        # developer sandboxes without one the gate correctly skips
        # (the manifest body is irrelevant) and the test is skipped
        # to avoid false failures outside CI.
        if not _has_origin_main():
            self.skipTest("origin/main not available; integration test requires a real PR base ref")
        _write_manifest(
            self.dir,
            """
[change]
paths = []
incremental_coverage_threshold = 0.80
test_classes = []
migration_impact = "none"
""".strip(),
        )
        exit_code = self._run(
            "--manifest", str(self.dir / "quality-manifest.toml"),
            "--base", "main",
        )
        self.assertEqual(exit_code, 1)

    def test_base_ref_with_valid_manifest_passes(self) -> None:
        if not _has_origin_main():
            self.skipTest("origin/main not available; integration test requires a real PR base ref")
        _write_manifest(
            self.dir,
            """
[change]
paths = ["src/OpenLearning.Mobile/"]
incremental_coverage_threshold = 0.80
test_classes = ["unit"]
migration_impact = "none"
""".strip(),
        )
        exit_code = self._run(
            "--manifest", str(self.dir / "quality-manifest.toml"),
            "--base", "main",
        )
        # main base is available: validator evaluates the manifest and
        # passes because it is fully populated.
        self.assertEqual(exit_code, 0)


if __name__ == "__main__":
    unittest.main()
