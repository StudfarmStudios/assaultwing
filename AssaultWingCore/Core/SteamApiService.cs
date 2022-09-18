using AW2.Helpers;
using Steamworks;

namespace AW2.Core
{

  public class SteamApiService : IDisposable
  {
    // TODO: Move the callback stuff to a separate service bc it is shared with SteamGameServerService
    public class CallbackBundleKey {};

    interface ICallbackBundle<out T> : IDisposable where T : CallbackBundleKey {}

    public class CallbackBundle<T> : ICallbackBundle<T> where T : CallbackBundleKey, new() {
      private List<IDisposable> Callbacks = new List<IDisposable>();
      public bool Disposed => Callbacks.Count == 0;
      public static readonly CallbackBundle<T> Bundle = new CallbackBundle<T>();

      public void Add<C>(Callback<C> callback) {
        Callbacks.Add(callback);
      }

      public void Dispose() {
        var callbacks = Callbacks;
        Callbacks.Clear();
        foreach (var cb in callbacks) {
          cb.Dispose();
        }
      }
    };

    private Dictionary<Type, ICallbackBundle<CallbackBundleKey>> CallbackBundles = new Dictionary<Type, ICallbackBundle<CallbackBundleKey>>();

    CallbackBundle<T> GetCallbackBundle<T>() where T : CallbackBundleKey, new() {
        var bundle = CallbackBundle<T>.Bundle;
        if (!CallbackBundles.ContainsKey(typeof(T))) {
          CallbackBundles.Add(typeof(T), bundle);
        }
        return bundle;
    }

    public bool Initialized { get; private set; }

    public SteamApiService()
    {
      InitializeSteamApi();
      if (Initialized) {
        SetupCallbacks();
      }
    }
    private SteamAPIWarningMessageHook_t? steamAPIWarningMessageHook;

    private static void DebugTextHook(int severity, System.Text.StringBuilder message)
    {
      Log.Write($"Steam debug message: [severity {severity}] {message}");
    }

    public void InitializeSteamApi()
    {
      if (Initialized)
      {
        return;
      }

      try {
        Initialized = SteamAPI.Init();
        if (Initialized)
        {
          Log.Write("SteamAPI initialized.");
          steamAPIWarningMessageHook = new SteamAPIWarningMessageHook_t(DebugTextHook);
          SteamClient.SetWarningMessageHook(steamAPIWarningMessageHook);
        }
        else
        {
          Log.Write("SteamAPI.Init() failed.");
          // Should we throw here? throw new ApplicationException("SteamAPI.Init() failed.")
          // Throwing is also problematic bc then it seems the error messages from SteamAPI don't have time to
          // get logged.
        }
      } catch (Exception e) {
        // This can happen if DLL's are not in the right place at least
        Log.Write("SteamAPI.Init() failed with error", e);
      }
    }

    public void Callback<K, T>(Callback<T>.DispatchDelegate func) where K : CallbackBundleKey, new() {
      GetCallbackBundle<K>().Add(Callback<T>.Create(func));
    }

    public void ServerCallback<K, T>(Callback<T>.DispatchDelegate func) where K : CallbackBundleKey, new() {
      GetCallbackBundle<K>().Add(Callback<T>.CreateGameServer(func));
    }

    private class DebugLoggingBundle : CallbackBundleKey {};

    private void SetupCallbacks()
    {
      Callback<DebugLoggingBundle, GameOverlayActivated_t>(LogGameOverlayActivated);
      Callback<DebugLoggingBundle, SteamNetConnectionStatusChangedCallback_t>(LogSteamNetConnectionStatusChanged);
      Callback<DebugLoggingBundle, SteamRelayNetworkStatus_t>(LogSteamRelayNetworkStatus);
    }

    public void SetupServerCallbacks()
    {
      ServerCallback<DebugLoggingBundle, SteamNetConnectionStatusChangedCallback_t>(LogSteamNetConnectionStatusChanged);
      ServerCallback<DebugLoggingBundle, SteamRelayNetworkStatus_t>(LogSteamRelayNetworkStatus);
    }

    public string UserNick
    {
      get { return SteamFriends.GetPersonaName(); }
    }

    public void ActivateGameOverlayToWebPage(string url)
    {
      SteamFriends.ActivateGameOverlayToWebPage(url);
    }

    private void LogSteamRelayNetworkStatus(SteamRelayNetworkStatus_t status) {
      Log.Write($"SteamRelayNetworkStatus available: {status.m_eAvail}, any relay available: {status.m_eAvailAnyRelay}, config: {status.m_eAvailNetworkConfig}");
    }

    private void LogGameOverlayActivated(GameOverlayActivated_t callback)
    {
      Log.Write($"Steam Overlay: {callback.m_bActive}");
    }

    public static string NetStateToString(ESteamNetworkingConnectionState state) {
      return state.ToString().Replace("k_ESteamNetworkingConnectionState_", "");
    }

    void LogSteamNetConnectionStatusChanged(SteamNetConnectionStatusChangedCallback_t status) {
      var endReason = (status.m_info.m_eEndReason != 0 || status.m_info.m_szEndDebug.Length > 0) ?
        $" endReason: {status.m_info.m_eEndReason} / \"{status.m_info.m_szEndDebug}\"" : "";
      Log.Write($"Connection: {status.m_info.m_szConnectionDescription}: {NetStateToString(status.m_eOldState)} -> {NetStateToString(status.m_info.m_eState)}{endReason}");
    }

    public void DisposeCallbackBundle<T>() where T : CallbackBundleKey {
      var t = typeof(T);
      CallbackBundles.GetValueOrDefault(t)?.Dispose();
    }

    public void Dispose()
    {
      if (Initialized) {
        Initialized = false;
        var bundles = CallbackBundles;
        foreach (var b in bundles) {
          b.Value.Dispose();
        }
        SteamAPI.Shutdown();
        Log.Write("SteamAPI.Shutdown()");
      }
    }

  }
}