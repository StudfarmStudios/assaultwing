using AW2.Helpers;
using Steamworks;

namespace AW2.Core
{

    public class SteamGameServerService : IDisposable
    {
        public bool Initialized { get; private set; }
        private SteamAPIWarningMessageHook_t? steamAPIWarningMessageHook;

        public SteamGameServerService(SteamApiService steamApiService, ushort port)
        {
            Initialized = InitializeSteamGameServerApi(port);
            if (Initialized)
            {
                steamAPIWarningMessageHook = new SteamAPIWarningMessageHook_t(DebugTextHook);
                SteamGameServerUtils.SetWarningMessageHook(steamAPIWarningMessageHook);
                steamApiService.SetupServerCallbacks();
            }
        }

        private bool InitializeSteamGameServerApi(ushort port)
        {
            Log.Write("Initializing Steam GameServer");
            try
            {
                uint chosenIp = 0;
                ushort QueryPort = port;
                ushort GamePort = (ushort)(port + 1);
                var serverMode = EServerMode.eServerModeNoAuthentication;
                var assaultWingVersion = "0.0.0.0";
                // https://github.com/rlabrecque/Steamworks.NET/blob/master/com.rlabrecque.steamworks.net/Runtime/Steam.cs#L157
                // https://github.com/rlabrecque/Steamworks.NET/blob/master/com.rlabrecque.steamworks.net/Runtime/autogen/SteamEnums.cs#L1297
                var initialized = GameServer.Init(chosenIp, GamePort, QueryPort, serverMode, assaultWingVersion);
                if (initialized)
                {
                    Log.Write("Steam GameServer initialized.");
                }
                else
                {
                    Log.Write("Steam GameServer.Init() failed.");
                    // Should we throw here? throw new ApplicationException("SteamAPI.Init() failed.")
                    // Throwing is also problematic bc then it seems the error messages from SteamAPI don't have time to
                    // get logged.
                }
                return initialized;
            }
            catch (Exception e)
            {
                Log.Write("Steam GameServer.Init() failed with error", e);
                return false;
            }
        }

        private static void DebugTextHook(int severity, System.Text.StringBuilder message)
        {
            Log.Write($"Steam server debug message: [severity {severity}] {message}");
        }

        public void Dispose()
        {
            if (SteamGameServer.BLoggedOn())
            {
                Log.Write("Steam GameServer.LogOff()");
                SteamGameServer.LogOff();
            }
            Log.Write("Steam GameServer.Shutdown()");
            GameServer.Shutdown();
        }
    }
}
