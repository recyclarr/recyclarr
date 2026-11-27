#!/usr/bin/env -S uv run --script
# /// script
# requires-python = ">=3.12"
# ///
"""Comment on the issues and human-authored PRs that a published release ships.

Inputs (environment):
  TAG                tag of the published release
  GH_TOKEN           token with issues and pull-requests write permission
  GITHUB_REPOSITORY  owner/repo, set by Actions
"""

import json
import os
import re
import subprocess
import sys
from typing import Any

SKIP_LABEL = "no-release-comment"

# GitHub's closing keywords, with an optional owner/repo before the issue number.
CLOSING_KEYWORD_RE = re.compile(
    r"\b(?:close[sd]?|fix(?:e[sd])?|resolve[sd]?):?\s+(?:([\w.-]+/[\w.-]+))?#(\d+)",
    re.IGNORECASE,
)

# The fixed page sizes are far above what a single PR carries; raise them if a PR ever exceeds one.
PR_QUERY = """
query($owner: String!, $name: String!, $oid: GitObjectID!) {
  repository(owner: $owner, name: $name) {
    object(oid: $oid) {
      ... on Commit {
        associatedPullRequests(first: 10) {
          nodes {
            number
            mergeCommit { oid }
            author { __typename }
            labels(first: 50) { nodes { name } }
            closingIssuesReferences(first: 50) {
              nodes { number repository { nameWithOwner } }
            }
          }
        }
      }
    }
  }
}
"""


class GhError(Exception):
    pass


def gh(*args: str) -> str:
    result = subprocess.run(["gh", *args], check=False, capture_output=True, text=True)
    if result.returncode != 0:
        raise GhError(result.stderr.strip())
    return result.stdout


def gh_json(*args: str) -> Any:
    return json.loads(gh(*args))


def git(*args: str) -> str:
    return subprocess.run(
        ["git", *args], stdout=subprocess.PIPE, text=True, check=True
    ).stdout


def keyword_issues(messages: str, repo: str) -> set[int]:
    """Issues closed by commits pushed directly to the mainline."""
    return {
        int(number)
        for slug, number in CLOSING_KEYWORD_RE.findall(messages)
        if not slug or slug.lower() == repo.lower()
    }


def released_prs(repo: str, shas: list[str]) -> list[dict[str, Any]]:
    """PRs whose merge commit (squash, rebase tip, or merge) is in the release range."""
    owner, name = repo.split("/")
    prs: dict[int, dict[str, Any]] = {}
    for sha in shas:
        data = gh_json(
            "api",
            "graphql",
            "-f",
            f"query={PR_QUERY}",
            "-f",
            f"owner={owner}",
            "-f",
            f"name={name}",
            "-f",
            f"oid={sha}",
        )
        commit = data["data"]["repository"]["object"] or {}
        for pr in commit.get("associatedPullRequests", {}).get("nodes", []):
            if (pr["mergeCommit"] or {}).get("oid") == sha:
                prs[pr["number"]] = pr
    return list(prs.values())


class Commenter:
    def __init__(self, repo: str, tag: str, release_url: str) -> None:
        self.issues_api = f"repos/{repo}/issues"
        self.marker = f"<!-- release-comment:{tag} -->"
        self.body = f"🎉 This is included in [{tag}]({release_url}).\n\n{self.marker}"
        self.failures: list[int] = []

    def comment_on_issue(self, number: int) -> None:
        try:
            issue = gh_json("api", "--method", "GET", f"{self.issues_api}/{number}")
        except GhError as e:
            if "HTTP 404" in str(e):
                print(
                    f"::warning::#{number}: referenced by a closing keyword but does not exist"
                )
            else:
                self._fail(number, f"could not read issue: {e}")
            return

        # Closing keywords can name a PR; PRs are handled only through their merge commits.
        if "pull_request" in issue:
            return

        if any(label["name"] == SKIP_LABEL for label in issue["labels"]):
            print(f"#{number}: has {SKIP_LABEL}")
            return

        self.comment_once(number)

    def comment_once(self, number: int) -> None:
        try:
            pages = gh_json(
                "api",
                "--method",
                "GET",
                "--paginate",
                "--slurp",
                f"{self.issues_api}/{number}/comments",
            )
            if any(
                self.marker in comment["body"] for page in pages for comment in page
            ):
                print(f"#{number}: already commented")
                return

            gh(
                "api",
                "--method",
                "POST",
                f"{self.issues_api}/{number}/comments",
                "-f",
                f"body={self.body}",
                "--silent",
            )
            print(f"#{number}: commented")
        except GhError as e:
            self._fail(number, str(e))

    def _fail(self, number: int, reason: str) -> None:
        print(f"::error::#{number}: {reason}", file=sys.stderr)
        self.failures.append(number)


def main() -> None:
    tag = os.environ["TAG"]
    repo = os.environ["GITHUB_REPOSITORY"]

    release = gh_json("release", "view", tag, "--repo", repo, "--json", "url,isDraft")
    if release["isDraft"]:
        print(f"::error::{tag} is a draft release", file=sys.stderr)
        sys.exit(1)

    previous = git("describe", "--tags", "--abbrev=0", f"{tag}^").strip()
    commit_range = f"{previous}..{tag}"
    shas = git("rev-list", commit_range).split()
    print(f"{tag}: {len(shas)} commits since {previous}")

    prs = released_prs(repo, shas)
    issues = keyword_issues(git("log", "--format=%B", commit_range), repo)
    issues |= {
        ref["number"]
        for pr in prs
        for ref in pr["closingIssuesReferences"]["nodes"]
        if ref["repository"]["nameWithOwner"] == repo
    }

    commenter = Commenter(repo, tag, release["url"])
    for number in sorted(issues):
        commenter.comment_on_issue(number)

    for pr in sorted(prs, key=lambda pr: pr["number"]):
        is_bot = (pr["author"] or {}).get("__typename") == "Bot"
        skipped = any(label["name"] == SKIP_LABEL for label in pr["labels"]["nodes"])
        if not is_bot and not skipped:
            commenter.comment_once(pr["number"])

    if commenter.failures:
        print(f"::error::Failed items: {commenter.failures}", file=sys.stderr)
        sys.exit(1)


if __name__ == "__main__":
    main()
