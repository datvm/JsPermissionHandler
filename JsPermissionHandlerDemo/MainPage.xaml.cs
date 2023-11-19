using JsPermissionHandler;

namespace JsPermissionHandlerDemo;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();

        new BlazorWebViewHandler()            
            .AddCamera()
            .AddMicrophone()
            .AddGeolocation()
            .AddInitializingHandler(blazorWebView);
    }
}
