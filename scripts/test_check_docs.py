"""Tests for check_docs.py. Run: python3 -m unittest discover scripts"""
import shutil
import pathlib
import tempfile
import unittest

import check_docs

DOCS = pathlib.Path(__file__).resolve().parent.parent / "docs"
HEROES = "06-leaders/heroes.md"


class CheckDocsTest(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.docs = pathlib.Path(self.tmp.name) / "docs"
        shutil.copytree(DOCS, self.docs, ignore=shutil.ignore_patterns(".obsidian"))

    def tearDown(self):
        self.tmp.cleanup()

    def edit(self, rel, old, new):
        p = self.docs / rel
        text = p.read_text()
        self.assertIn(old, text, "fixture text not found; update the test")
        p.write_text(text.replace(old, new, 1))

    def assertError(self, fragment):
        errors = check_docs.check(self.docs)
        self.assertTrue(any(fragment in e for e in errors), f"no error containing {fragment!r} in {errors}")

    def test_real_vault_passes(self):
        self.assertEqual(check_docs.check(self.docs), [])

    def test_missing_frontmatter_key(self):
        self.edit(HEROES, "title: Heroes\n", "")
        self.assertError("frontmatter missing 'title'")

    def test_invalid_note_status(self):
        self.edit(HEROES, "status: decided\n", "status: maybe\n")
        self.assertError("invalid note status 'maybe'")

    def test_bad_updated_date(self):
        self.edit(HEROES, "updated: 2026-11-22", "updated: yesterday")
        self.assertError("'updated' is not YYYY-MM-DD")

    def test_broken_wikilink(self):
        self.edit(HEROES, "[[player-character]])", "[[no-such-note]])")
        self.assertError("broken link [[no-such-note]]")

    def test_invalid_rule_status(self):
        self.edit(HEROES, "**HER-001** (`decided`)", "**HER-001** (`agreed`)")
        self.assertError("HER-001 has invalid status 'agreed'")

    def test_rule_prefix_mismatch(self):
        self.edit(HEROES, "**HER-001** (`decided`)", "**OFF-901** (`decided`)")
        self.assertError("OFF-901 does not match note id 'HER'")

    def test_duplicate_rule_id(self):
        self.edit(HEROES, "**HER-002** (`decided`)", "**HER-001** (`decided`)")
        self.assertError("HER-001 defined more than once")

    def test_undefined_reference(self):
        self.edit(HEROES, "(OFF-009)", "(OFF-999)")
        self.assertError("OFF-999 referenced but never defined")

    def test_struck_rule_counts_as_defined(self):
        self.edit(HEROES, "(OFF-009)", "(VET-006)")  # VET-006 is a withdrawn ~~rule~~
        self.assertEqual(check_docs.check(self.docs), [])

    def test_stale_register(self):
        self.edit(HEROES, "| P-HER-01 | Max heroes per Brigade | 1 |", "| P-HER-01 | Max heroes per Brigade | 2 |")
        self.assertError("registers/parameters.md is stale")


if __name__ == "__main__":
    unittest.main()
