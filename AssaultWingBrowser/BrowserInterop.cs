using Microsoft.JSInterop;

namespace AW2.Helpers;

/// <summary>Small synchronous browser services used by the original game loop.</summary>
public static class BrowserInterop
{
    private const string SettingsKey = "assaultwing.settings.xml";
    private static IJSInProcessRuntime _jsRuntime;

    public static void Initialize(IJSInProcessRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public static string LoadSettings()
    {
        return _jsRuntime.Invoke<string>("assaultWingBrowser.loadSettings", SettingsKey);
    }

    public static void SaveSettings(string xml)
    {
        _jsRuntime.Invoke<object>("assaultWingBrowser.saveSettings", SettingsKey, xml);
    }

    public static void OpenUrl(string url)
    {
        _jsRuntime.Invoke<object>("assaultWingBrowser.openUrl", url);
    }
}
