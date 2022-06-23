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
    }

    public void InitializeSteamApi()
    {
      if (Initialized) {
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

    public void Dispose()
    {
      Initialized = false;
      SteamAPI.Shutdown();
    }

  }
}