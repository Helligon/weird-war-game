#!/usr/bin/env python3
"""Check the design vault in docs/ for structural errors.

Run from anywhere:  python3 scripts/check_docs.py [docs_dir]
Exits 1 and lists every problem if any check fails. Checks:
  - frontmatter has id, title, status, updated; status and date are valid
  - every [[wikilink]] points at an existing note
  - rule lines use a valid status and the note's ID prefix
  - rule, parameter, question and decision IDs are defined only once
  - every ID referenced anywhere is defined (struck ~~rules~~ count as defined)
  - the generated registers match their source notes
"""
import re
import sys
import shutil
import pathlib
import tempfile
import importlib.util
from collections import defaultdict

ROOT = pathlib.Path(__file__).resolve().parent.parent
STATUSES = {"decided", "proposed", "open", "deferred"}
SKIP_DIRS = {"templates", ".obsidian"}
GENERATED = ("open-questions.md", "parameters.md")

FRONTMATTER = re.compile(r"\A---\n(.*?)\n---\n", re.S)
WIKILINK = re.compile(r"\[\[([^\]|#]+)(?:[#|][^\]]*)?\]\]")
RULE = re.compile(r"^- \*\*([A-Z]+)-(\d{3})\*\* \(`([^`]*)`")
STRUCK = re.compile(r"~~([A-Z]+-\d{3})~~")
PARAM_DEF = re.compile(r"^\| (P-[A-Z]+-\d+) \|")
QUESTION_DEF = re.compile(r"^- (Q-[A-Z]+-\d+)\b")
DECISION_DEF = re.compile(r"^\| (DEC-\d{3}) \|")
REFERENCE = re.compile(r"\b((?:P-|Q-)?[A-Z]{2,4}-\d{2,3})\b")


def notes(docs):
    return sorted(p for p in docs.rglob("*.md") if not SKIP_DIRS & set(p.relative_to(docs).parts))


def frontmatter(text):
    m = FRONTMATTER.match(text)
    if not m:
        return None
    fm = {}
    for line in m.group(1).splitlines():
        if ":" in line and not line.startswith((" ", "-")):
            key, value = line.split(":", 1)
            fm[key.strip()] = value.split(" #")[0].strip()
    return fm


def check(docs):
    errors = []
    paths = notes(docs)
    stems = {p.stem for p in docs.rglob("*.md")}  # templates are valid link targets
    defined = defaultdict(list)  # ID -> [where]
    struck = set()
    referenced = defaultdict(set)  # ID -> {note}

    for p in paths:
        rel = p.relative_to(docs)
        text = p.read_text()
        fm = frontmatter(text)
        if fm is None:
            errors.append(f"{rel}: missing frontmatter")
            fm = {}
        else:
            for key in ("id", "title", "status", "updated"):
                if not fm.get(key):
                    errors.append(f"{rel}: frontmatter missing '{key}'")
            if fm.get("status") and fm["status"] not in STATUSES:
                errors.append(f"{rel}: invalid note status '{fm['status']}'")
            if fm.get("updated") and not re.fullmatch(r"\d{4}-\d{2}-\d{2}", fm["updated"]):
                errors.append(f"{rel}: 'updated' is not YYYY-MM-DD: '{fm['updated']}'")

        for target in WIKILINK.findall(text):
            if target.strip() not in stems:
                errors.append(f"{rel}: broken link [[{target}]]")

        is_register = rel.parts[0] == "registers"
        for n, line in enumerate(text.splitlines(), 1):
            m = RULE.match(line)
            if m:
                prefix, num, status = m.groups()
                rule_id = f"{prefix}-{num}"
                defined[rule_id].append(f"{rel}:{n}")
                if status not in STATUSES:
                    errors.append(f"{rel}:{n}: {rule_id} has invalid status '{status}'")
                if fm.get("id") and prefix != fm["id"]:
                    errors.append(f"{rel}:{n}: {rule_id} does not match note id '{fm['id']}'")
            struck.update(STRUCK.findall(line))
            if not is_register:  # registers mirror the notes, so would always look duplicated
                for pattern in (PARAM_DEF, QUESTION_DEF):
                    m = pattern.match(line)
                    if m:
                        defined[m.group(1)].append(f"{rel}:{n}")
            m = DECISION_DEF.match(line)
            if m:
                defined[m.group(1)].append(f"{rel}:{n}")
            for ref in REFERENCE.findall(line):
                referenced[ref].add(str(rel))

    for id_, where in sorted(defined.items()):
        if len(where) > 1:
            errors.append(f"{id_} defined more than once: {', '.join(where)}")
    for id_, where in sorted(referenced.items()):
        if id_ not in defined and id_ not in struck:
            errors.append(f"{id_} referenced but never defined (in {', '.join(sorted(where))})")

    errors.extend(stale_registers(docs))
    return errors


def stale_registers(docs):
    """Regenerate the registers from a copy of docs/ and compare, ignoring the 'updated' date."""
    spec = importlib.util.spec_from_file_location("generate_registers", ROOT / "scripts/generate_registers.py")
    gen = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(gen)
    errors = []
    with tempfile.TemporaryDirectory() as tmp:
        copy = pathlib.Path(tmp) / "docs"
        shutil.copytree(docs, copy, ignore=shutil.ignore_patterns(".obsidian"))
        gen.DOCS = copy
        gen.print = lambda *a, **k: None  # silence the generator's summary line
        gen.main()
        for name in GENERATED:
            def body(p):
                return [l for l in p.read_text().splitlines() if not l.startswith("updated:")]
            if body(docs / "registers" / name) != body(copy / "registers" / name):
                errors.append(f"registers/{name} is stale: run python3 scripts/generate_registers.py")
    return errors


def main():
    docs = pathlib.Path(sys.argv[1]) if len(sys.argv) > 1 else ROOT / "docs"
    errors = check(docs)
    for e in errors:
        print(f"ERROR {e}")
    print(f"{len(notes(docs))} notes checked, {len(errors)} error(s)")
    sys.exit(1 if errors else 0)


if __name__ == "__main__":
    main()
