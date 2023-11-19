using Android.Webkit;
using AndroidX.Activity;
using Microsoft.Maui.Platform;

namespace JsPermissionHandler;

partial class BlazorWebViewHandler
{

    protected virtual partial void BlazorWebView_BlazorWebViewInitializing(object? sender, BlazorWebViewInitializingEventArgs e) { }

    protected virtual partial void BlazorWebView_BlazorWebViewInitialized(object? sender, BlazorWebViewInitializedEventArgs e)
    {
        if (e.WebView.Context?.GetActivity() is not ComponentActivity activity)
        {
            throw new InvalidOperationException($"The permission-managing WebChromeClient requires that the current activity be a '{nameof(ComponentActivity)}'.");
        }

        e.WebView.Settings.JavaScriptEnabled = true;
        e.WebView.Settings.AllowFileAccess = true;
        e.WebView.Settings.MediaPlaybackRequiresUserGesture = false;
        e.WebView.Settings.SetGeolocationEnabled(true);

        if (!OperatingSystem.IsAndroidVersionAtLeast(24))
        {
            e.WebView.Settings.SetGeolocationDatabasePath(e.WebView.Context?.FilesDir?.Path);
        }

        var handler = GetPermissionHandler(e.WebView.WebChromeClient!, activity);
        e.WebView.SetWebChromeClient(handler);
    }

    protected virtual PermissionHandler GetPermissionHandler(WebChromeClient blazorWebChromeClient, ComponentActivity activity)
    {
        var handler = new PermissionHandler(blazorWebChromeClient, activity);

        if (CameraRationale is not null)
        {
            handler.AddCameraPermission(CameraRationale);
        }

        if (MicrophoneRationale is not null)
        {
            handler.AddMicrophonePermission(MicrophoneRationale);
        }

        if (GeolocationRationale is not null)
        {
            handler.AddGeolocationPermission(GeolocationRationale);
        }
        
        return handler;
    }

}
