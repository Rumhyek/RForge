#!/usr/bin/env bash
# Keep exactly ONE draft release per version tag (fix #119).
#
# Called from .github/workflows/deploy_main.yml after packages are pushed.
#   - Nothing for $NEW_TAG     -> create a --generate-notes draft (today's
#     behavior), then wrap the generated notes in AUTO markers so future runs
#     can tell auto text from PO text.
#   - One draft for $NEW_TAG   -> regenerate the notes for the new head of main
#     (compare API) and rewrite ONLY the marker-wrapped region. Text outside
#     the markers is PO scratch-space and is preserved verbatim — the refresh
#     never deletes (spec #119 §4/§6 decision, "never delete text" variant).
#   - Several drafts stacked   -> legacy duplicates (today's bug): keep the
#     NEWEST (its notes cover the most merges), absorb the older drafts' PO
#     text into the survivor so nothing is lost, delete the extras.
#   - Published release exists -> leave it alone (publishing is the PO's one
#     click); any stray draft for the same version is cleaned up.
#
# GitHub quirks caught by the #119 dry-run, encoded here:
#   1. A DRAFT created from a not-yet-existing tag is NOT resolvable via
#      /releases/tags/{tag} or `gh release view <tag>` (those resolve through a
#      git ref that only exists once published) — use the /releases LIST and
#      read/write by release ID.
#   2. GitHub renames such a draft's tag_name to "untagged-<hash>" within
#      minutes while the version string survives in .name — so drafts are
#      keyed by tag_name OR name, never tag_name alone.
#   3. The list API can lag a moment behind a create — retry briefly after.
#
# Required env: GH_TOKEN (gh auth), REPO ("owner/name"), HEAD_SHA (full sha of
# head of main). Arg 1: the tag, e.g. v0.15.0. `concurrency:` in the workflow
# already serializes runs, so no locking here (spec §6).
set -euo pipefail

NEW_TAG="${1:?usage: draft-release.sh <tag> (e.g. v0.15.0)}"
REPO="${REPO:?REPO env (owner/name) is required}"
HEAD_SHA="${HEAD_SHA:?HEAD_SHA env is required}"

BEGIN='<!-- auto-release-notes:start -->'
END='<!-- auto-release-notes:end -->'

TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"' EXIT

write_body() { # $1 = release id, stdin = new body
  jq -Rs '{body: .}' < /dev/stdin > "$TMP/patch.json"
  gh api -X PATCH "repos/$REPO/releases/$1" --input "$TMP/patch.json" --jq '.html_url' > /dev/null
}

# PO text = everything outside the marker region. A legacy body (created by the
# old create-only step, no markers) is preserved wholesale as PO text; its stale
# auto block then survives once as text — the accepted cost of "never delete".
po_text() { # $1 = file holding the current body, writes $TMP/po.txt
  if grep -qF "$BEGIN" "$1"; then
    awk -v b="$BEGIN" -v e="$END" \
      'index($0, b) { inside = 1; next } index($0, e) { inside = 0; next } !inside { print }' \
      "$1" > "$TMP/po.txt"
  else
    cp "$1" "$TMP/po.txt"
  fi
}

# --------------------------------------------------------------------------
# Which release objects belong to $NEW_TAG? -> "id draft" lines by created_at.
# Published releases key on tag_name only (their git ref exists); drafts key on
# tag_name OR name (see quirk 2).
# --------------------------------------------------------------------------
find_matches() {
  # A FAILED lookup is never read as "absent" (reviewer note 1 / BP1 harness):
  # masking here would send Path A to create a duplicate of an existing draft —
  # the exact bug class this script kills. Failed lookup exits red (job noise
  # beats a silent duplicate); only the post-create retry loop tolerates lag,
  # and only for a *successful-but-empty* list.
  local raw rc
  raw="$(gh api --paginate "repos/$REPO/releases?per_page=100" \
     --jq "[.[] | select(.tag_name == \"$NEW_TAG\" or (.draft and .name == \"$NEW_TAG\"))]
           | sort_by(.created_at) | .[] | \"\(.id) \(.draft)\"" 2>"$TMP/find_matches.err")" \
    && rc=0 || rc=$?
  if [ "$rc" -ne 0 ]; then
    printf '::error title=Draft release::find_matches: gh api releases lookup failed (rc=%s) — refusing to treat "unknown" as "absent":\n' "$rc" >&2
    cat "$TMP/find_matches.err" >&2 2>/dev/null || true
    return 1
  fi
  printf '%s\n' "$raw" | grep -E '^[0-9]+ (true|false)$' || true
}

MATCHES="$(find_matches)"
PUBLISHED_ID="$(printf '%s\n' "$MATCHES" | awk '$2 == "false" { print $1; exit }')"
DRAFT_IDS=()
while IFS=' ' read -r id _; do [ -n "${id:-}" ] && DRAFT_IDS+=("$id"); done \
  < <(printf '%s\n' "$MATCHES" | awk '$2 == "true" { print $1 }')

# Path C: PUBLISHED — never touch it; sweep stray drafts of the same version.
if [ -n "$PUBLISHED_ID" ]; then
  echo "::notice title=Draft release::$NEW_TAG is already published — leaving it untouched."
  for extra in ${DRAFT_IDS[@]+"${DRAFT_IDS[@]}"}; do
    # Reviewer note 2 (never-delete symmetry): the published release must stay
    # byte-identical (harness B2a), so a stray's PO scratch text has no survivor
    # to absorb into — preserve it verbatim in the job log before deleting.
    if gh api "repos/$REPO/releases/$extra" --jq .body > "$TMP/stray.txt" 2> "$TMP/stray.err"; then
      po_text "$TMP/stray.txt"
      [ -s "$TMP/po.txt" ] && { echo "PO text preserved from stray draft $extra (published $NEW_TAG untouched):"; cat "$TMP/po.txt"; }
    else
      # Delta re-review Finding 2: a transient fetch failure must not lose the
      # PO's scratch text in silence. Loud warning, delete still proceeds -
      # blocking the whole deploy over one stray draft is not justified;
      # silence is not either (harness case B2w).
      echo "::warning title=Draft release::Failed to read stray draft $extra's body before deleting it - any PO scratch text in it may be lost. gh api said: $(tr '\n' ' ' < "$TMP/stray.err")"
    fi
    echo "deleting stray draft $extra for published $NEW_TAG"
    gh api -X DELETE "repos/$REPO/releases/$extra" > /dev/null
  done
  exit 0
fi

# Path A: nothing yet — create today's draft, then wrap its notes in markers.
if [ "${#DRAFT_IDS[@]}" -eq 0 ]; then
  echo "::notice title=Draft release::No release for $NEW_TAG — creating a fresh draft."
  gh release create "$NEW_TAG" --generate-notes --draft
  for _ in 1 2 3 4 5; do  # list-API lag (quirk 3); fail LOUDLY, never blindly
    MATCHES="$(find_matches)"
    DRAFT_IDS=()
    while IFS=' ' read -r id _; do [ -n "${id:-}" ] && DRAFT_IDS+=("$id"); done \
      < <(printf '%s\n' "$MATCHES" | awk '$2 == "true" { print $1 }')
    [ "${#DRAFT_IDS[@]}" -gt 0 ] && break
    sleep 2
  done
  [ "${#DRAFT_IDS[@]}" -gt 0 ] || { echo "release for $NEW_TAG not findable after create" >&2; exit 1; }
  REL_ID="${DRAFT_IDS[-1]}"
  {
    echo "$BEGIN"
    gh api "repos/$REPO/releases/$REL_ID" --jq .body
    echo "$END"
  } | write_body "$REL_ID"
  exit 0
fi

# Paths B/D: refresh the NEWEST draft, absorb PO text from any older stacked
# duplicates (the #119 incident), delete those duplicates.
REL_ID="${DRAFT_IDS[-1]}"
echo "::notice title=Draft release::Draft for $NEW_TAG is $REL_ID ($(printf '%s ' "${DRAFT_IDS[@]}")seen)."
gh api "repos/$REPO/releases/$REL_ID" --jq .body > "$TMP/newest.txt"
po_text "$TMP/newest.txt"
cp "$TMP/po.txt" "$TMP/po_merged.txt"
for dup in "${DRAFT_IDS[@]:0:${#DRAFT_IDS[@]}-1}"; do
  gh api "repos/$REPO/releases/$dup" --jq .body > "$TMP/dup.txt"
  po_text "$TMP/dup.txt"
  if [ -s "$TMP/po_merged.txt" ]; then
    printf '\n## Preserved from duplicate draft %s\n\n' "$dup" >> "$TMP/po_merged.txt"
  fi
  cat "$TMP/po.txt" >> "$TMP/po_merged.txt"
  echo "absorbed duplicate draft $dup into $REL_ID — deleting the duplicate"
  gh api -X DELETE "repos/$REPO/releases/$dup" > /dev/null
done
mv "$TMP/po_merged.txt" "$TMP/po.txt"

# Base for the changelog: newest PUBLISHED tag other than ours; if none, the
# newest other tag of any kind; if none at all, the root commit.
PREV="$(gh release list --limit 100 --json tagName,isDraft,createdAt \
        --jq "[.[] | select(.tagName != \"$NEW_TAG\" and .isDraft == false)] | sort_by(.createdAt) | last | .tagName // \"\"")"
if [ -z "$PREV" ]; then
  PREV="$(gh release list --limit 100 --json tagName,isDraft,createdAt \
          --jq "[.[] | select(.tagName != \"$NEW_TAG\")] | sort_by(.createdAt) | last | .tagName // \"\"")"
fi
if [ -z "$PREV" ]; then
  PREV="$(git rev-list --max-parents=0 HEAD | tail -n 1)"
fi

# Fresh notes mirroring `gh release create --generate-notes`: PR bullets since
# PREV + Full Changelog footer. PR numbers come ONLY from `Merge pull request
# #N` commit lines — titles like "feat(#116):" name ISSUES, not PRs (caught by
# the dry-run). NOTE: the compare API caps .commits at 250 — an implementation
# acceptance of this script, not a spec clause; release windows here are far
# smaller than the cap (corrects reviewer note 3's mis-attribution to §6).
gh api "repos/$REPO/compare/$PREV...$HEAD_SHA" \
   --jq '[.commits[] | .commit.message | split("\n")[0]] | .[]' \
   | sed -nE 's/^Merge pull request #([0-9]+) .*/\1/p' | sort -un > "$TMP/prs.txt"

{
  echo "## What's Changed"
  while IFS= read -r n; do
    gh api "repos/$REPO/pulls/$n" \
      --jq '"* [\(.title)](\(.html_url)) by @\(.user.login)"' 2>/dev/null \
      || echo "* merged PR #$n"
  done < "$TMP/prs.txt"
  echo
  echo "**Full Changelog**: https://github.com/$REPO/compare/$PREV...$NEW_TAG"
} > "$TMP/notes.txt"

# Rebuild: refreshed auto block on top, PO text appended verbatim (order flips
# once on a legacy body; stable on every later refresh since the region is now
# marked).
{
  echo "$BEGIN"
  cat "$TMP/notes.txt"
  echo "$END"
  echo
  cat "$TMP/po.txt"
} | sed '/./,$!d' | write_body "$REL_ID"

echo "::notice title=Draft release::Refreshed the single draft for $NEW_TAG (id $REL_ID; notes regenerated vs $PREV; PO text preserved)."
