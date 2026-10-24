#!/bin/bash
#
# Tests for release.sh. Plain bash: builds a throw-away git repo (with a bare "origin"), plants tags and
# asserts the computed tags with --dry-run, plus one real tag+push into the bare origin.
#
# Usage: ./release.test.sh

set -uo pipefail

SCRIPT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/release.sh"
PACKAGE=$(sed -n 's/^PACKAGE="\(.*\)"$/\1/p' "$SCRIPT")
unset GITHUB_OUTPUT

WORK=$(mktemp -d)
trap 'rm -rf "$WORK"' EXIT

failures=0
passes=0

pass() { passes=$((passes + 1)); echo "PASS: $1"; }
fail() { failures=$((failures + 1)); echo "FAIL: $1"; }

# expect_tag <description> <expected tag suffix> <release.sh args...>
expect_tag() {
    local description=$1 expected="$PACKAGE/$2"
    shift 2
    local actual status
    actual=$("$SCRIPT" "$@" 2> "$WORK/stderr")
    status=$?
    if [ "$status" -eq 0 ] && [ "$actual" = "$expected" ]; then
        pass "$description -> $actual"
    else
        fail "$description: expected '$expected' exit 0, got '$actual' exit $status"
        sed 's/^/    /' "$WORK/stderr"
    fi
}

# expect_exit <description> <expected exit code> <release.sh args...>
expect_exit() {
    local description=$1 expected=$2
    shift 2
    local status
    "$SCRIPT" "$@" > /dev/null 2> "$WORK/stderr" < /dev/null
    status=$?
    if [ "$status" -eq "$expected" ]; then
        pass "$description (exit $status)"
    else
        fail "$description: expected exit $expected, got $status"
        sed 's/^/    /' "$WORK/stderr"
    fi
}

git init -q --bare "$WORK/origin.git"
git init -q -b master "$WORK/repo"
cd "$WORK/repo" || exit 1
git config user.email "test@example.com"
git config user.name "release.test.sh"
git config commit.gpgsign false
git config tag.gpgsign false
git remote add origin "$WORK/origin.git"
echo "a" > file.txt
git add file.txt
git commit -q -m "initial"

# No tag yet: INITIAL_VERSION is the current version.
initial=$(sed -n 's/^INITIAL_VERSION="\(.*\)"$/\1/p' "$SCRIPT")
IFS='.' read -r ma mi pa bu <<< "$initial"
expect_tag "no tags: --patch bumps INITIAL_VERSION" "$ma.$mi.$((pa + 1)).$((bu + 1))" --patch --ci --dry-run

git tag -a "$PACKAGE/1.9.9.76" -m t
git tag -a "$PACKAGE/1.13.0.71" -m t
git tag -a "$PACKAGE/1.14.0.72-preview.1" -m t
git tag -a "$PACKAGE/2.0.0.99-preview.1" -m t

"$SCRIPT" --dry-run --ci > /dev/null 2> "$WORK/stderr"
if grep -q "Current version: 1.13.0.71" "$WORK/stderr"; then
    pass "current version ignores -preview tags and sorts by version (1.13.0.71)"
else
    fail "current version: $(grep 'Current version' "$WORK/stderr")"
fi

expect_tag "default bump is patch" "1.13.1.72" --ci --dry-run
expect_tag "--patch" "1.13.1.72" --patch --ci --dry-run
expect_tag "--minor" "1.14.0.72" --minor --ci --dry-run
expect_tag "--major keeps build+1" "2.0.0.72" --major --ci --dry-run
expect_tag "--version" "1.20.0.80" --version 1.20.0.80 --ci --dry-run
expect_tag "--preview --minor counts existing previews" "1.14.0.72-preview.2" --preview --minor --ci --dry-run
expect_tag "--preview --patch, first preview" "1.13.1.72-preview.1" --preview --patch --ci --dry-run
expect_tag "--dry-run without --ci needs no prompt" "1.14.0.72" --minor --dry-run

expect_exit "existing tag is refused" 1 --version 1.13.0.71 --ci --dry-run
expect_exit "malformed --version is refused" 1 --version 1.2.3 --ci --dry-run
expect_exit "unknown option is refused" 1 --bogus

echo "b" >> file.txt
expect_exit "--ci on a dirty tree" 1 --minor --ci --dry-run
expect_exit "--preview --ci on a dirty tree" 1 --preview --minor --ci --dry-run
git checkout -q -- file.txt

git checkout -q -b feature/x
expect_exit "release off master (--ci)" 1 --minor --ci --dry-run
expect_exit "release off master (interactive)" 1 --minor --dry-run
expect_tag "--preview off master" "1.14.0.72-preview.2" --preview --minor --ci --dry-run
git checkout -q master

# A real run: tags, pushes to origin, writes $GITHUB_OUTPUT.
out="$WORK/github_output"
: > "$out"
if GITHUB_OUTPUT="$out" "$SCRIPT" --minor --ci > /dev/null 2> "$WORK/stderr"; then
    pass "--minor --ci tags and pushes"
else
    fail "--minor --ci real run"
    sed 's/^/    /' "$WORK/stderr"
fi
if git --git-dir="$WORK/origin.git" rev-parse -q --verify "refs/tags/$PACKAGE/1.14.0.72" > /dev/null; then
    pass "tag $PACKAGE/1.14.0.72 is on origin"
else
    fail "tag $PACKAGE/1.14.0.72 is not on origin"
fi
expected_output=$(printf 'version=1.14.0.72\ntag=%s/1.14.0.72\nprevious=1.13.0.71' "$PACKAGE")
if [ "$(cat "$out")" = "$expected_output" ]; then
    pass "\$GITHUB_OUTPUT has version, tag and previous"
else
    fail "\$GITHUB_OUTPUT content: $(cat "$out")"
fi
expect_tag "next --patch after the release" "1.14.1.73" --patch --ci --dry-run

echo ""
echo "$passes passed, $failures failed"
[ "$failures" -eq 0 ]
