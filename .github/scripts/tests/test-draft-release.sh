#!/usr/bin/env bash
# Hermetic acceptance tests for draft-release.sh (spec #119 §8).
# Runs the real script against the fake gh in tests/bin — no network, no
# real releases. Assertions derive from the SPEC's acceptance criteria and
# its §2 invariant ("exactly one draft per unreleased tag"), never from the
# script's internals.
#
# Usage: bash .github/scripts/tests/test-draft-release.sh
# Exit 0 = all scenarios green.
set -u
DIR="$(cd "$(dirname "$0")" && pwd)"
SCRIPT="$DIR/../draft-release.sh"
TAG="v0.15.0"
BEGIN='<!-- auto-release-notes:start -->'

PASS=0; FAIL=0; FAILED_NAMES=""

new_state() { # $1 = fixture json array; sets FAKE_STATE for one scenario
  FAKE_STATE="$(mktemp -d)"
  printf '%s' "$1" > "$FAKE_STATE/state.json"
  : > "$FAKE_STATE/calls.log"
  export FAKE_STATE
  unset FAKE_FAIL_FIRST_LIST
}

run_script() {
  PATH="$DIR/bin:$PATH" REPO="fake/r" HEAD_SHA="deadbeefdeadbeef" GH_TOKEN="x" \
    bash "$SCRIPT" "$TAG" > "$FAKE_STATE/out.log" 2>&1
  RC=$?
}

# helpers over the fake state
draft_ids_for_tag() {
  jq -r --arg t "$TAG" '[.[] | select(.draft and (.tag_name==$t or .name==$t))] | length' "$FAKE_STATE/state.json"
}
draft_body() { # $1 = id
  jq -r --argjson i "$1" '[.[] | select(.id==$i)][0].body // ""' "$FAKE_STATE/state.json"
}
body_of() { # $1 = id (any draft or published)
  jq -r --argjson i "$1" '[.[] | select(.id==$i)][0].body // ""' "$FAKE_STATE/state.json"
}
count_created() { grep -c "gh release create" "$FAKE_STATE/calls.log" || true; }
count_delete()  { grep -c "gh api -X DELETE" "$FAKE_STATE/calls.log" || true; }

check() { # $1 = scenario, $2 = condition already evaluated ($?), $3 = description
  if [ "$2" -eq 0 ]; then
    PASS=$((PASS+1)); printf '  ok   %s\n' "$3"
  else
    FAIL=$((FAIL+1)); FAILED_NAMES="$FAILED_NAMES\n  FAIL [$1] $3"
    printf '  FAIL %s\n' "$3"
  fi
}

echo "== AC-1 good-path: no draft exists -> create exactly one, notes wrapped in markers =="
new_state '[]'
run_script
check AC1a $([ $RC -eq 0 ] && [ "$(draft_ids_for_tag)" = "1" ] && [ "$(count_created)" = "1" ]; echo $?) \
  "one draft created for $TAG"
b="$(draft_body 100)"
echo "$b" | grep -qF "$BEGIN" && echo "$b" | grep -qF "generated for $TAG"
check AC1b $? "created draft's notes wrapped in the auto-marker region"
rm -rf "$FAKE_STATE"

echo "== AC-2 good-path: draft exists (GitHub untagged-* rename quirk) -> refreshed in place, notes cover new PRs, zero creates =="
# Build the fixture with both markers via jq -n to dodge quoting.
fixture="$(jq -nc --arg begin "$BEGIN" --arg end '<!-- auto-release-notes:end -->' '
  [ {"id":30,"tag_name":"v0.14.0","name":"v0.14.0","draft":false,"body":"PUBLISHED FINAL NOTES","created_at":"2026-01-01T00:00:00Z","html_url":"u30"},
    {"id":51,"tag_name":"untagged-a1b2","name":"v0.15.0","draft":true,
     "body": ($begin + "\n## What'"'"'s Changed\n* stale entry from an older run\n" + $end),
     "created_at":"2026-06-01T00:00:00Z","html_url":"u51"} ]')"
new_state "$fixture"
run_script
check AC2a $([ $RC -eq 0 ] && [ "$(draft_ids_for_tag)" = "1" ] && [ "$(count_created)" = "0" ]; echo $?) \
  "count for $TAG stays 1, no create call, exit 0"
b="$(draft_body 51)"
echo "$b" | grep -qF "PR 201 title" && echo "$b" | grep -qF "Full Changelog"
check AC2b $? "refreshed notes include the newly merged PR and the Full Changelog footer"
echo "$b" | grep -qF "stale entry from an older run"; rc=$?
[ $rc -ne 0 ]; check AC2c $? "stale auto notes were replaced (auto region regenerated)"
echo "$b" | grep -qF "PUBLISHED FINAL NOTES"; rc=$?
[ $rc -ne 0 ]; check AC2d $? "published release untouched"
rm -rf "$FAKE_STATE"

echo "== AC-3 good-path: PO scratch text outside markers survives the refresh =="
fixture="$(jq -nc --arg begin "$BEGIN" --arg end '<!-- auto-release-notes:end -->' '
  [ {"id":30,"tag_name":"v0.14.0","name":"v0.14.0","draft":false,"body":"PUBLISHED","created_at":"2026-01-01T00:00:00Z","html_url":"u30"},
    {"id":51,"tag_name":"untagged-a1b2","name":"v0.15.0","draft":true,
     "body": ($begin + "\nold auto\n" + $end + "\n\nMY PO LINE: hold until QA signs off\n"),
     "created_at":"2026-06-01T00:00:00Z","html_url":"u51"} ]')"
new_state "$fixture"
run_script
b="$(draft_body 51)"
echo "$b" | grep -qF "MY PO LINE: hold until QA signs off" && echo "$b" | grep -qF "PR 201 title"
check AC3 $? "PO-added line preserved AND notes refreshed"
rm -rf "$FAKE_STATE"

echo "== AC-2 boundary (the live incident): several stacked drafts -> absorb PO text, keep newest, delete extras =="
fixture="$(jq -nc --arg begin "$BEGIN" --arg end '<!-- auto-release-notes:end -->' '
  [ {"id":30,"tag_name":"v0.14.0","name":"v0.14.0","draft":false,"body":"PUBLISHED","created_at":"2026-01-01T00:00:00Z","html_url":"u30"},
    {"id":40,"tag_name":"untagged-aaaa","name":"v0.15.0","draft":true,
     "body":"LEGACY PO SCRATCH: release with the trio notes\n(no markers - created by the old step)",
     "created_at":"2026-10-27T22:54:00Z","html_url":"u40"},
    {"id":41,"tag_name":"untagged-bbbb","name":"v0.15.0","draft":true,
     "body": ($begin + "\nnewer auto\n" + $end + "\nNEWER PO LINE\n"),
     "created_at":"2026-10-27T23:00:00Z","html_url":"u41"} ]')"
new_state "$fixture"
run_script
check B1a $([ $RC -eq 0 ] && [ "$(draft_ids_for_tag)" = "1" ] && [ "$(count_created)" = "0" ]; echo $?) \
  "stacked drafts deduped to exactly one, exit 0"
b="$(draft_body 41)"
echo "$b" | grep -qF "LEGACY PO SCRATCH" && echo "$b" | grep -qF "NEWER PO LINE" && echo "$b" | grep -qF "Preserved from duplicate draft 40"
check B1b $? "survivor holds BOTH drafts' PO text under a preservation header"
[ "$(count_delete)" = "1" ]
check B1c $? "exactly one delete call (the duplicate), survivor kept"
rm -rf "$FAKE_STATE"

echo "== boundary: version already published -> published release untouched, stray draft swept =="
new_state '[
 {"id":30,"tag_name":"v0.15.0","name":"v0.15.0","draft":false,"body":"PUBLISHED FINAL NOTES","created_at":"2026-01-01T00:00:00Z","html_url":"u30"},
 {"id":31,"tag_name":"untagged-c","name":"v0.15.0","draft":true,"body":"stray","created_at":"2026-10-27T00:00:00Z","html_url":"u31"}
]'
run_script
check B2a $([ $RC -eq 0 ] && [ "$(draft_ids_for_tag)" = "0" ] && [ "$(body_of 30)" = "PUBLISHED FINAL NOTES" ] && [ "$(count_created)" = "0" ]; echo $?) \
  "published body byte-identical, stray draft deleted, no create (publish stays the PO's click)"
rm -rf "$FAKE_STATE"

echo "== AC-2 bad-path: transient gh api failure during lookup must NOT hide the existing draft and create a duplicate =="
fixture="$(jq -nc --arg begin "$BEGIN" --arg end '<!-- auto-release-notes:end -->' '
  [ {"id":30,"tag_name":"v0.14.0","name":"v0.14.0","draft":false,"body":"PUBLISHED","created_at":"2026-01-01T00:00:00Z","html_url":"u30"},
    {"id":51,"tag_name":"untagged-a1b2","name":"v0.15.0","draft":true,
     "body": ($begin + "\nold auto\n" + $end), "created_at":"2026-06-01T00:00:00Z","html_url":"u51"} ]')"
new_state "$fixture"
export FAKE_FAIL_FIRST_LIST=1   # first releases-list call answers HTTP 502
run_script
[ $RC -ne 0 ]; check BP1a $? "script exits non-zero on a failed lookup (job noise beats a silent duplicate)"
[ "$(draft_ids_for_tag)" = "1" ] && [ "$(count_created)" = "0" ]
check BP1b $? "no duplicate created: $TAG count stays 1 after the failed lookup"
rm -rf "$FAKE_STATE"

echo
echo "== draft-release.sh acceptance: $PASS passed, $FAIL failed =="
if [ "$FAIL" -gt 0 ]; then printf 'Failing:%b\n' "$FAILED_NAMES"; exit 1; fi
exit 0
