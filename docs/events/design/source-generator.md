# Source Generator & Analyzers

> **Status:** Draft · 2026-10-06 · Part of the [RForge.Blazor.Bus design spec](index.md)

The generator turns attributes into a `RegisterSubscriptions` override. That's all it does. The lifecycle work lives in [`BusComponentBase`](components.md), so generated code stays small and readable, and the [manual API](components.md#manual-registration) can always produce the same result.

Why use a generator instead of reflection:
- **Trim/AOT safety.** WebAssembly apps are trimmed and may be AOT-compiled. Discovering handlers by reflection at runtime fights both. Generated code is plain, statically analyzable method calls.
- **Compile-time errors.** Bad signatures, wrong types and missing selectors are diagnostics, not runtime surprises.
- **Zero startup cost.** No scanning.

## Code-behind is required

> **Annotated members must be in a C# code-behind file (`*.razor.cs`), not in a `@code { }` block.**

The Razor compiler is itself a source generator, and Roslyn source generators **cannot see each other's output**. Anything declared in `.razor` markup (`@code` members, `@attribute`, `@inherits`) exists only in Razor's generated output, which is invisible to this generator.

Code-behind partial classes are ordinary C# that every generator sees. Razor components are already `partial`, so a code-behind file only needs to repeat the class name:

```
Components/
  CartBadge.razor       ← @inherits BusComponentBase, then markup (and @code for anything not annotated)
  CartBadge.razor.cs    ← public partial class CartBadge { [Subscribe] ... }
```

### Declaring the base class

**Put `@inherits BusComponentBase` in the `.razor` file.** The code-behind can repeat `: BusComponentBase` or leave the base out.

When a `.razor` file has no `@inherits`, the Razor compiler emits `: ComponentBase` on its half of the partial class. A code-behind that declares a *different* base then fails with CS0263 ("partial declarations must not specify different base classes"). This was confirmed while implementing. It's why the original advice in this spec (base class in code-behind only) was wrong.

The generator doesn't need to see the base class: it emits the `RegisterSubscriptions` override regardless. The base class is checked by the analyzer (`RFBUS003`) against the final compilation, which does include `@inherits`. Components written entirely in C# (no `.razor` file) simply declare `: BusComponentBase`.

### Detecting misuse

**Analyzers *do* run on the final compilation, including Razor's output.** So the misuse can be *detected* even though it can't be *supported*: `RFBUS001` reports an error when `[Subscribe]` or `[UseState]` appears on a member that comes from a `.razor` file. Razor's `#line` mapping makes the error point at the right line of the `.razor` file. Both behaviors are confirmed with the .NET 10 SDK.

See [ADR-004](decisions.md#adr-004-annotated-members-require-code-behind).

## Attributes

### `[Subscribe]`

Applied to an instance method. The event type is the method's **first parameter type**.

| Signature | Notes |
|---|---|
| `void M(TEvent e)` | Sync |
| `Task M(TEvent e)` | Async |
| `ValueTask M(TEvent e)` | Async |
| `Task M(TEvent e, CancellationToken ct)` | Receives the linked publish + disposal token |
| `ValueTask M(TEvent e, CancellationToken ct)` | |

Any accessibility is allowed (private is typical). Static methods, generic methods, `ref`/`out`/`in` parameters and other return types are errors.

| Property | Default | Meaning |
|---|---|---|
| `Scope` | `ScopeKind.Nearest` | Which scope to attach to |
| `ScopeName` | `null` | Setting it implies `Scope = Named` |
| `Render` | `true` | Call `StateHasChanged` after the handler |

### `[UseState]`

Applied to a **partial property with only a `get` accessor** (C# 13+ partial properties; this library targets C# 14).

| Form | Property type | Re-renders on |
|---|---|---|
| `[UseState] partial CartState Cart { get; }` | The state type | Any effective change |
| `[UseState(Select = nameof(Sel))] partial int Count { get; }` | The selector's return type | Changes to the selected value |

Selector requirements: `static`, one parameter of a type implementing `IState<T>`, and a return type identity-convertible to the property type. Tuple element names may differ, so `(int, string)` matches `(int Count, string Label)`. The state type is inferred from the parameter.

| Property | Default | Meaning |
|---|---|---|
| `Select` | `null` | Name of a static selector method in the same type |
| `Scope` / `ScopeName` | Nearest / `null` | Where to start provider resolution |
| `Render` | `true` | Re-render on change. With `false`, `Value` stays current but no render is triggered |

## Generated code

**Input** (`CartBadge.razor.cs`):

```csharp
public partial class CartBadge : BusComponentBase
{
    [UseState] private partial CartState Cart { get; }

    [UseState(Select = nameof(SelectCount))]
    private partial int Count { get; }
    private static int SelectCount(CartState s) => s.Count;

    [Subscribe]
    private void OnItemAdded(ItemAdded e) => _pulse = true;

    [Subscribe(ScopeName = "checkout", Render = false)]
    private Task OnSubmitted(CheckoutSubmitted e, CancellationToken ct) => ResetAsync(ct);

    private bool _pulse;
}
```

**Output** (`MyApp.Components.CartBadge.Bus.g.cs`, lines wrapped for readability):

```csharp
// <auto-generated/>
#nullable enable
#pragma warning disable CS1591

namespace MyApp.Components
{
    partial class CartBadge
    {
        private global::RForge.Blazor.Bus.StateHandle<global::MyApp.CartState> __bus_Cart = default!;
        private partial global::MyApp.CartState Cart => __bus_Cart.Value;

        private global::RForge.Blazor.Bus.SelectorHandle<global::MyApp.CartState, int> __bus_Count = default!;
        private partial int Count => __bus_Count.Value;

        partial void OnRegisterSubscriptions(global::RForge.Blazor.Bus.SubscriptionBuilder builder);

        /// <inheritdoc />
        protected override void RegisterSubscriptions(global::RForge.Blazor.Bus.SubscriptionBuilder builder)
        {
            base.RegisterSubscriptions(builder);
            __bus_Cart = builder.State<global::MyApp.CartState>(
                global::RForge.Blazor.Bus.ScopeTarget.Nearest, render: true);
            __bus_Count = builder.State<global::MyApp.CartState, int>(
                (global::System.Func<global::MyApp.CartState, int>)SelectCount,
                global::RForge.Blazor.Bus.ScopeTarget.Nearest, render: true);
            builder.On<global::MyApp.ItemAdded>(
                (global::System.Action<global::MyApp.ItemAdded>)OnItemAdded,
                global::RForge.Blazor.Bus.ScopeTarget.Nearest, render: true);
            builder.On<global::MyApp.CheckoutSubmitted>(
                (global::System.Func<global::MyApp.CheckoutSubmitted, global::System.Threading.CancellationToken, global::System.Threading.Tasks.Task>)OnSubmitted,
                global::RForge.Blazor.Bus.ScopeTarget.Named("checkout"), render: false);
            OnRegisterSubscriptions(builder);
        }
    }
}
```

Method groups are cast to their exact delegate type. That keeps overload resolution unambiguous even when a user has several overloads with the same name, such as two `On(...)` handlers for different events.

### Invalid `[UseState]` properties are stubbed

The C# compiler **doesn't run analyzers when compilation errors exist**. If the generator simply skipped an invalid `[UseState]` property, the only error would be CS9248 ("partial property must have an implementation part"), and the analyzer's explanation would never appear.

So when the property shape is valid but the type (`RFBUS007`) or the selector (`RFBUS008`) isn't, the generator still implements the property, as `=> throw new InvalidOperationException(...)`. The build then fails with the analyzer's clear error and nothing else. Invalid `[Subscribe]` handlers are just left unwired, since they never cause a compile error.

Conventions:
- Fully qualified `global::` names everywhere. The generated file declares no `using` directives.
- Generated members are prefixed `__bus_` to avoid collisions.
- One generated file per annotated type: `{TypeName}.Bus.g.cs`. Nested and generic types are supported, and the file name includes the containing types and arity.
- Registration order follows **source declaration order**: state handles first, then handlers. This order is visible to users because it's the delivery order within a scope.

## Combining with manual registration

A type that uses attributes **must not** also override `RegisterSubscriptions` (`RFBUS009`), because the generator already does. Use the generated partial hook instead:

```csharp
partial void OnRegisterSubscriptions(SubscriptionBuilder builder)
{
    if (TrackAnalytics)
        builder.On<ItemAdded>(Track, ScopeTarget.Root, render: false);
}
```

It runs after the generated registrations.

## Inheritance

Generated overrides call `base.RegisterSubscriptions(builder)` first. A component deriving from another annotated component therefore gets the base's subscriptions, then its own. Each type in the hierarchy gets its own generated file. Base handlers run before derived handlers for the same event.

## Diagnostics

| ID | Severity | Condition |
|---|---|---|
| `RFBUS001` | Error | `[Subscribe]`/`[UseState]` on a member declared in a `.razor` file. Move it to code-behind. |
| `RFBUS002` | Error | Containing type (or a containing type of a nested type) is not `partial` |
| `RFBUS003` | Error | Containing type doesn't derive from `BusComponentBase` |
| `RFBUS004` | Error | Invalid `[Subscribe]` handler signature (see table above) |
| `RFBUS005` | Error | Handler's first parameter type doesn't implement `IEvent` |
| `RFBUS006` | Error | `[UseState]` target isn't a non-static `partial` property with only a `get` accessor |
| `RFBUS007` | Error | `[UseState]` without `Select`: property type doesn't implement `IState<T>` |
| `RFBUS008` | Error | `Select` method not found, not static, wrong arity, parameter isn't an `IState<T>`, or return type ≠ property type |
| `RFBUS009` | Error | Type uses attributes **and** overrides `RegisterSubscriptions`. Use `partial void OnRegisterSubscriptions`. |
| `RFBUS010` | Warning | Inconsistent scope arguments: `ScopeName` with `Scope` ≠ `Named`, or `Scope = Named` without `ScopeName` |
| `RFBUS011` | Warning | More than one `[Subscribe]` handler for the same event type in one type (legal but usually a mistake) |
| `RFBUS012` | Info | Event type isn't immutable: not a `record`, or exposes settable properties or non-readonly fields |
| `RFBUS013` | Warning | State type isn't a `record`, so change detection falls back to reference equality |
| `RFBUS014` | Warning | Type deriving from `BusComponentBase` implements `IDisposable`/`IAsyncDisposable` directly. Override `DisposeAsyncCore`. |

Code fixes are planned but **not yet implemented** as of 0.0.1-rc4: make the type partial (`RFBUS002`), convert a property to a partial get-only property (`RFBUS006`), and move a manual override into `OnRegisterSubscriptions` (`RFBUS009`).

## Packaging

- The generator and analyzers are one assembly, `RForge.Blazor.Bus.Generators`, targeting **`netstandard2.0`** as Roslyn requires.
- It ships **inside** the `RForge.Blazor.Bus` NuGet package under `analyzers/dotnet/cs/`, so installing the runtime package enables the generator automatically. There's no separate package to forget.
- It's an incremental generator (`IIncrementalGenerator`) keyed on `ForAttributeWithMetadataName`, so typing in unrelated files doesn't trigger regeneration.
