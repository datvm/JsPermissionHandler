using Foundation;
using UIKit;

namespace JsPermissionHandler;

partial class PermissionHandler
{

    public static partial async Task OpenAppPermissionPanelAsync(string? windowsScheme)
    {
        var url = new NSUrl(UIApplication.OpenSettingsUrlString);
        await UIApplication.SharedApplication.OpenUrlAsync(url, new UIApplicationOpenUrlOptions());
    }

}
