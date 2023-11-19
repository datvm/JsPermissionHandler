using JsPermissionHandler;
using Microsoft.JSInterop;

namespace JsPermissionHandlerDemo.Components.Pages;

partial class Home
{

    bool noPermission;

    bool video = true;
    bool audio = true;
    string? geolocation;

    IJSObjectReference app = null!;

    protected override async Task OnInitializedAsync()
    {
        var mod = await Js.InvokeAsync<IJSObjectReference>(
            "import", "/js/interop.js");
        app = await mod.InvokeAsync<IJSObjectReference>("getApp");
    }

    async Task OpenPermissionPanel()
    {
        await PermissionHandler.OpenAppPermissionPanelAsync();
    }

    async Task OpenMicPermissionPanel()
    {
        await PermissionHandler.OpenAppPermissionPanelAsync("ms-settings:privacy-microphone");
    }

    async Task RequestUserMediaAsync()
    {
        noPermission = false;
        StateHasChanged();

        try
        {
            await app.InvokeVoidAsync("requestStreamAsync", audio, video);
        }
        catch
        {
            noPermission = true;
        }
    }

    async Task StopStreamAsync()
    {
        await app.InvokeVoidAsync("disposeStream");
    }

    async Task RequestGeolocationAsync()
    {
        noPermission = false;
        StateHasChanged();

        try
        {
            geolocation = await app.InvokeAsync<string>("requestGeoAsync");
        }
        catch
        {
            noPermission = true;
        }
    }

}
