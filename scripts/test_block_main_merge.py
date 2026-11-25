"""Tests for .claude/hooks/block_main_merge.py. Run: python3 -m unittest discover -s scripts"""
import importlib.util
import pathlib
import unittest

HOOK = pathlib.Path(__file__).resolve().parent.parent / ".claude/hooks/block_main_merge.py"
spec = importlib.util.spec_from_file_location("block_main_merge", HOOK)
hook = importlib.util.module_from_spec(spec)
spec.loader.exec_module(hook)

BASES = {"15": "feature/spike-1-hex-map", "5": "main"}


def lookup(selector, repo):
    return BASES.get(selector)


class BlockMainMergeTest(unittest.TestCase):
    def decide(self, cmd):
        return hook.decide(cmd, base_lookup=lookup)

    def test_merge_into_feature_branch_allowed(self):
        self.assertIsNone(self.decide("gh pr merge 15 --squash --delete-branch"))

    def test_merge_into_main_denied(self):
        self.assertIn("DLV-016", self.decide("gh pr merge 5 --merge"))

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
