using Microsoft.UI.Xaml;

namespace MauiBlazorPermission;

partial class BlazorWebViewHandler
{

    protected virtual partial void BlazorWebView_BlazorWebViewInitializing(object? sender, BlazorWebViewInitializingEventArgs e) { }

    protected virtual partial void BlazorWebView_BlazorWebViewInitialized(object? sender, BlazorWebViewInitializedEventArgs e)
    {
        var handler = GetPermissionHandler(e.WebView);

        e.WebView.CoreWebView2.PermissionRequested += handler.OnPermissionRequested;
    }

    protected virtual PermissionHandler GetPermissionHandler(UIElement parentElement) =>
        new();

}
