namespace RForgeDocs.Models;

/// <summary>One Events tutorial: the route slug, the page title and a one-line summary.</summary>
public record EventsTutorial(string Number, string Slug, string Title, string Description)
{
    public string Url => $"/events/tutorials/{Slug}";
}

/// <summary>The Events tutorials in reading order. The side nav, previous/next links and the index all read this list.</summary>
public static class EventsTutorialList
{
    public static IReadOnlyList<EventsTutorial> All { get; } =
    [
        new("01", "01-setup", "Setup", "Install the package, register the hub, lay out your project, build your first event-aware component"),
        new("02", "02-state", "State", "Define state, read it reactively with [UseState], write it, and use selectors to limit re-renders"),
        new("03", "03-state-with-cascades", "State with cascades", "Let plain (or third-party) components read hub state through [CascadingParameter]"),
        new("04", "04-events", "Events", "Define events, publish them, handle them with [Subscribe], and decide when to use an event vs. state"),
        new("05", "05-scopes-and-routing", "Scopes and routing", "Fence events into a subtree with <EventScope> and choose where events travel with routes"),
        new("06", "06-scoped-state", "Scoped state", "Give each instance of a widget its own copy of a state with <StateScope>"),
        new("07", "07-services-and-background-work", "Services and background work", "Publish and subscribe from services, timers and other threads"),
        new("08", "08-prerendering-and-persistence", "Prerendering and persistence", "Keep state loaded during prerendering when the page becomes interactive"),
        new("09", "09-render-modes", "Render modes", "What works where (Static, Server, WebAssembly, Auto), and the island boundary"),
        new("10", "10-manual-registration", "Manual registration", "Subscribe without attributes: parameter-dependent selectors, dynamic subscriptions"),
        new("11", "11-testing-with-bunit", "Testing with bUnit", "Unit-test event-aware components"),
        new("12", "12-troubleshooting", "Troubleshooting", "Every RFEVT diagnostic and the most common runtime mistakes"),
        new("13", "13-best-practices", "Best practices", "One-page checklist: project layout, file placement, naming, design rules"),
    ];
}
