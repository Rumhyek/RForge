namespace RForge.Blazor.Events.Sample.Components.Examples.Notifications;

/// <summary>Shows toasts published from anywhere. It attaches to the root, so it hears every bubbled ToastRequested.</summary>
public partial class ToastHost
{
    private readonly List<Toast> _toasts = [];

    [Subscribe(Scope = ScopeKind.Root)]
    private void OnToast(ToastRequested request)
    {
        var toast = new Toast(Guid.NewGuid(), request.Message, request.Level);
        _toasts.Add(toast);
        _ = DismissLaterAsync(toast.Id);
    }

    private async Task DismissLaterAsync(Guid id)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(4), DisposalToken);
        }
        catch (OperationCanceledException)
        {
            return; // The component was disposed.
        }

        Dismiss(id);
        StateHasChanged();
    }

    private void Dismiss(Guid id) => _toasts.RemoveAll(t => t.Id == id);

    private static string CssFor(ToastLevel level) => level switch
    {
        ToastLevel.Success => "success",
        ToastLevel.Warning => "warning",
        ToastLevel.Danger => "danger",
        _ => "primary",
    };

    private sealed record Toast(Guid Id, string Message, ToastLevel Level);
}
