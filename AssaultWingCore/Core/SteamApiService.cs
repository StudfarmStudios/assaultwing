using AW2.Helpers;
using Steamworks;

namespace AW2.Core
{
  public class SteamApiService : IDisposable
  {
    public bool Initialized { get; private set; }

    public SteamApiService()
    {
      InitializeSteamApi();
      SetupCallbacks();
    }
    protected Callback<GameOverlayActivated_t> gameOverlayActivatedCallback;

    public void InitializeSteamApi()
    {
      if (Initialized)
      {
        return;
      }

      Initialized = SteamAPI.Init();
      if (Initialized)
      {
        Log.Write("SteamAPI initialized.");
      }
      else
      {
        Log.Write("SteamAPI.Init() failed.");
        // Should we throw here? throw new ApplicationException("SteamAPI.Init() failed.")
        // Throwing is also problematic bc then it seems the error messages from SteamAPI don't have time to
        // get logged.
      }
    }

    private void SetupCallbacks()
    {
      gameOverlayActivatedCallback = Callback<GameOverlayActivated_t>.Create(OnGameOverlayActivated);
    }

    public string UserNick {
      get {return SteamFriends.GetPersonaName();}
    }

    private void OnGameOverlayActivated(GameOverlayActivated_t callback)
    {
      if (callback.m_bActive != 0)
      {
        Log.Write("Steam Overlay has been activated");
      }
      else
      {
        Log.Write("Steam Overlay has been closed");
      }
    }

    public void Dispose()
    {
      Initialized = false;
      SteamAPI.Shutdown();
      Log.Write("SteamAPI.Shutdown()");
    }

  }
}