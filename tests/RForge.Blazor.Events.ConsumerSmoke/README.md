# RForge.Blazor.Events consumer smoke test

Two small projects that build the **packed** `RForge.Blazor.Events` package the way an app would. They are
not part of `RForge.sln`; the `pr_build` workflow packs the library and builds them.

| Project | What it proves |
|---|---|
| `Sdk8` | A net8 app on the .NET 8 SDK (pinned by `global.json`) at the default C# 12: the source generator loads in that older compiler, and `[Subscribe]` and every `SubscriptionBuilder.On` shape that C# 12 can resolve compile. |
| `Latest` | A net8 app with `LangVersion` 13 or later: async lambdas passed to `On` resolve, and `[UseState]` works. |

To run them locally:

```bash
dotnet pack src/RForge/RForge.Blazor.Events -c Release -o /tmp/smoke-feed -p:Version=0.0.0-smoke
cd tests/RForge.Blazor.Events.ConsumerSmoke/Sdk8
dotnet build "-p:RestoreSources=/tmp/smoke-feed%3Bhttps://api.nuget.org/v3/index.json"
```

Delete `~/.nuget/packages/rforge.blazor.events` between runs, or NuGet reuses the previous `0.0.0-smoke`.
