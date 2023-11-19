using Microsoft.Web.WebView2.Core;

namespace MauiBlazorPermission;

partial class PermissionHandler()
{

    public static partial async Task OpenAppPermissionPanelAsync(string? windowsScheme)
    {
        windowsScheme ??= "ms-settings:appsfeatures-app";
        await Windows.System.Launcher.LaunchUriAsync(new(windowsScheme));
    }

    public virtual void OnPermissionRequested(CoreWebView2 wv2, CoreWebView2PermissionRequestedEventArgs args)
    {
        args.State = CoreWebView2PermissionState.Allow;        
    }

}
