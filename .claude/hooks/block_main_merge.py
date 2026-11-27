#!/usr/bin/env python3
"""PreToolUse hook: agents may merge PRs into any branch except main (DLV-016, DLV-017),
plus docs-only PRs into main (DLV-020).

Reads the Bash tool call from stdin. For every `gh pr merge`, it looks up the PR's base
branch. A merge into `main` is denied unless every file the PR changes is under `docs/`,
outside the delivery-process docs (`docs/11-delivery/`, `docs/AGENTS.md`). Anything it can't
determine is denied (fail closed). GitHub still requires the `Doc checks` job to pass.
Merging through the raw API (`gh api .../pulls/N/merge`) is always denied: use `gh pr merge`.
"""
import json
import re
import shlex
import subprocess
import sys

PROTECTED = "main"
# DLV-020: docs-only PRs may be merged into main by an agent, except changes to the delivery process.
DOCS_PREFIX = "docs/"
PROCESS_DOCS = ("docs/11-delivery/", "docs/AGENTS.md")
# `gh pr view --json files` lists at most 100 files; a PR that size is never treated as docs-only.
MAX_FILES = 100
# gh pr merge flags that take a value, so their value isn't mistaken for the PR selector.
VALUE_FLAGS = {"-b", "--body", "-F", "--body-file", "-t", "--subject",
               "-A", "--author-email", "--match-head-commit"}


def gh_base(selector, repo):
    """Return the PR's base branch, or None if it can't be looked up."""
    args = ["gh", "pr", "view", *([selector] if selector else []), *repo,
            "--json", "baseRefName", "--jq", ".baseRefName"]
    try:
        r = subprocess.run(args, capture_output=True, text=True, timeout=20)
    except (OSError, subprocess.TimeoutExpired):
        return None
    return r.stdout.strip() or None if r.returncode == 0 else None


def gh_files(selector, repo):
    """Return the paths the PR changes, or None if they can't be looked up."""
    args = ["gh", "pr", "view", *([selector] if selector else []), *repo,
            "--json", "files", "--jq", ".files[].path"]
    try:
        r = subprocess.run(args, capture_output=True, text=True, timeout=20)
    except (OSError, subprocess.TimeoutExpired):
        return None
    return r.stdout.splitlines() if r.returncode == 0 else None


def docs_only_reason(files):
    """Return None if an agent may merge a PR changing these files into main (DLV-020), else why not."""
    if not files:
        return "Couldn't list this PR's changed files"
    if len(files) >= MAX_FILES:
        return "This PR changes too many files to check"
    outside = [f for f in files if not f.startswith(DOCS_PREFIX)]
    if outside:
        return "This PR changes files outside docs/ (" + ", ".join(outside[:3]) + ")"
    process = [f for f in files if f.startswith(PROCESS_DOCS)]
    if process:
        return "This PR changes the delivery-process docs (" + ", ".join(process[:3]) + ")"
    return None


def decide(command, base_lookup=gh_base, files_lookup=gh_files):
    """Return a reason string to deny the command, or None to allow it."""
    if re.search(r"\bgh\s+api\b", command) and re.search(r"/pulls/\d+/merge\b|mergePullRequest", command):
        return "Merging through the raw API is blocked. Use `gh pr merge` (merges into main are never allowed)."
    for segment in re.split(r"&&|\|\||;|\||\n", command):
        if not re.search(r"\bgh\s+pr\s+merge\b", segment):
            continue
        try:
            tokens = shlex.split(segment)
        except ValueError:
            return "Couldn't parse this `gh pr merge` command, so it is blocked."
        # Strip shell wrappers like `$(`, backticks and parentheses so `$(gh pr merge 5)` is still seen.
        tokens = [t.lstrip("$(`").rstrip(")`") for t in tokens]
        starts = [i + 3 for i in range(len(tokens) - 2) if tokens[i:i + 3] == ["gh", "pr", "merge"]]
        if not starts:
            return ("This command mentions `gh pr merge` but couldn't be read as a merge, so it is blocked. "
                    "If it only mentions the text (e.g. in a file you're writing), use the Edit or Write tool instead.")
        selector, repo, args = None, [], iter(tokens[starts[0]:])
        for a in args:
            if a in ("-R", "--repo"):
                repo = ["-R", next(args, "")]
            elif a.startswith("--repo="):
                repo = ["-R", a.split("=", 1)[1]]
            elif a in VALUE_FLAGS:
                next(args, None)
            elif not a.startswith("-") and selector is None:
                selector = a
        base = base_lookup(selector, repo)
        if base is None:
            return "Couldn't find this PR's target branch, so the merge is blocked."
        if base == PROTECTED:
            why = docs_only_reason(files_lookup(selector, repo))
            if why:
                return (f"{why}, so an agent may not merge it into `{PROTECTED}` (DLV-016). Only docs-only PRs "
                        "outside docs/11-delivery/ and docs/AGENTS.md may be (DLV-020). Alex merges the rest himself.")
    return None


def main():
    data = json.load(sys.stdin)
    reason = decide(data.get("tool_input", {}).get("command", ""))
    if reason:
        print(json.dumps({"hookSpecificOutput": {
            "hookEventName": "PreToolUse",
            "permissionDecision": "deny",
            "permissionDecisionReason": reason,
        }}))


if __name__ == "__main__":
    main()
