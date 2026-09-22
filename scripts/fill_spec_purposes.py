#!/usr/bin/env python3
"""Fill TBD Purpose placeholders in openspec/specs/*/spec.md.

Walks every spec file under openspec/specs/, detects the
`TBD - created by archiving change <name>. Update Purpose after archive.`
placeholder, and replaces it with a Purpose derived from the first
Requirement block in the file. The replacement is intentionally
mechanical so it does not depend on human judgment per spec.

Usage:
    python3 scripts/fill_spec_purposes.py [--dry-run]

Exit codes: 0 = success, 1 = parse error, 2 = no changes needed.
"""

from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SPECS_DIR = ROOT / "openspec" / "specs"

PLACEHOLDER_RE = re.compile(
    r"^## Purpose\s*\nTBD - created by archiving change [^\n]*\n",
    re.MULTILINE,
)
REQUIREMENT_RE = re.compile(r"^### Requirement:\s*(.+?)\s*$", re.MULTILINE)
CAPABILITY_RE = re.compile(r"^#\s+(.+?)\s+Specification\s*$", re.MULTILINE)


def derive_purpose(spec_text: str, capability: str) -> str:
    match = REQUIREMENT_RE.search(spec_text)
    if match is None:
        return (
            f"The {capability} specification defines the behaviors, contracts, and "
            "scenarios that govern this capability across the platform."
        )
    topic = match.group(1).strip().rstrip(".")
    topic_lower = topic[0].lower() + topic[1:] if topic else topic
    return (
        f"The {capability} specification covers {topic_lower} and the related "
        "scenarios that govern this capability across the platform."
    )


def process_file(path: Path, dry_run: bool) -> bool:
    text = path.read_text(encoding="utf-8")
    if not PLACEHOLDER_RE.search(text):
        return False

    cap_match = CAPABILITY_RE.search(text)
    capability = cap_match.group(1).strip() if cap_match else path.parent.name
    new_purpose = derive_purpose(text, capability)
    replacement = f"## Purpose\n\n{new_purpose}\n"

    updated = PLACEHOLDER_RE.sub(replacement, text, count=1)
    if dry_run:
        print(f"[dry-run] {path.relative_to(ROOT)}: would set Purpose to {new_purpose!r}")
    else:
        path.write_text(updated, encoding="utf-8")
        print(f"[update]  {path.relative_to(ROOT)}")
    return True


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dry-run", action="store_true")
    args = parser.parse_args()

    if not SPECS_DIR.is_dir():
        print(f"error: {SPECS_DIR} does not exist", file=sys.stderr)
        return 1

    changed = 0
    for spec in sorted(SPECS_DIR.glob("*/spec.md")):
        if process_file(spec, args.dry_run):
            changed += 1

    print(f"{'[dry-run] ' if args.dry_run else ''}updated {changed} spec file(s)")
    return 0 if changed > 0 else 2


if __name__ == "__main__":
    sys.exit(main())
