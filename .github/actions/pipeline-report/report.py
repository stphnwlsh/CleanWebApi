#!/usr/bin/env python3
"""Pipeline report for the Build Pipeline.

Two commands, both standard library only:

  report.py test --results DIR
      Reads the .trx test results, the OpenCover coverage report and the test exit code
      exported by the Dockerfile's results stage. Writes the Test job summary and fails
      if any test failed or line coverage is below COVERAGE_THRESHOLD (95 by default).

  report.py summary --results DIR
      Reads every stage's result from the NEEDS environment variable (toJSON(needs)),
      the same test results, and the open code scanning alerts. Writes one table to the
      run summary and, on a pull request, keeps a single comment up to date with it.
      Exits non-zero if any stage blocks the merge.
"""

from __future__ import annotations

import argparse
import glob
import json
import os
import sys
import urllib.error
import urllib.parse
import urllib.request
import xml.etree.ElementTree as ET
from dataclasses import dataclass, field

MARKER = "<!-- build-pipeline-report -->"
TRX_NS = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}


# ---------------------------------------------------------------------------
# Test results and coverage
# ---------------------------------------------------------------------------


@dataclass
class FailedTest:
    name: str
    message: str


@dataclass
class TestResults:
    passed: int = 0
    failed: int = 0
    skipped: int = 0
    failures: list[FailedTest] = field(default_factory=list)

    @property
    def total(self) -> int:
        return self.passed + self.failed + self.skipped


@dataclass
class FileCoverage:
    path: str
    covered: int
    total: int

    @property
    def percent(self) -> float:
        return 100 * self.covered / self.total if self.total else 100.0


@dataclass
class Coverage:
    percent: float
    covered: int
    total: int
    files: list[FileCoverage] = field(default_factory=list)


def parse_trx(paths: list[str]) -> TestResults:
    results = TestResults()
    for path in paths:
        root = ET.parse(path).getroot()
        for result in root.iterfind(".//t:UnitTestResult", TRX_NS):
            outcome = result.get("outcome", "")
            if outcome == "Passed":
                results.passed += 1
            elif outcome in ("NotExecuted", "Inconclusive"):
                results.skipped += 1
            else:
                results.failed += 1
                message = result.findtext(".//t:ErrorInfo/t:Message", default="", namespaces=TRX_NS)
                results.failures.append(FailedTest(result.get("testName", "unknown"), first_line(message)))
    return results


def parse_coverage(path: str, root_marker: str = "/sln/") -> Coverage:
    root = ET.parse(path).getroot()
    summary = root.find("Summary")
    total = int(summary.get("numSequencePoints", "0")) if summary is not None else 0
    covered = int(summary.get("visitedSequencePoints", "0")) if summary is not None else 0
    percent = float(summary.get("sequenceCoverage", "0")) if summary is not None else 0.0

    lines: dict[tuple[str, int], int] = {}
    for module in root.iter("Module"):
        files = {f.get("uid"): f.get("fullPath", "") for f in module.iter("File")}
        for method in module.iter("Method"):
            ref = method.find("FileRef")
            default = ref.get("uid") if ref is not None else None
            for point in method.iter("SequencePoint"):
                full = files.get(point.get("fileid", default))
                if not full:
                    continue
                relative = full.replace("\\", "/").split(root_marker, 1)[-1]
                key = (relative, int(point.get("sl", "0")))
                lines[key] = max(lines.get(key, 0), int(point.get("vc", "0")))

    per_file: dict[str, list[int]] = {}
    for (relative, _), hits in lines.items():
        counts = per_file.setdefault(relative, [0, 0])
        counts[1] += 1
        counts[0] += hits > 0

    files = [FileCoverage(p, c, t) for p, (c, t) in per_file.items()]
    files.sort(key=lambda f: (f.percent, f.path))
    return Coverage(percent, covered, total, files)


def read_results(directory: str) -> tuple[TestResults | None, Coverage | None, int | None]:
    trx = sorted(glob.glob(os.path.join(directory, "*.trx")))
    tests = parse_trx(trx) if trx else None
    coverage_path = os.path.join(directory, "coverage.opencover.xml")
    coverage = parse_coverage(coverage_path) if os.path.isfile(coverage_path) else None
    exit_path = os.path.join(directory, "exit-code")
    exit_code = None
    if os.path.isfile(exit_path):
        with open(exit_path, encoding="utf-8") as handle:
            text = handle.read().strip()
        exit_code = int(text) if text.lstrip("-").isdigit() else None
    return tests, coverage, exit_code


def render_tests(tests: TestResults | None, coverage: Coverage | None, threshold: float, worst: int = 10) -> str:
    out = ["### Tests", ""]
    if tests is None:
        out.append("No test results were produced. Check the Test step's log for a build or restore error.")
    else:
        out.append(f"**{tests.passed}** passed, **{tests.failed}** failed, **{tests.skipped}** skipped, out of {tests.total}.")
        if tests.failures:
            out += ["", "| Failed test | Message |", "| --- | --- |"]
            for failure in tests.failures[:50]:
                out.append(f"| `{cell(failure.name)}` | {cell(failure.message)} |")
            if len(tests.failures) > 50:
                out.append(f"| and {len(tests.failures) - 50} more | |")

    out += ["", "### Coverage", ""]
    if coverage is None:
        out.append("No coverage report was produced.")
    else:
        verdict = "meets" if coverage.percent >= threshold else "is below"
        out.append(f"Line coverage is **{format_percent(coverage.percent)}** ({coverage.covered} of {coverage.total} lines), which {verdict} the {format_percent(threshold)} threshold.")
        lowest = [f for f in coverage.files if f.percent < 100][:worst]
        if lowest:
            out += ["", "| Least covered files | Lines | Coverage |", "| --- | --- | --- |"]
            for f in lowest:
                out.append(f"| `{cell(f.path)}` | {f.covered} of {f.total} | {format_percent(f.percent)} |")
        else:
            out.append("")
            out.append("Every line is covered. Show-off.")
    return "\n".join(out) + "\n"


# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------


@dataclass
class Stage:
    name: str
    result: str  # success, failure, cancelled, skipped, or "n/a" when the stage doesn't apply
    headline: str
    link: str = ""
    reason: str = ""  # what the verdict line says when this stage fails; the headline if empty

    @property
    def blocks(self) -> bool:
        return self.result not in ("success", "n/a")


ICONS = {
    "success": "🟢 Passed",
    "failure": "🔴 Failed",
    "cancelled": "⚫ Cancelled",
    "skipped": "⚪ Skipped",
    "n/a": "⚪ Not run",
}


def verdict(stages: list[Stage], pull_request: bool) -> str:
    blockers = [s for s in stages if s.blocks]
    if not blockers:
        return "✅ **Ready to merge.** Every stage passed." if pull_request else "✅ **Pipeline passed.** Every stage passed."
    first = blockers[0]
    reason = (first.reason or first.headline) if first.result == "failure" else f"the stage was {first.result}"
    more = f" (and {len(blockers) - 1} more)" if len(blockers) > 1 else ""
    return f"❌ **Blocked by {first.name}**{more}: {reason}."


def render_summary(stages: list[Stage], pull_request: bool, run_url: str, sha: str) -> str:
    out = [
        MARKER,
        "## Build Pipeline",
        "",
        verdict(stages, pull_request),
        "",
        "| Stage | Result | Details |",
        "| --- | --- | --- |",
    ]
    for stage in stages:
        name = f"[{stage.name}]({stage.link})" if stage.link else stage.name
        out.append(f"| {name} | {ICONS.get(stage.result, stage.result)} | {cell(SEPARATOR.join(capitalise(part) for part in stage.headline.split(SEPARATOR)))} |")
    short = sha[:7] if sha else "unknown"
    out += ["", f"<sub>Commit `{short}` · [Full run]({run_url})</sub>"]
    return "\n".join(out) + "\n"


def capitalise(text: str) -> str:
    """Each part of a detail starts with a capital or a number; the verdict line keeps them lower case."""
    return text[:1].upper() + text[1:]


SEPARATOR = " · "


def stage_result(needs: dict, key: str) -> str:
    return needs.get(key, {}).get("result", "skipped")


def build_headline(needs: dict) -> str:
    outputs = needs.get("build", {}).get("outputs", {})
    version = outputs.get("version")
    size = outputs.get("package_size")
    parts = []
    if version:
        parts.append(f"version `{version}`")
    if size and size.isdigit():
        parts.append(f"template package {int(size) / 1024:.0f} KB")
    if stage_result(needs, "build") == "failure":
        return "the build, format check or template pack failed"
    return SEPARATOR.join(parts) or "built"


def test_headline(result: str, tests: TestResults | None, tests_outcome: str = "", full: bool = True) -> str:
    parts = []
    if tests is not None:
        parts.append(f"{tests.passed} passed{SEPARATOR}{tests.failed} failed{SEPARATOR}{tests.skipped} skipped")
    if result == "success" and full:
        parts.append("template installs, builds and passes its tests")
    elif result == "failure" and tests_outcome == "passed":
        parts.append("the generated template failed to build or test")
    return SEPARATOR.join(parts) or "no test results"


def coverage_result(test_result: str, coverage: Coverage | None, threshold: float) -> str:
    if coverage is None:
        return "failure" if test_result in ("success", "failure") else test_result
    return "success" if coverage.percent >= threshold else "failure"


def coverage_headline(coverage: Coverage | None, threshold: float) -> str:
    if coverage is None:
        return "no coverage report"
    return f"{format_percent(coverage.percent)} ({coverage.covered} of {coverage.total} lines){SEPARATOR}needs {format_percent(threshold)}"


def alerts_headline(alerts: list[dict] | None, what: str, passed: bool = False) -> str:
    if alerts is None:
        return f"{what} ran; open alerts couldn't be read"
    if not alerts:
        return f"no open {what} alerts"
    counts: dict[str, int] = {}
    for alert in alerts:
        rule = alert.get("rule", {})
        level = rule.get("security_severity_level") or rule.get("severity") or "unknown"
        counts[level] = counts.get(level, 0) + 1
    order = ["critical", "high", "medium", "low", "error", "warning", "note", "unknown"]
    text = ", ".join(f"{counts[k]} {k}" for k in order if k in counts)
    blocking = ", none blocking" if passed else ""
    return f"{len(alerts)} open {what} alert{'s' if len(alerts) != 1 else ''}{blocking}: {text}"


def smoke_headline(needs: dict) -> str:
    """Leads the Container Scan details: whether the image started and answered a request."""
    smoke = needs.get("scan", {}).get("outputs", {}).get("smoke", "")
    if smoke == "passed":
        return f"image starts and answers{SEPARATOR}"
    if smoke == "failed":
        return f"image didn't start or answer{SEPARATOR}"
    return ""


def dependency_headline(needs: dict) -> str:
    if stage_result(needs, "dependencies") in ("skipped", "cancelled"):
        return "didn't run"
    outputs = needs.get("dependencies", {}).get("outputs", {})
    vulnerable = count_json(outputs.get("vulnerable"))
    licences = count_json(outputs.get("licences"))
    if vulnerable is None and licences is None:
        return "no dependency changes to review" if stage_result(needs, "dependencies") == "success" else "review failed"
    return f"{vulnerable or 0} vulnerable package{'s' if vulnerable != 1 else ''}{SEPARATOR}{licences or 0} licence problem{'s' if licences != 1 else ''}"


NOT_RUN_OUTSIDE = "runs in the LeanMeanCleanApps repositories only; see the ci-for-your-solution skill to add it"


def full_pipeline() -> bool:
    """False in a copy outside our organisation, where only build and test run."""
    return os.environ.get("FULL", "true").strip().lower() != "false"


def outside_stage(name: str) -> Stage:
    return Stage(name, "n/a", NOT_RUN_OUTSIDE)


def count_ignored(path: str) -> int:
    if not os.path.isfile(path):
        return 0
    with open(path, encoding="utf-8") as handle:
        return sum(1 for line in handle if line.strip() and not line.lstrip().startswith("#"))


# ---------------------------------------------------------------------------
# GitHub API
# ---------------------------------------------------------------------------


class GitHub:
    def __init__(self, token: str, api: str, repository: str):
        self.token = token
        self.api = api.rstrip("/")
        self.repository = repository

    def request(self, method: str, path: str, body: dict | None = None):
        url = path if path.startswith("http") else f"{self.api}/repos/{self.repository}/{path.lstrip('/')}"
        data = json.dumps(body).encode() if body is not None else None
        request = urllib.request.Request(url, data=data, method=method)
        request.add_header("Accept", "application/vnd.github+json")
        request.add_header("X-GitHub-Api-Version", "2022-11-28")
        if self.token:
            request.add_header("Authorization", f"Bearer {self.token}")
        if data is not None:
            request.add_header("Content-Type", "application/json")
        with urllib.request.urlopen(request, timeout=30) as response:
            text = response.read().decode()
            return json.loads(text) if text else None

    def paged(self, path: str) -> list:
        items, page = [], 1
        separator = "&" if "?" in path else "?"
        while True:
            batch = self.request("GET", f"{path}{separator}per_page=100&page={page}")
            if isinstance(batch, dict):
                batch = batch.get("jobs", [])
            items += batch
            if len(batch) < 100:
                return items
            page += 1

    def jobs(self, run_id: str) -> dict[str, str]:
        jobs = self.paged(f"actions/runs/{run_id}/jobs?filter=latest")
        return {job["name"]: job["html_url"] for job in jobs}

    def alerts(self, tool: str, ref: str) -> list[dict]:
        query = urllib.parse.urlencode({"tool_name": tool, "state": "open", "ref": ref})
        return self.paged(f"code-scanning/alerts?{query}")

    def upsert_comment(self, number: str, body: str) -> None:
        comments = self.paged(f"issues/{number}/comments")
        mine = [c for c in comments if MARKER in (c.get("body") or "")]
        if mine:
            self.request("PATCH", f"issues/comments/{mine[-1]['id']}", {"body": body})
        else:
            self.request("POST", f"issues/{number}/comments", {"body": body})


def safely(description: str, action, default=None):
    try:
        return action()
    except (urllib.error.URLError, OSError, ValueError, KeyError) as error:
        print(f"::warning::Couldn't {description}: {error}")
        return default


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------


def first_line(text: str) -> str:
    text = (text or "").strip()
    line = text.splitlines()[0] if text else ""
    return line[:200] + ("…" if len(line) > 200 else "")


def cell(text: str) -> str:
    return (text or "").replace("|", "\\|").replace("\n", " ")


def format_percent(value: float) -> str:
    return f"{value:.0f}%" if float(value).is_integer() else f"{value:.2f}%"


def count_json(value: str | None) -> int | None:
    if not value:
        return None
    try:
        parsed = json.loads(value)
    except ValueError:
        return None
    return len(parsed) if isinstance(parsed, list) else None


def write_output(name: str, value: str) -> None:
    path = os.environ.get("GITHUB_OUTPUT")
    if path:
        with open(path, "a", encoding="utf-8") as handle:
            handle.write(f"{name}={value}\n")


def write_step_summary(text: str) -> None:
    path = os.environ.get("GITHUB_STEP_SUMMARY")
    if path:
        with open(path, "a", encoding="utf-8") as handle:
            handle.write(text + "\n")
    print(text)


def threshold() -> float:
    return float(os.environ.get("COVERAGE_THRESHOLD", "95"))


# ---------------------------------------------------------------------------
# Commands
# ---------------------------------------------------------------------------


def command_test(results: str) -> int:
    tests, coverage, exit_code = read_results(results)
    write_step_summary(render_tests(tests, coverage, threshold()))
    failed = False
    if exit_code is None:
        print("::error::The test stage didn't record an exit code")
        failed = True
    elif exit_code != 0:
        if tests is not None and tests.failed:
            print(f"::error::{tests.failed} test{'s' if tests.failed != 1 else ''} failed")
        else:
            print(f"::error::dotnet test exited with {exit_code}")
        failed = True
    if coverage is None:
        print("::error::No coverage report was produced")
        failed = True
    elif coverage.percent < threshold():
        print(f"::error::Line coverage {format_percent(coverage.percent)} is below {format_percent(threshold())}")
        failed = True
    write_output("tests", "failed" if failed else "passed")
    return 1 if failed else 0


def command_summary(results: str) -> int:
    needs = json.loads(os.environ.get("NEEDS", "{}") or "{}")
    event = os.environ.get("GITHUB_EVENT_NAME", "")
    pull_request = event == "pull_request"
    number = os.environ.get("PR_NUMBER", "")
    repository = os.environ.get("GITHUB_REPOSITORY", "")
    server = os.environ.get("GITHUB_SERVER_URL", "https://github.com")
    run_id = os.environ.get("GITHUB_RUN_ID", "")
    run_url = f"{server}/{repository}/actions/runs/{run_id}"
    sha = os.environ.get("GITHUB_SHA", "")
    if pull_request:
        event_path = os.environ.get("GITHUB_EVENT_PATH")
        if event_path and os.path.isfile(event_path):
            with open(event_path, encoding="utf-8") as handle:
                sha = json.load(handle).get("pull_request", {}).get("head", {}).get("sha", sha)
    ref = f"refs/pull/{number}/merge" if pull_request and number else os.environ.get("GITHUB_REF", "")

    github = GitHub(os.environ.get("GITHUB_TOKEN", ""), os.environ.get("GITHUB_API_URL", "https://api.github.com"), repository)
    jobs = safely("read the run's jobs", lambda: github.jobs(run_id), {}) if run_id else {}
    tests, coverage, _ = read_results(results)
    security = f"{server}/{repository}/security/code-scanning?query=" + urllib.parse.quote(
        f"is:open {'pr:' + number if pull_request and number else 'branch:' + ref.removeprefix('refs/heads/')}"
    )

    def job_link(*names: str) -> str:
        for name in names:
            if name in jobs:
                return jobs[name]
        return run_url

    analysis = stage_result(needs, "analysis")
    scan = stage_result(needs, "scan")
    codeql = safely("read CodeQL alerts", lambda: github.alerts("CodeQL", ref)) if analysis == "success" or analysis == "failure" else []
    trivy = safely("read Trivy alerts", lambda: github.alerts("Trivy", ref)) if scan == "success" or scan == "failure" else []
    ignored = count_ignored(".trivyignore")

    test = stage_result(needs, "test")
    full = full_pipeline()
    stages = [
        Stage("Build", stage_result(needs, "build"), build_headline(needs), job_link("Build")),
        Stage(
            "Test",
            test,
            test_headline(test, tests, needs.get("test", {}).get("outputs", {}).get("tests", ""), full),
            job_link("Test"),
            f"{tests.failed} test{'s' if tests.failed != 1 else ''} failed" if tests and tests.failed else "",
        ),
        Stage(
            "Coverage",
            coverage_result(test, coverage, threshold()),
            coverage_headline(coverage, threshold()) if coverage or test in ("success", "failure") else "didn't run",
            job_link("Test"),
            f"line coverage {format_percent(coverage.percent)} is below {format_percent(threshold())}" if coverage else "",
        ),
        Stage("Static Analysis", analysis, alerts_headline(codeql, "CodeQL", analysis == "success") if analysis in ("success", "failure") else "didn't run", security + urllib.parse.quote(" tool:CodeQL")),
        Stage(
            "Dependency Review",
            stage_result(needs, "dependencies") if pull_request else "n/a",
            dependency_headline(needs) if pull_request else "runs on pull requests; NuGetAudit checks every build",
            job_link("Dependency Review") if pull_request else "",
        ),
        Stage(
            "Container Scan",
            scan,
            smoke_headline(needs) + (alerts_headline(trivy, "Trivy", scan == "success") + (f"{SEPARATOR}{ignored} ignored in .trivyignore" if ignored else "")) if scan in ("success", "failure") else "didn't run",
            security + urllib.parse.quote(" tool:Trivy"),
            "the image didn't start or answer a request" if needs.get("scan", {}).get("outputs", {}).get("smoke") == "failed" else "",
        ),
    ]
    if not full:
        stages = [outside_stage(s.name) if s.name in ("Static Analysis", "Dependency Review", "Container Scan") else s for s in stages]

    body = render_summary(stages, pull_request, run_url, sha)
    write_step_summary(body.replace(MARKER + "\n", ""))
    if pull_request and number:
        safely("post the pull request comment (pull requests from forks can't be commented on)", lambda: github.upsert_comment(number, body))

    blockers = [s.name for s in stages if s.blocks]
    if blockers:
        print(f"::error::Blocked by {', '.join(blockers)}")
        return 1
    return 0


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("command", choices=["test", "summary"])
    parser.add_argument("--results", default="results", help="Directory holding the .trx files, coverage.opencover.xml and exit-code")
    args = parser.parse_args(argv)
    return command_test(args.results) if args.command == "test" else command_summary(args.results)


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
