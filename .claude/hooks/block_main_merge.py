#!/usr/bin/env python3
"""PreToolUse hook: agents may merge PRs into any branch except main (DLV-016, DLV-017).

Reads the Bash tool call from stdin. For every `gh pr merge`, it looks up the PR's base
branch and denies the call if it is `main`, or if the base can't be determined (fail closed).
Merging through the raw API (`gh api .../pulls/N/merge`) is always denied: use `gh pr merge`.
"""
import json
import re
import shlex
import subprocess
import sys

PROTECTED = "main"
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


def decide(command, base_lookup=gh_base):
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
            return f"Agents may not merge into `{PROTECTED}` (DLV-016). Alex merges Feature branches himself."
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
