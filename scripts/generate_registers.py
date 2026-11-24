#!/usr/bin/env python3
"""Regenerate docs/registers/open-questions.md and parameters.md from feature notes.

Run from the repo root:  python3 scripts/generate_registers.py
Feature notes are the source of truth; never edit the generated registers by hand.
"""
import re
import pathlib
from datetime import date

DOCS = pathlib.Path(__file__).resolve().parent.parent / "docs"
SKIP_DIRS = {"registers", "templates"}
SKIP_FILES = {"AGENTS.md", "00-index.md"}


def header(id_, title, desc):
    return (f"---\nid: {id_}\ntitle: {title}\nstatus: decided\ntags: [register]\n"
            f"updated: {date.today().isoformat()}\n---\n\n# {title}\n\n{desc}\n\n")


def main():
    notes = sorted(p for p in DOCS.rglob("*.md")
                   if not SKIP_DIRS & set(p.relative_to(DOCS).parts) and p.name not in SKIP_FILES)
    questions, params = [], []
    for p in notes:
        for line in p.read_text().splitlines():
            m = re.match(r"- (Q-[A-Z]+-\d+)(?: \(((?:answered|deferred)[^)]*)\))?: (.*)", line)
            if m:
                questions.append((m.group(1), p.stem, m.group(3), m.group(2) or "open"))
            m = re.match(r"\| (P-[A-Z]+-\d+) \|(.*)\|\s*$", line)
            if m:
                cells = [c.strip() for c in m.group(2).split("|")]
                value = cells[1] if len(cells) == 3 else "–"
                params.append((m.group(1), cells[0], value, cells[-1], p.stem))

    out = header("REG-Q", "Open questions register",
                 "Generated from the **Open questions** sections of every feature note. The note is the source of truth; "
                 "regenerate this register after edits. Questions block the promotion of related rules to `decided`.")
    out += "| ID | Note | Question | Status |\n|---|---|---|---|\n"
    out += "".join(f"| {q} | [[{s}]] | {t.replace('|', '/')} | {st} |\n" for q, s, t, st in questions)
    (DOCS / "registers/open-questions.md").write_text(out)

    out = header("REG-P", "Parameters register",
                 "Every tunable value in one place, generated from the **Parameters** tables in the feature notes "
                 "(the note is the source of truth). Values marked `open` or `proposed` MUST be implemented as "
                 "configurable data with placeholders, never hard-coded.")
    out += "| ID | Name | Value | Status | Note |\n|---|---|---|---|---|\n"
    out += "".join(f"| {i} | {n} | {v} | {st} | [[{s}]] |\n" for i, n, v, st, s in params)
    (DOCS / "registers/parameters.md").write_text(out)
    open_n = sum(1 for *_, st in questions if st == "open")
    print(f"{len(questions)} questions ({open_n} open), {len(params)} parameters")


if __name__ == "__main__":
    main()
