using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Webkit;
using AndroidX.Activity;
using AndroidX.Activity.Result;
using AndroidX.Activity.Result.Contract;
using AndroidX.Core.Content;
using Android.Provider;
using Java.Interop;
using View = Android.Views.View;
using WebView = Android.Webkit.WebView;
using Android.Util;

namespace MauiBlazorPermission;

partial class PermissionHandler : WebChromeClient, IActivityResultCallback
{
    public static partial async Task OpenAppPermissionPanelAsync(string? windowsScheme)
    {
        var ctx = await Platform.WaitForActivityAsync();

        Intent intent = new(Settings.ActionApplicationDetailsSettings);
        var uri = Android.Net.Uri.FromParts("package", ctx.PackageName, null);
        intent.SetData(uri);

        ctx.StartActivity(intent);
    }

    // This class implements a permission requesting workflow that matches workflow recommended
    // by the official Android developer documentation.
    // See: https://developer.android.com/training/permissions/requesting#workflow_for_requesting_permissions
    // The current implementation supports location, camera, and microphone permissions. To add your own,
    // update the s_rationalesByPermission dictionary to include your rationale for requiring the permission.
    // If necessary, you may need to also update s_requiredPermissionsByWebkitResource to define how a specific
    // Webkit resource maps to an Android permission.

    public Dictionary<string, string> RationalesByPermission { get; set; } = [];
    public Dictionary<string, string[]> RequiredPermissionsByWebkitResource { get; set; } = [];

    private readonly WebChromeClient blazorWebChromeClient;
    private readonly ComponentActivity activity;
    private readonly ActivityResultLauncher _requestPermissionLauncher;

    private Action<bool>? _pendingPermissionRequestCallback;

    public PermissionHandler(WebChromeClient blazorWebChromeClient, ComponentActivity activity)
    {
        this.blazorWebChromeClient = blazorWebChromeClient;
        this.activity = activity;
        _requestPermissionLauncher = activity.RegisterForActivityResult(new ActivityResultContracts.RequestPermission(), this);
    }

    public PermissionHandler AddWebkitPermission(IEnumerable<(string permission, string? rationale)> permissions, string? webkitResource)
    {
        foreach (var (p, rationale) in permissions)
        {
            if (rationale is not null)
            {
                RationalesByPermission[p] = rationale;
            }
        }

        if (webkitResource is not null)
        {
            RequiredPermissionsByWebkitResource[webkitResource] = permissions.Select(p => p.permission).ToArray();
        }

        return this;
    }

    public PermissionHandler AddCameraPermission(string rationale) =>
        AddWebkitPermission(
            [(Manifest.Permission.Camera, rationale)],
            PermissionRequest.ResourceVideoCapture
        );

    public PermissionHandler AddGeolocationPermission(string rationale) =>
        AddWebkitPermission(
            [(Manifest.Permission.AccessFineLocation, rationale)],
            null
        );

    public PermissionHandler AddMicrophonePermission(string rationale) =>
        AddWebkitPermission(
            [
                (Manifest.Permission.ModifyAudioSettings, null),
                (Manifest.Permission.RecordAudio, rationale),
            ],
            PermissionRequest.ResourceAudioCapture
        );

    public override void OnCloseWindow(WebView? window)
    {
        blazorWebChromeClient.OnCloseWindow(window);
        _requestPermissionLauncher.Unregister();
    }

    public override void OnGeolocationPermissionsShowPrompt(string? origin, GeolocationPermissions.ICallback? callback)
    {
        ArgumentNullException.ThrowIfNull(callback, nameof(callback));

        RequestPermission(Manifest.Permission.AccessFineLocation, isGranted => callback.Invoke(origin, isGranted, false));
    }

    public override void OnPermissionRequest(PermissionRequest? request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        if (request.GetResources() is not { } requestedResources)
        {
            request.Deny();
            return;
        }

        RequestAllResources(requestedResources, grantedResources =>
        {
            if (grantedResources.Count == 0)
            {
                request.Deny();
            }
            else
            {
                request.Grant([.. grantedResources]);
            }
        });
    }

    private void RequestAllResources(Memory<string> requestedResources, Action<List<string>> callback)
    {
        if (requestedResources.Length == 0)
        {
            // No resources to request - invoke the callback with an empty list.
            callback([]);
            return;
        }

        var currentResource = requestedResources.Span[0];
        var requiredPermissions = RequiredPermissionsByWebkitResource.GetValueOrDefault(currentResource, Array.Empty<string>());

        RequestAllPermissions(requiredPermissions, isGranted =>
        {
            // Recurse with the remaining resources. If the first resource was granted, use a modified callback
            // that adds the first resource to the granted resources list.
            RequestAllResources(requestedResources[1..], !isGranted ? callback : grantedResources =>
            {
                grantedResources.Add(currentResource);
                callback(grantedResources);
            });
        });
    }

    private void RequestAllPermissions(Memory<string> requiredPermissions, Action<bool> callback)
    {
        if (requiredPermissions.Length == 0)
        {
            // No permissions left to request - success!
            callback(true);
            return;
        }

        RequestPermission(requiredPermissions.Span[0], isGranted =>
        {
            if (isGranted)
            {
                // Recurse with the remaining permissions.
                RequestAllPermissions(requiredPermissions[1..], callback);
            }
            else
            {
                // The first required permission was not granted. Fail now and don't attempt to grant
                // the remaining permissions.
                callback(false);
            }
        });
    }

    protected virtual void RequestPermission(string permission, Action<bool> callback)
    {
        // This method implements the workflow described here:
        // https://developer.android.com/training/permissions/requesting#workflow_for_requesting_permissions

        if (ContextCompat.CheckSelfPermission(activity, permission) == Permission.Granted)
        {
            callback.Invoke(true);
        }
        else if (activity.ShouldShowRequestPermissionRationale(permission) && RationalesByPermission.TryGetValue(permission, out var rationale))
        {
            ShowPromptDialog(permission, rationale, callback);
        }
        else
        {
            LaunchPermissionRequestActivity(permission, callback);
        }
    }

    protected virtual AlertDialog ShowPromptDialog(string permission, string rationale, Action<bool> callback) =>
        new AlertDialog.Builder(activity)
            .SetTitle(GetDialogTitle())!
            .SetMessage(rationale)!
            .SetNegativeButton(GetNegativeButtonText(), (_, _) => callback(false))!
            .SetPositiveButton(GetPositiveButtonText(), (_, _) => LaunchPermissionRequestActivity(permission, callback))!
            .Show()!;

    protected virtual string GetDialogTitle() => "Enable app permissions";

    protected virtual string GetNegativeButtonText() => "No thanks";
    protected virtual string GetPositiveButtonText() => "Continue";

    private void LaunchPermissionRequestActivity(string permission, Action<bool> callback)
    {
        if (_pendingPermissionRequestCallback is not null)
        {
            throw new InvalidOperationException("Cannot perform multiple permission requests simultaneously.");
        }

        _pendingPermissionRequestCallback = callback;
        _requestPermissionLauncher.Launch(permission);
    }

    void IActivityResultCallback.OnActivityResult(Java.Lang.Object? isGranted)
    {
        var callback = _pendingPermissionRequestCallback;
        _pendingPermissionRequestCallback = null;
        callback?.Invoke(isGranted is not null && (bool)isGranted);
    }

    #region Unremarkable overrides
    // See: https://github.com/dotnet/maui/issues/6565
    public override JniPeerMembers JniPeerMembers => blazorWebChromeClient.JniPeerMembers;
    public override Bitmap? DefaultVideoPoster => blazorWebChromeClient.DefaultVideoPoster;
    public override View? VideoLoadingProgressView => blazorWebChromeClient.VideoLoadingProgressView;
    public override void GetVisitedHistory(IValueCallback? callback)
        => blazorWebChromeClient.GetVisitedHistory(callback);
    public override bool OnConsoleMessage(ConsoleMessage? consoleMessage)
        => blazorWebChromeClient.OnConsoleMessage(consoleMessage);
    public override bool OnCreateWindow(WebView? view, bool isDialog, bool isUserGesture, Message? resultMsg)
        => blazorWebChromeClient.OnCreateWindow(view, isDialog, isUserGesture, resultMsg);
    public override void OnGeolocationPermissionsHidePrompt()
        => blazorWebChromeClient.OnGeolocationPermissionsHidePrompt();
    public override void OnHideCustomView()
        => blazorWebChromeClient.OnHideCustomView();
    public override bool OnJsAlert(WebView? view, string? url, string? message, JsResult? result)
        => blazorWebChromeClient.OnJsAlert(view, url, message, result);
    public override bool OnJsBeforeUnload(WebView? view, string? url, string? message, JsResult? result)
        => blazorWebChromeClient.OnJsBeforeUnload(view, url, message, result);
    public override bool OnJsConfirm(WebView? view, string? url, string? message, JsResult? result)
        => blazorWebChromeClient.OnJsConfirm(view, url, message, result);
    public override bool OnJsPrompt(WebView? view, string? url, string? message, string? defaultValue, JsPromptResult? result)
        => blazorWebChromeClient.OnJsPrompt(view, url, message, defaultValue, result);
    public override void OnPermissionRequestCanceled(PermissionRequest? request)
        => blazorWebChromeClient.OnPermissionRequestCanceled(request);
    public override void OnProgressChanged(WebView? view, int newProgress)
        => blazorWebChromeClient.OnProgressChanged(view, newProgress);
    public override void OnReceivedIcon(WebView? view, Bitmap? icon)
        => blazorWebChromeClient.OnReceivedIcon(view, icon);
    public override void OnReceivedTitle(WebView? view, string? title)
        => blazorWebChromeClient.OnReceivedTitle(view, title);
    public override void OnReceivedTouchIconUrl(WebView? view, string? url, bool precomposed)
        => blazorWebChromeClient.OnReceivedTouchIconUrl(view, url, precomposed);
    public override void OnRequestFocus(WebView? view)
        => blazorWebChromeClient.OnRequestFocus(view);
    public override void OnShowCustomView(View? view, ICustomViewCallback? callback)
        => blazorWebChromeClient.OnShowCustomView(view, callback);
    public override bool OnShowFileChooser(WebView? webView, IValueCallback? filePathCallback, FileChooserParams? fileChooserParams)
        => blazorWebChromeClient.OnShowFileChooser(webView, filePathCallback, fileChooserParams);
    #endregion
}
