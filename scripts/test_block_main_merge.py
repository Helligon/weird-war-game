"""Tests for .claude/hooks/block_main_merge.py. Run: python3 -m unittest discover -s scripts"""
import importlib.util
import pathlib
import unittest

HOOK = pathlib.Path(__file__).resolve().parent.parent / ".claude/hooks/block_main_merge.py"
spec = importlib.util.spec_from_file_location("block_main_merge", HOOK)
hook = importlib.util.module_from_spec(spec)
spec.loader.exec_module(hook)

BASES = {"15": "feature/spike-1-hex-map", "5": "main", "87": "main", "88": "main", "89": "main",
         "90": "main", "91": "main", "92": "main"}
FILES = {
    "5": [".github/workflows/ci.yml", "src/WeirdWar.Core/GameState.cs"],         # a Feature merge
    "87": ["docs/02-campaign/supply.md", "docs/registers/decision-log.md"],    # docs-only
    "88": ["docs/02-campaign/supply.md", ".claude/hooks/block_main_merge.py"],  # docs plus config
    "89": ["docs/11-delivery/pipeline.md"],                                   # process docs
    "90": ["docs/AGENTS.md", "docs/02-campaign/supply.md"],                    # process docs
    "91": [],                                                                 # no files listed
    "92": ["docs/notes/n%d.md" % i for i in range(100)],                       # too many to check
}


def lookup(selector, repo):
    return BASES.get(selector)


def files(selector, repo):
    return FILES.get(selector)


class BlockMainMergeTest(unittest.TestCase):
    def decide(self, cmd):
        return hook.decide(cmd, base_lookup=lookup, files_lookup=files)

    def test_merge_into_feature_branch_allowed(self):
        self.assertIsNone(self.decide("gh pr merge 15 --squash --delete-branch"))

    def test_merge_into_main_denied(self):
        self.assertIn("DLV-016", self.decide("gh pr merge 5 --merge"))

    def test_DLV_020_docs_only_pr_into_main_allowed(self):
        self.assertIsNone(self.decide("gh pr merge 87 --merge"))

    def test_DLV_020_pr_into_main_with_non_docs_files_denied(self):
        self.assertIn("outside docs/", self.decide("gh pr merge 88 --merge"))

    def test_DLV_020_pipeline_docs_into_main_denied(self):
        self.assertIn("delivery-process", self.decide("gh pr merge 89 --merge"))

    def test_DLV_020_agents_md_into_main_denied(self):
        self.assertIn("delivery-process", self.decide("gh pr merge 90 --merge"))

    def test_DLV_020_unlisted_files_denied(self):
        self.assertIn("Couldn't list", self.decide("gh pr merge 91 --merge"))
        self.assertIn("Couldn't list", hook.decide("gh pr merge 87", base_lookup=lookup,
                                                   files_lookup=lambda s, r: None))

    def test_DLV_020_too_many_files_denied(self):
        self.assertIn("too many", self.decide("gh pr merge 92 --merge"))

    def test_DLV_020_lookalike_paths_denied(self):
        for path in ("docsx/a.md", "src/docs/a.md", "docs/11-delivery/x.md", "docs/AGENTS.md"):
            self.assertIsNotNone(hook.docs_only_reason([path]), path)

    def test_unknown_base_denied(self):
        self.assertIn("Couldn't find", self.decide("gh pr merge 999"))

    def test_value_flag_not_taken_as_selector(self):
        self.assertIsNone(self.decide('gh pr merge --subject "ship it" 15'))

    def test_repo_flag_passed_through(self):
        seen = []
        hook.decide("gh pr merge 15 -R Helligon/weird-war-game",
                    base_lookup=lambda s, r: seen.append((s, r)) or "feature/x")
        self.assertEqual(seen, [("15", ["-R", "Helligon/weird-war-game"])])

    def test_chained_main_merge_denied(self):
        self.assertIsNotNone(self.decide("gh pr checks 15 && gh pr merge 5"))

    def test_raw_api_merge_denied(self):
        self.assertIn("raw API", self.decide("gh api -X PUT repos/o/r/pulls/5/merge"))

    def test_subshell_merge_into_main_denied(self):
        self.assertIn("DLV-016", self.decide("echo $(gh pr merge 5)"))

    def test_unparseable_mention_denied_without_crash(self):
        self.assertIn("couldn't be read", self.decide("echo 'see `gh pr merge` docs'"))

    def test_unrelated_commands_allowed(self):
        for cmd in ("git status", "gh pr view 5", "gh pr create --base main", "git merge feature/x"):
            self.assertIsNone(self.decide(cmd), cmd)


if __name__ == "__main__":
    unittest.main()
