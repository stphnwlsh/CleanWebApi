import io
import json
import os
import sys
import tempfile
import unittest
from unittest import mock

sys.path.insert(0, os.path.dirname(__file__))

import report  # noqa: E402

TRX = """<?xml version="1.0" encoding="utf-8"?>
<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
  <Results>
    <UnitTestResult testName="Tests.One" outcome="Passed" />
    <UnitTestResult testName="Tests.Two" outcome="Passed" />
    <UnitTestResult testName="Tests.Skipped" outcome="NotExecuted" />
    <UnitTestResult testName="Tests.Broken" outcome="Failed">
      <Output><ErrorInfo><Message>Expected 1 but was 2
at line 4</Message></ErrorInfo></Output>
    </UnitTestResult>
  </Results>
</TestRun>
"""

COVERAGE = """<?xml version="1.0" encoding="utf-8"?>
<CoverageSession>
  <Summary numSequencePoints="4" visitedSequencePoints="3" sequenceCoverage="75" />
  <Modules>
    <Module>
      <Files>
        <File uid="1" fullPath="/sln/src/Application/Full.cs" />
        <File uid="2" fullPath="/sln/src/Application/Half.cs" />
      </Files>
      <Classes><Class><Methods>
        <Method><FileRef uid="1" /><SequencePoints>
          <SequencePoint vc="1" sl="1" fileid="1" />
          <SequencePoint vc="3" sl="2" fileid="1" />
        </SequencePoints></Method>
        <Method><FileRef uid="2" /><SequencePoints>
          <SequencePoint vc="2" sl="1" fileid="2" />
          <SequencePoint vc="0" sl="2" fileid="2" />
        </SequencePoints></Method>
      </Methods></Class></Classes>
    </Module>
  </Modules>
</CoverageSession>
"""


def write_results(directory, exit_code="0", trx=TRX, coverage=COVERAGE):
    if trx is not None:
        with open(os.path.join(directory, "tests_1.trx"), "w", encoding="utf-8") as handle:
            handle.write(trx)
    if coverage is not None:
        with open(os.path.join(directory, "coverage.opencover.xml"), "w", encoding="utf-8") as handle:
            handle.write(coverage)
    if exit_code is not None:
        with open(os.path.join(directory, "exit-code"), "w", encoding="utf-8") as handle:
            handle.write(exit_code + "\n")


class ParseTests(unittest.TestCase):
    def test_trx_counts_outcomes_and_keeps_first_line_of_failures(self):
        with tempfile.TemporaryDirectory() as directory:
            write_results(directory)
            tests, _, _ = report.read_results(directory)
        self.assertEqual((tests.passed, tests.failed, tests.skipped, tests.total), (2, 1, 1, 4))
        self.assertEqual(tests.failures[0].name, "Tests.Broken")
        self.assertEqual(tests.failures[0].message, "Expected 1 but was 2")

    def test_coverage_uses_summary_and_ranks_files_by_coverage(self):
        with tempfile.TemporaryDirectory() as directory:
            write_results(directory)
            _, coverage, exit_code = report.read_results(directory)
        self.assertEqual(coverage.percent, 75)
        self.assertEqual((coverage.covered, coverage.total), (3, 4))
        self.assertEqual(coverage.files[0].path, "src/Application/Half.cs")
        self.assertEqual(coverage.files[0].percent, 50)
        self.assertEqual(exit_code, 0)

    def test_missing_files_are_reported_as_none(self):
        with tempfile.TemporaryDirectory() as directory:
            self.assertEqual(report.read_results(directory), (None, None, None))


class RenderTests(unittest.TestCase):
    def test_test_summary_lists_failures_and_least_covered_files(self):
        with tempfile.TemporaryDirectory() as directory:
            write_results(directory)
            tests, coverage, _ = report.read_results(directory)
        text = report.render_tests(tests, coverage, 95)
        self.assertIn("**2** passed, **1** failed, **1** skipped", text)
        self.assertIn("| `Tests.Broken` | Expected 1 but was 2 |", text)
        self.assertIn("which is below the 95% threshold", text)
        self.assertIn("| `src/Application/Half.cs` | 1 of 2 | 50% |", text)
        self.assertNotIn("Full.cs", text)

    def test_test_summary_explains_missing_results(self):
        text = report.render_tests(None, None, 95)
        self.assertIn("No test results were produced", text)
        self.assertIn("No coverage report was produced", text)

    def test_pipe_characters_are_escaped_in_cells(self):
        self.assertEqual(report.cell("a|b\nc"), "a\\|b c")


class VerdictTests(unittest.TestCase):
    def test_all_passing_is_ready_to_merge(self):
        stages = [report.Stage("Build", "success", "ok"), report.Stage("Dependency Review", "n/a", "PRs only")]
        self.assertIn("Ready to merge", report.verdict(stages, pull_request=True))
        self.assertIn("Pipeline passed", report.verdict(stages, pull_request=False))

    def test_first_failure_is_named_with_its_headline(self):
        stages = [
            report.Stage("Build", "success", "ok"),
            report.Stage("Test", "failure", "1 failed"),
            report.Stage("Container Scan", "skipped", "didn't run"),
        ]
        self.assertEqual(report.verdict(stages, True), "❌ **Blocked by Test** (and 1 more): 1 failed.")

    def test_skipped_stage_blocks(self):
        stages = [report.Stage("Container Scan", "skipped", "didn't run")]
        self.assertIn("the stage was skipped", report.verdict(stages, True))

    def test_summary_table_has_marker_rows_and_links(self):
        stages = [report.Stage("Build", "success", "version `1.0`", "https://example.com/job")]
        text = report.render_summary(stages, True, "https://example.com/run", "abcdef1234")
        self.assertTrue(text.startswith(report.MARKER))
        self.assertIn("| [Build](https://example.com/job) | 🟢 Passed | Version `1.0` |", text)
        self.assertIn("Commit `abcdef1`", text)


class HeadlineTests(unittest.TestCase):
    def test_build_headline_shows_version_and_size(self):
        needs = {"build": {"result": "success", "outputs": {"version": "2026.11.01.5", "package_size": "165426"}}}
        self.assertEqual(report.build_headline(needs), "version `2026.11.01.5` · template package 162 KB")

    def test_alerts_are_counted_by_severity(self):
        alerts = [
            {"rule": {"security_severity_level": "high"}},
            {"rule": {"security_severity_level": "high"}},
            {"rule": {"severity": "warning"}},
        ]
        self.assertEqual(report.alerts_headline(alerts, "CodeQL"), "3 open CodeQL alerts: 2 high, 1 warning")
        self.assertEqual(report.alerts_headline(alerts, "CodeQL", passed=True), "3 open CodeQL alerts, none blocking: 2 high, 1 warning")
        self.assertEqual(report.alerts_headline([], "Trivy"), "no open Trivy alerts")
        self.assertIn("couldn't be read", report.alerts_headline(None, "Trivy"))

    def test_dependency_headline_counts_outputs(self):
        needs = {"dependencies": {"result": "failure", "outputs": {"vulnerable": json.dumps([{}, {}]), "licences": "[]"}}}
        self.assertEqual(report.dependency_headline(needs), "2 vulnerable packages · 0 licence problems")

    def test_test_headline_blames_the_template_only_when_the_tests_passed(self):
        tests = report.TestResults(passed=3)
        self.assertIn("generated template failed", report.test_headline("failure", tests, "passed"))
        self.assertNotIn("template", report.test_headline("failure", tests, "failed"))
        self.assertIn("template installs", report.test_headline("success", tests))

    def test_coverage_row_has_its_own_result(self):
        low, full = report.Coverage(75, 3, 4), report.Coverage(100, 10, 10)
        self.assertEqual(report.coverage_result("failure", full, 95), "success")
        self.assertEqual(report.coverage_result("failure", low, 95), "failure")
        self.assertEqual(report.coverage_result("failure", None, 95), "failure")
        self.assertEqual(report.coverage_result("cancelled", None, 95), "cancelled")
        self.assertEqual(report.coverage_headline(low, 95), "75% (3 of 4 lines) · needs 95%")

    def test_details_start_with_a_capital(self):
        stage = report.Stage("Static Analysis", "success", "no open CodeQL alerts")
        self.assertIn("| No open CodeQL alerts |", report.render_summary([stage], True, "https://run", "abc1234"))

    def test_smoke_headline_leads_the_container_scan(self):
        self.assertEqual(report.smoke_headline({"scan": {"outputs": {"smoke": "passed"}}}), "image starts and answers · ")
        self.assertEqual(report.smoke_headline({"scan": {"outputs": {"smoke": "failed"}}}), "image didn't start or answer · ")
        self.assertEqual(report.smoke_headline({}), "")

    def test_dependency_headline_when_skipped(self):
        self.assertEqual(report.dependency_headline({"dependencies": {"result": "skipped"}}), "didn't run")


class CommandTests(unittest.TestCase):
    def setUp(self):
        # The commands print ::error:: lines for the runner. Swallow them here, or the Build job
        # turns every deliberate failure in these tests into a red annotation on the run.
        quiet = mock.patch("sys.stdout", new_callable=io.StringIO)
        quiet.start()
        self.addCleanup(quiet.stop)

    def test_test_command_fails_on_a_failed_run_or_low_coverage(self):
        full = COVERAGE.replace('sequenceCoverage="75"', 'sequenceCoverage="100"')
        with tempfile.TemporaryDirectory() as directory, mock.patch.dict(os.environ, {"GITHUB_STEP_SUMMARY": os.path.join(directory, "summary.md")}):
            write_results(directory, exit_code="1", coverage=full)
            self.assertEqual(report.command_test(directory), 1)
            write_results(directory, exit_code="0", coverage=COVERAGE)
            self.assertEqual(report.command_test(directory), 1)
            write_results(directory, exit_code="0", coverage=full)
            with mock.patch.dict(os.environ, {"GITHUB_OUTPUT": os.path.join(directory, "output")}):
                self.assertEqual(report.command_test(directory), 0)
            with open(os.path.join(directory, "output"), encoding="utf-8") as handle:
                self.assertEqual(handle.read(), "tests=passed\n")
            with mock.patch.dict(os.environ, {"COVERAGE_THRESHOLD": "70"}):
                write_results(directory, exit_code="0", coverage=COVERAGE)
                self.assertEqual(report.command_test(directory), 0)

    def test_test_command_fails_without_coverage(self):
        with tempfile.TemporaryDirectory() as directory, mock.patch.dict(os.environ, {"GITHUB_STEP_SUMMARY": os.path.join(directory, "summary.md")}):
            write_results(directory, exit_code="0", coverage=None)
            self.assertEqual(report.command_test(directory), 1)

    def test_test_command_fails_without_an_exit_code(self):
        with tempfile.TemporaryDirectory() as directory, mock.patch.dict(os.environ, {"GITHUB_STEP_SUMMARY": os.path.join(directory, "summary.md")}):
            write_results(directory, exit_code=None)
            self.assertEqual(report.command_test(directory), 1)

    def test_summary_command_blocks_on_failure_and_posts_one_comment(self):
        needs = {
            "build": {"result": "success", "outputs": {"version": "1"}},
            "test": {"result": "failure", "outputs": {}},
            "analysis": {"result": "skipped", "outputs": {}},
            "dependencies": {"result": "skipped", "outputs": {}},
            "scan": {"result": "skipped", "outputs": {}},
        }
        posted = []

        class FakeGitHub(report.GitHub):
            def jobs(self, run_id):
                return {"Build": "https://example.com/build"}

            def alerts(self, tool, ref):
                return []

            def upsert_comment(self, number, body):
                posted.append((number, body))

        with tempfile.TemporaryDirectory() as directory:
            write_results(directory, exit_code="1")
            environment = {
                "NEEDS": json.dumps(needs),
                "GITHUB_EVENT_NAME": "pull_request",
                "PR_NUMBER": "7",
                "GITHUB_REPOSITORY": "owner/repo",
                "GITHUB_RUN_ID": "99",
                "GITHUB_SHA": "abc1234",
                "GITHUB_STEP_SUMMARY": os.path.join(directory, "summary.md"),
            }
            with mock.patch.dict(os.environ, environment), mock.patch.object(report, "GitHub", FakeGitHub):
                self.assertEqual(report.command_summary(directory), 1)

        self.assertEqual(len(posted), 1)
        number, body = posted[0]
        self.assertEqual(number, "7")
        self.assertIn("Blocked by Test", body)
        self.assertIn("2 passed · 1 failed · 1 skipped", body)
        self.assertIn("**Blocked by Test** (and 4 more): 1 test failed.", body)
        self.assertIn("| [Build](https://example.com/build) | 🟢 Passed |", body)

    def test_summary_command_passes_in_a_copy_with_only_build_and_test(self):
        needs = {key: {"result": "success", "outputs": {}} for key in ("build", "test")}
        needs.update({key: {"result": "skipped", "outputs": {}} for key in ("analysis", "dependencies", "scan")})
        posted = []

        class FakeGitHub(report.GitHub):
            def jobs(self, run_id):
                return {}

            def alerts(self, tool, ref):
                raise AssertionError("no alerts are read when the scans didn't run")

            def upsert_comment(self, number, body):
                posted.append(body)

        with tempfile.TemporaryDirectory() as directory:
            write_results(directory, exit_code="0", coverage=COVERAGE.replace('sequenceCoverage="75"', 'sequenceCoverage="100"'))
            environment = {
                "NEEDS": json.dumps(needs),
                "FULL": "false",
                "GITHUB_EVENT_NAME": "pull_request",
                "PR_NUMBER": "3",
                "GITHUB_REPOSITORY": "someone/copy",
                "GITHUB_RUN_ID": "1",
                "GITHUB_STEP_SUMMARY": os.path.join(directory, "summary.md"),
            }
            with mock.patch.dict(os.environ, environment), mock.patch.object(report, "GitHub", FakeGitHub):
                self.assertEqual(report.command_summary(directory), 0)

        body = posted[0]
        self.assertIn("Ready to merge", body)
        self.assertIn("| Container Scan | ⚪ Not run |", body)
        self.assertIn("⚪ Not run | Runs in the LeanMeanCleanApps repositories only", body)
        self.assertNotIn("template installs", body)

    def test_summary_command_passes_on_main_without_dependency_review(self):
        needs = {key: {"result": "success", "outputs": {}} for key in ("build", "test", "analysis", "scan")}
        needs["dependencies"] = {"result": "skipped", "outputs": {}}

        class FakeGitHub(report.GitHub):
            def jobs(self, run_id):
                return {}

            def alerts(self, tool, ref):
                return []

            def upsert_comment(self, number, body):
                raise AssertionError("no comment outside pull requests")

        with tempfile.TemporaryDirectory() as directory:
            write_results(directory, exit_code="0", coverage=COVERAGE.replace('sequenceCoverage="75"', 'sequenceCoverage="100"'))
            environment = {
                "NEEDS": json.dumps(needs),
                "GITHUB_EVENT_NAME": "push",
                "GITHUB_REF": "refs/heads/main",
                "GITHUB_REPOSITORY": "owner/repo",
                "GITHUB_RUN_ID": "99",
                "GITHUB_STEP_SUMMARY": os.path.join(directory, "summary.md"),
            }
            with mock.patch.dict(os.environ, environment), mock.patch.object(report, "GitHub", FakeGitHub):
                self.assertEqual(report.command_summary(directory), 0)
                with open(os.path.join(directory, "summary.md"), encoding="utf-8") as handle:
                    summary = handle.read()

        self.assertIn("Pipeline passed", summary)
        self.assertNotIn(report.MARKER, summary)
        self.assertIn("| Dependency Review | ⚪ Not run |", summary)


if __name__ == "__main__":
    unittest.main()
