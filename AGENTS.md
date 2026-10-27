# RForge — Agent Working Rules

For every Hermes worker touching this repo. Role playbooks come from the attached
skills (`sdlc-workflow`, your role skill, `rforge-conventions`). This file is repo
facts + gates only — it does not replace them.

## What this repo is
A lightweight Blazor component library on Bulma CSS (MIT). The PO is GitHub user
**Rumhyek** — the sole approver, per `sdlc-workflow`.

## Layout & build
- Library: `src/RForge/RForgeBlazor` · solution `src/RForge/RForge.sln` · TFMs `net8.0;net9.0;net10.0`
- Docs site: in-repo Blazor app `docs/RForgeDocs` (`RForgeDocs.sln`) → rforge.azurewebsites.net
- Tests: `src/RForge/RForge.Blazor.UnitTest` — **MSTest + Verify** (no bUnit; adding it needs a PO-approved spec)
- Build: `dotnet build src/RForge/RForge.sln -c Release`
- Test: `dotnet test src/RForge/RForge.Blazor.UnitTest` (net10 SDK at `/usr/local/bin/dotnet`)
- Free evidence: push your branch and check GitHub Actions (`deploy_main`, `deploy_release`, `deploy_rforge_docs`)

## Gates (never cross as a worker)
- Gate 1: only the PO sets `status:approved`.
- Gate 2: only the PO merges code PRs.
- Exception (PO pre-authorized 2026-10-26): PRs labeled `docs-only` that touch **only** `docs/RForgeDocs` or docs files MAY be merged by the doc-writer. Anything else in the diff voids the label.

## Release train (PO decision, issue #116)
Merges to `main` NEVER publish to nuget.org. `deploy_main` builds, packs, pushes
packages to the **GitHub package feed** (private, dogfoodable) and opens a
**draft release** per merge. The ONLY ship event is the PO **publishing that
draft release** (one click per finished milestone): that triggers
`deploy_release` (nuget.org push) + `deploy_rforge_docs` (live site). Branching
stays flat (`feat/<issue#>-<slug>` → main); publishing is PO-only — workers never
touch releases.

## Docs target
Component docs are pages in `docs/RForgeDocs` (dogfoods the components). "Feature done"
includes its docs page + nav entry.

## Runner conventions
- Clone: `/root/repos/RForge` · worktrees: `/root/worktrees/<issue#>-<slug>` · one branch per issue (`feat/<issue#>-<slug>` off `main`)
- `gh` CLI authenticated as Rumhyek. If `gh auth status` fails, block the issue — never retry blindly.
