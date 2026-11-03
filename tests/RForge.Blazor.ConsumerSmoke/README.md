# RForge.Blazor consumer smoke test

Small projects that build the **packed** packages the way an app would. They are not part of `RForge.sln`;
the `pr_build` workflow packs everything and builds them.

| Project | What it proves |
|---|---|
| `Sdk8` | A net8 app on the .NET 8 SDK (pinned by `global.json`) at the default C# 12: the Events source generator loads in that older compiler, and `[Subscribe]` and every `SubscriptionBuilder.On` shape that C# 12 can resolve compile. |
| `Latest` | A net8 app with `LangVersion` 13 or later: async lambdas passed to `On` resolve, and `[UseState]` works. |
| `Bundle` | A project that references **only** the `RForge.Blazor` bundle: it compiles only if the bundle delivers the components, the Events generator and the AutoApi generator. |

To run them locally:

```bash
dotnet pack src/RForge -c Release -o /tmp/smoke-feed -p:Version=0.0.0-smoke
cd tests/RForge.Blazor.ConsumerSmoke/Sdk8
dotnet build "-p:RestoreSources=/tmp/smoke-feed%3Bhttps://api.nuget.org/v3/index.json"
```

On Windows, MSBuild turns the `/` in `RestoreSources` into `\`, so put the same two sources in a `nuget.config`
next to the project instead. Delete `~/.nuget/packages/rforge.blazor*` between runs, or NuGet reuses the previous
`0.0.0-smoke`.
