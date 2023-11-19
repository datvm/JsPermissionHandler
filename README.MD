This is a MAUI Blazor library to simplify permission management for Blazor Javascript APIs like camera, microphone (through `getUserMedia`) or `geolocation`. This is accomplished hugely thanks to [MackinnonBuck/MauiBlazorPermissionsExample](https://github.com/MackinnonBuck/JsPermissionHandlersExample). It worked for many of my projects so it's time to pack it to easily reuse it.

This library is highly customizable and extensible. Most methods are `virtual` and can be overriden to fit your needs.

See a full demo project at [project Github](https://github.com/datvm/JsPermissionHandler/tree/master/JsPermissionHandlerDemo).

This package does not support Tizen.

# Installation

Install the [NuGet package](https://www.nuget.org/packages/JsPermissionHandlers) in your project:

```ps
dotnet add package JsPermissionHandlers
```

# Setup Permissions

## Windows

You do not need to do anything special to use this library on Windows.

## Android

Add permissions to your `AndroidManifest.xml` file (in `Platforms/Android`).

```xml
<uses-permission android:name="android.permission.CAMERA" />
<uses-permission android:name="android.permission.RECORD_AUDIO" />
<uses-permission android:name="android.permission.MODIFY_AUDIO_SETTINGS" />
<uses-permission android:name="android.permission.ACCESS_FINE_LOCATION" />
```

Note: only add what you need. For `getUserMedia` with audio, you need both `RECORD_AUDIO` and `MODIFY_AUDIO_SETTINGS`.

## iOS

Add permissions to your `Info.plist` file (in `Platforms/iOS`).

```xml
<key>NSLocationWhenInUseUsageDescription</key>
<string>This app requires access to your location. Please grant access to your precise location when requested.</string>
<key>NSCameraUsageDescription</key>
<string>This app requires access to your camera. Please grant access to your camera when requested.</string>
<key>NSMicrophoneUsageDescription</key>
<string>This app requires access to your microphone. Please grant access to your microphone when requested.</string>
```

# Add the Handler to your WebView

In your `MainPage.xaml.cs` file, add the following code:

```cs
public MainPage()
{
    InitializeComponent();

    new BlazorWebViewHandler()            
        // Add whichever you need:
        .AddCamera()
        .AddMicrophone()
        .AddGeolocation()
        // blazorWebView is the name of your BlazorWebView
        .AddInitializingHandler(blazorWebView);
}
```

And that's it, you can now use your Javascript APIs in the Blazor Webview.

# Additional permissions

The default `BlazorWebViewHandler` gives the three common permissions call `AddCamera`, `AddMicrophone` and `AddGeolocation`. If you need more, you need to inherit `PermissionHandler` and add your own. This is only needed for Android. For example:

```cs
// MyHandler.Android.cs

public class MyHandler : PermissionHandler
{
	public MyHandler AddMyPermission() =>
        AddWebkitPermission(
            [
                (Manifest.Permission.ModifyAudioSettings, null),
                (Manifest.Permission.RecordAudio, rationale),
            ],
            PermissionRequest.ResourceAudioCapture
        );
}
```

# Opening Permission panel

Usually working with permissions flow requires to open the permission panel especially when user denies your app access to permission. This is done with the `PermissionHandler.OpenAppPermissionPanelAsync` static method.

```cs
public static partial Task OpenAppPermissionPanelAsync(string? windowsScheme = null);
```

- `windowsScheme` is used on Windows only. You can specify which [Windows Settings panel](https://learn.microsoft.com/en-us/windows/uwp/launch-resume/launch-settings-app#ms-settings-uri-scheme-reference) to open. If `null`, the default is `ms-settings:appsfeatures-app` to open the settings of the current app (which would include permissions).
![Windows Permission](https://github.com/datvm/JsPermissionHandler/assets/6388546/b4dd4310-7791-49e9-9509-4e52c584a351)
- On Android, this method opens the app settings panel. It's not possible to open the permission panel directly.
- On iOS, this method opens the app settings panel which includes the permissions.
