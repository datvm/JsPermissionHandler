using MauiBlazorPermission;

namespace MauiBlazorPermissionDemo;

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
