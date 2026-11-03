# How deployments work

Merging to `main` never ships anything to the public. A release only reaches
nuget.org and the live docs site when someone **publishes the draft GitHub release**.

| What you do | Workflow | What happens |
|---|---|---|
| Merge a PR that touches `src/**` | `deploy_main` | Packs `X.Y.Z-rc.N`, pushes it to the GitHub package feed, and creates or refreshes the `vX.Y.Z` draft release |
| Merge a PR that doesn't touch `src/**` | None | Nothing is built or deployed |
| Publish the draft release | `deploy_release` | Packs `X.Y.Z` and pushes it to the GitHub feed and nuget.org |
| Publish the draft release | `deploy_rforge_docs` | Builds `docs/RForgeDocs` and deploys it to https://rforge.azurewebsites.net |

All three workflows can also be started by hand from the Actions tab.
`deploy_release` then asks which tag to package, and `deploy_rforge_docs` deploys
whichever branch you pick straight to production.

No workflow runs the tests, so run them yourself before releasing.

## Cutting a release

1. Run the tests on an up-to-date `main`:

   ```powershell
   dotnet test src/RForge/RForge.Blazor.UnitTest
   ```

2. In a PR, rename `## Unreleased` to `## Version X.Y.Z` in both changelogs, then merge it:
   - `src/RForge/RForgeBlazor/CHANGELOG.md`
   - `src/RForge/RForge.Abstractions/CHANGELOG.md`

   Each file's full text becomes that package's release notes on nuget.org.

3. Wait for `Create RForge Draft Release` to pass in the Actions tab. Its last
   `X.Y.Z-rc.N` package on the GitHub feed is the code you're about to ship.

4. Open the draft on the [Releases page](https://github.com/Rumhyek/RForge/releases).
   Replace its notes with a complete list (see [Known gaps](#known-gaps)):

   ```powershell
   gh api repos/Rumhyek/RForge/releases/generate-notes -f tag_name=vX.Y.Z -f previous_tag_name=vPREVIOUS -f target_commitish=main --jq .body
   ```

5. Check the tag field says `vX.Y.Z`, not `untagged-...`, and that the target is
   `main`. Then click **Publish release**. Publishing creates the tag from the
   current head of `main`, so anything merged since step 3 ships too.

6. Confirm the release landed:
   - Both publish workflows passed in the Actions tab.
   - [RForge.Blazor](https://www.nuget.org/packages/RForge.Blazor) and
     [RForge.Blazor.Abstractions](https://www.nuget.org/packages/RForge.Blazor.Abstractions)
     show the new version. nuget.org can take a few minutes to index it.
   - The [docs site](https://rforge.azurewebsites.net) loads.

A nuget.org version can never be replaced or reused. It can only be unlisted, so
a bad release has to be fixed with a new version number.

## How versions are chosen

[GitVersion](https://gitversion.net) works out the version from Git history and
`GitVersion.yml`. The csproj files don't hold a version number; workflows pass it
in with `-p:Version`. Both packages always get the same version.

- **Starting point:** the newest `vX.Y.Z` tag.
- **Default bump:** patch, so after `v0.15.0` the next builds are `0.15.1-rc.N`.
- **Bigger bump:** put one of these in a commit message on `main`. It stays in
  effect until the next tag.
  - `+semver: minor` or `+semver: feature`
  - `+semver: major` or `+semver: breaking`
- **`rc.N`:** N is the number of commits since the last tag.
- **Tagged commit:** builds exactly `X.Y.Z`, with no `rc` suffix.

When you squash-merge, keep any `+semver:` line in the final commit message, or the
bump is lost.

## Where packages go

| Feed | Gets | Reading it |
|---|---|---|
| GitHub (`https://nuget.pkg.github.com/Rumhyek/index.json`) | Every `rc` build and every release | Needs a GitHub token with `read:packages`, even for a public repo |
| nuget.org | Releases | Public |

## Secrets

Set under **Settings > Secrets and variables > Actions**. `gh secret list` shows
when each was last updated.

| Secret | Used by | Notes |
|---|---|---|
| `NUGET_USER` | `deploy_release` | Your nuget.org profile name, not your email address |
| `AZUREAPPSERVICE_PUBLISHPROFILE_...` | `deploy_rforge_docs` | Publish profile for the `rforge` Azure web app |
| `GITHUB_TOKEN` | `deploy_main`, `deploy_release` | Provided automatically for each run |

## nuget.org trusted publishing

`deploy_release` doesn't store a nuget.org API key. Its `NuGet/login` step exchanges
a GitHub identity token for a temporary key that lasts one hour. nuget.org only
allows this if a policy matches the workflow. To see or change the policy, click
your username on nuget.org and choose **Trusted Publishing**.

| Policy setting | Value |
|---|---|
| Repository Owner | `Rumhyek` |
| Repository | `RForge` |
| Workflow File | `deploy_release.yml` |
| Environment | Blank |
| Scope | Push only new package versions |
| Glob pattern | `RForge.Blazor*` |

Update the policy if you rename the workflow file or add an environment to its job.
A brand-new package ID needs the "Push new packages and package versions" scope for
its first push.

## Releasing without workflows

Use this only if Actions is unavailable. Trusted publishing only works inside
Actions, so you need a regular nuget.org API key in
`NUGET_API_KEY` and a `gh` token with the packages scope
(`gh auth refresh -s write:packages`). Finish steps 1 and 2 of
[Cutting a release](#cutting-a-release) first.

```powershell
$version = "X.Y.Z"
$out = "$env:TEMP\rforge-$version"
dotnet pack src/RForge/RForge.sln -c Release --output $out -p:Version=$version -p:ContinuousIntegrationBuild=true

$feeds = [ordered]@{ "https://nuget.pkg.github.com/Rumhyek/index.json" = (gh auth token); "https://api.nuget.org/v3/index.json" = $env:NUGET_API_KEY }
foreach ($feed in $feeds.GetEnumerator()) {
    dotnet nuget push "$out\RForge.Blazor.Abstractions.$version.nupkg" --source $feed.Key --api-key $feed.Value
    dotnet nuget push "$out\RForge.Blazor.$version.nupkg" --source $feed.Key --api-key $feed.Value
}
```

Then push a `vX.Y.Z` tag and publish the draft. Publishing still starts both publish
workflows if Actions is running: `deploy_release` skips the versions that already
exist, and `deploy_rforge_docs` deploys the site. To stop them, run
`gh workflow disable` on each before publishing and `gh workflow enable` afterwards.
The docs site then won't update until `deploy_rforge_docs` runs.

## Known gaps

- **Draft notes miss squash-merged PRs.** `.github/scripts/draft-release.sh` only
  recognizes `Merge pull request #N` commits when it refreshes the draft. That's why
  step 4 regenerates the notes.
