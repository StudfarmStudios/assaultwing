using System;
using System.Collections.Specialized;
using AW2.Core;
using AW2.Helpers;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace AW2.Browser.Pages;

public partial class Index : ComponentBase, IDisposable
{
    private AssaultWingProgram _program;
    private DotNetObjectReference<Index> _selfReference;

    protected override void OnAfterRender(bool firstRender)
    {
        if (!firstRender)
            return;

        BrowserInterop.Initialize((IJSInProcessRuntime)JsRuntime);
        _selfReference = DotNetObjectReference.Create(this);
        _ = JsRuntime.InvokeVoidAsync("assaultWingBrowser.start", _selfReference);
    }

    [JSInvokable]
    public void TickDotNet()
    {
        try
        {
            if (_program == null)
            {
                var options = new CommandLineOptions(
                    Array.Empty<string>(), new NameValueCollection(), string.Empty);
                _program = new AssaultWingProgram(options);
                _program.Run();
            }

            _program.Tick();
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Assault Wing browser frame failed:\n{exception}");
            throw;
        }
    }

    public void Dispose()
    {
        _program?.Dispose();
        _selfReference?.Dispose();
    }
}
