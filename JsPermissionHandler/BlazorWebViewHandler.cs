namespace JsPermissionHandler;

public partial class BlazorWebViewHandler
{
    const string CameraAccessRationale = "This app requires access to your camera. Please grant access to your camera when requested.";
    const string LocationAccessRationale = "This app requires access to your location. Please grant access to your precise location when requested.";
    const string MicrophoneAccessRationale = "This app requires access to your microphone. Please grant access to your microphone when requested.";

    public string? CameraRationale { get; set; }
    public string? MicrophoneRationale { get; set; }
    public string? GeolocationRationale { get; set; }

    public void AddInitializingHandler(BlazorWebView blazorWebView)
    {
        blazorWebView.BlazorWebViewInitializing += BlazorWebView_BlazorWebViewInitializing;
        blazorWebView.BlazorWebViewInitialized += BlazorWebView_BlazorWebViewInitialized;
    }

    public BlazorWebViewHandler AddCamera(string rationale = CameraAccessRationale)
    {
        CameraRationale = rationale;
        return this;
    }

    public BlazorWebViewHandler AddMicrophone(string rationale = MicrophoneAccessRationale)
    {
        MicrophoneRationale = rationale;
        return this;
    }

    public BlazorWebViewHandler AddGeolocation(string rationale = LocationAccessRationale)
    {
        GeolocationRationale = rationale;
        return this;
    }

    protected virtual partial void BlazorWebView_BlazorWebViewInitializing(object? sender, BlazorWebViewInitializingEventArgs e);

    protected virtual partial void BlazorWebView_BlazorWebViewInitialized(object? sender, BlazorWebViewInitializedEventArgs e);

}
