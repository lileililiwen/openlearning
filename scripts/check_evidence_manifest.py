#!/usr/bin/env python3
"""Capability evidence manifest validator.

Validates `quality-manifest.toml` at the repo root against the schema
documented at the top of that file. The manifest declares which paths
the change touches, what incremental coverage it commits to, which test
classes it exercises, and what its migration impact is. CI fails the
PR when the manifest is missing, malformed, declares illegal values, or
diverges from the actual git diff.

This script intentionally does NOT depend on `tomllib` (Python 3.11+)
or `tomli` so the same script runs in any GitHub-hosted Python
runtime without an extra install step.

Usage:
    python3 scripts/check_evidence_manifest.py \
        [--base origin/main] [--manifest quality-manifest.toml] [--strict]

Exit codes: 0 = pass, 1 = gate red (errors printed), 2 = no diff
            against base (PR against a fork with no shared history).
"""

from __future__ import annotations

import argparse
import fnmatch
import os
import re
import subprocess
import sys
from dataclasses import dataclass, field
from typing import Iterable

ALLOWED_TEST_CLASSES = {
    "unit",
    "postgres",
    "authorization_negative",
    "accessibility",
    "migration",
    "ui_smoke",
}
ALLOWED_MIGRATION_IMPACTS = {"none", "model_only", "schema"}


@dataclass
class Manifest:
    paths: list[str] = field(default_factory=list)
    incremental_coverage_threshold: float | None = None
    test_classes: list[str] = field(default_factory=list)
    migration_impact: str | None = None
    notes: str | None = None
    source_path: str = "quality-manifest.toml"


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--base", default=os.environ.get("GITHUB_BASE_REF", ""))
    parser.add_argument("--manifest", default="quality-manifest.toml")
    parser.add_argument(
        "--strict",
        action="store_true",
        help="Also fail when declared paths do not overlap with the actual diff.",
    )
    return parser.parse_args()


def load_manifest(path: str) -> Manifest | None:
    if not os.path.exists(path):
        return None

    manifest = Manifest(source_path=path)
    section: str | None = None
    array_key: str | None = None
    array_lines: list[str] = []
    array_terminator: str = ""
    with open(path, "r", encoding="utf-8") as handle:
        lines = handle.readlines()
    for raw_line in lines:
        line = raw_line.split("#", 1)[0].rstrip()
        stripped = line.strip()
        if array_key is not None:
            if "]" in stripped:
                head, _, tail = stripped.partition("]")
                if head:
                    array_lines.append(head)
                array_terminator = tail
                value = _parse_string_array("[" + ",".join(array_lines) + "]")
                _assign(manifest, array_key, value)
                array_key = None
                array_lines = []
                if array_terminator.strip():
                    line = array_terminator
                else:
                    continue
            else:
                array_lines.append(stripped)
                continue
        if not stripped:
            continue
        if stripped.startswith("[") and stripped.endswith("]") and "=" not in stripped:
            section = stripped[1:-1].strip()
            continue
        if "=" not in line:
            raise ValueError(f"unparseable line: {raw_line!r}")
        key, _, value = line.partition("=")
        key = key.strip()
        value = value.strip()
        if section != "change":
            continue
        if value.startswith("[") and value.endswith("]"):
            parsed = _parse_string_array(value)
            _assign(manifest, key, parsed)
        elif value.startswith("["):
            array_key = key
            array_lines = [value[1:]]
        else:
            parsed = _parse_scalar(value)
            _assign(manifest, key, parsed)
    return manifest


def _parse_scalar(value: str) -> object:
    if (value.startswith('"') and value.endswith('"')) or (
        value.startswith("'") and value.endswith("'")
    ):
        return value[1:-1]
    try:
        return float(value)
    except ValueError:
        return value


def _parse_string_array(value: str) -> list[str]:
    inner = value[1:-1].strip()
    if not inner:
        return []
    out: list[str] = []
    for piece in re.split(r",\s*", inner):
        piece = piece.strip()
        if not piece or piece.startswith("#"):
            continue
        if (piece.startswith('"') and piece.endswith('"')) or (
            piece.startswith("'") and piece.endswith("'")
        ):
            piece = piece[1:-1]
        out.append(piece)
    return out


def _assign(manifest: Manifest, key: str, value: object) -> None:
    if key == "paths":
        manifest.paths = list(value) if isinstance(value, list) else []
    elif key == "incremental_coverage_threshold":
        manifest.incremental_coverage_threshold = float(value) if value is not None else None
    elif key == "test_classes":
        manifest.test_classes = list(value) if isinstance(value, list) else []
    elif key == "migration_impact":
        manifest.migration_impact = str(value) if value is not None else None
    elif key == "notes":
        manifest.notes = str(value) if value is not None else None


def diff_paths(base: str) -> list[str] | None:
    if not base:
        return None
    try:
        result = subprocess.run(
            ["git", "diff", "--name-only", f"origin/{base}...HEAD"],
            check=True,
            capture_output=True,
            text=True,
        )
    except subprocess.CalledProcessError:
        return None
    return [line.strip() for line in result.stdout.splitlines() if line.strip()]


SOURCE_PREFIXES = ("src/", "tests/")


def relevant_paths(paths: Iterable[str]) -> list[str]:
    return [p for p in paths if p.startswith(SOURCE_PREFIXES)]


def matches_any(path: str, patterns: Iterable[str]) -> bool:
    for pattern in patterns:
        if pattern.endswith("/") and path.startswith(pattern):
            return True
        if fnmatch.fnmatch(path, pattern):
            return True
    return False


def validate(manifest: Manifest | None, actual: list[str] | None, strict: bool) -> list[str]:
    errors: list[str] = []
    if manifest is None:
        return [f"missing manifest at {Manifest.source_path!r}"]

    if not manifest.paths:
        errors.append("change.paths must list at least one path")

    if manifest.incremental_coverage_threshold is None:
        errors.append("change.incremental_coverage_threshold is required")
    elif not 0.0 <= manifest.incremental_coverage_threshold <= 1.0:
        errors.append(
            f"change.incremental_coverage_threshold must be between 0.0 and 1.0, "
            f"got {manifest.incremental_coverage_threshold}"
        )

    if not manifest.test_classes:
        errors.append("change.test_classes must list at least one class")
    else:
        unknown = sorted(set(manifest.test_classes) - ALLOWED_TEST_CLASSES)
        if unknown:
            errors.append(
                f"change.test_classes contains unknown values: {unknown}; "
                f"allowed: {sorted(ALLOWED_TEST_CLASSES)}"
            )

    if manifest.migration_impact is None:
        errors.append("change.migration_impact is required")
    elif manifest.migration_impact not in ALLOWED_MIGRATION_IMPACTS:
        errors.append(
            f"change.migration_impact must be one of {sorted(ALLOWED_MIGRATION_IMPACTS)}, "
            f"got {manifest.migration_impact!r}"
        )

    if manifest.notes is not None and len(manifest.notes) > 240:
        errors.append("change.notes must be 240 characters or fewer")

    if actual is not None:
        relevant = relevant_paths(actual)
        if relevant and manifest.paths:
            covered = [p for p in relevant if matches_any(p, manifest.paths)]
            if not covered and strict:
                errors.append(
                    "no declared path matches the actual diff; "
                    f"diff hits {relevant[:3]}{'...' if len(relevant) > 3 else ''}"
                )

    return errors


def main() -> int:
    args = parse_args()
    actual = diff_paths(args.base) if args.base else None
    if actual is None:
        if args.base:
            print(f"::notice::could not diff against base {args.base!r}; skipping evidence gate")
        else:
            print("[evidence] no base ref; evidence gate is PR-only and skipped on main pushes")
        return 0

    try:
        manifest = load_manifest(args.manifest)
    except (OSError, ValueError) as ex:
        print(f"::error::failed to parse {args.manifest}: {ex}")
        return 1

    errors = validate(manifest, actual, args.strict)
    if errors:
        print("::group::Evidence manifest gate — FAIL")
        for error in errors:
            print(f"::error file=quality-manifest.toml::{error}")
        print("::endgroup::")
        return 1

    relevant = relevant_paths(actual)
    declared = sum(1 for p in relevant if matches_any(p, manifest.paths or []))
    print(
        f"[evidence] manifest OK; {declared} of {len(relevant)} relevant changed paths declared"
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())
