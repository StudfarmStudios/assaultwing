using Steamworks;
using AW2.Helpers;

namespace AW2.Core
{
  public class SteamServerComponent : AWGameComponent
  {
    // To keep the callback registrations live
    private List<IDisposable> callbacks = new List<IDisposable>();

    private bool Initialized;

    public SteamServerComponent(AssaultWingCore game, int updateOrder)
        : base(game, updateOrder)
    {
    }

    private static void DebugTextHook(int severity, System.Text.StringBuilder message)
    {
      Log.Write($"Steam server debug message: [severity {severity}] {message}");
    }

    private SteamAPIWarningMessageHook_t steamAPIWarningMessageHook;

    public override void Initialize() {
      if (!Game.Services.GetService<SteamApiService>().Initialized)
      {
        Log.Write("Can't start Steam server functionality, the steam API is not initialized");
        return;
      }

      Log.Write("Starting steam server functionality");

      SetupCallbacks();

      Initialized = InitializeSteamGameServerApi();

      steamAPIWarningMessageHook = new SteamAPIWarningMessageHook_t(DebugTextHook);
      SteamGameServerUtils.SetWarningMessageHook(steamAPIWarningMessageHook);

      // SteamGameServer.SetModDir(...); do we need this? Space war example sets this to game dir, but docs say it is default empty which is ok
  		SteamGameServer.SetModDir("AssaultWing");
		  SteamGameServer.SetProduct("Assault Wing");
		  SteamGameServer.SetGameDescription("A fast-paced physics-based shooter for many players over the internet.");

      if (Initialized) {
        // TODO: Support for server accounts? (LogOn and not LogOnAnonymous)
        Log.Write("Logging in steam server");
        SteamGameServer.LogOnAnonymous(); // does not work?
        SteamGameServer.SetAdvertiseServerActive(true);
      }
    }
    public bool InitializeSteamGameServerApi()
    {

      uint chosenIp = 0;
      ushort GamePort = 16727;
      ushort QueryPort = 16726;
      var serverMode = EServerMode.eServerModeNoAuthentication;
      var assaultWingVersion = "0.0.0.0";
      // https://github.com/rlabrecque/Steamworks.NET/blob/master/com.rlabrecque.steamworks.net/Runtime/Steam.cs#L157
      // https://github.com/rlabrecque/Steamworks.NET/blob/master/com.rlabrecque.steamworks.net/Runtime/autogen/SteamEnums.cs#L1297
      Log.Write("Initializing Steam GameServer");
      var initialized = GameServer.Init(chosenIp, GamePort, QueryPort, serverMode, assaultWingVersion);
      if (initialized)
      {
        Log.Write("Steam GameServer initialized.");
      }
      else
      {
        Log.Write("GameServer.Init() failed.");
        // Should we throw here? throw new ApplicationException("SteamAPI.Init() failed.")
        // Throwing is also problematic bc then it seems the error messages from SteamAPI don't have time to
        // get logged.
      }
      return initialized;
    }

    private void SetupCallbacks()
    {
      callbacks.Clear();
      callbacks.Add(Callback<SteamServersConnected_t>.CreateGameServer(OnSteamServersConnected));
      callbacks.Add(Callback<SteamServerConnectFailure_t>.CreateGameServer(OnSteamServersConnectFailure));
      callbacks.Add(Callback<SteamServersDisconnected_t>.CreateGameServer(OnSteamServersDisconnected));
      callbacks.Add(Callback<GSPolicyResponse_t>.CreateGameServer(OnPolicyResponse));
      callbacks.Add(Callback<ValidateAuthTicketResponse_t>.CreateGameServer(OnValidateAuthTicketResponse));
      callbacks.Add(Callback<P2PSessionRequest_t>.CreateGameServer(OnP2PSessionRequest));
      callbacks.Add(Callback<P2PSessionConnectFail_t>.CreateGameServer(OnP2PSessionConnectFail));
    }

    private void OnSteamServersConnected(SteamServersConnected_t tag)
    {
      Log.Write("Server connected to Steam");
      SendUpdatedServerDetailsToSteam();
    }
    void OnSteamServersConnectFailure(SteamServerConnectFailure_t failure)
    {
      Log.Write($"Server failed to connect to Steam, result: {failure.m_eResult}, still retrying: {failure.m_bStillRetrying}");
    }

    void OnSteamServersDisconnected(SteamServersDisconnected_t result)
    {
      Log.Write($"Server lost connection to Steam, result: {result.m_eResult}");
    }

    void OnPolicyResponse(GSPolicyResponse_t response)
    {
      Log.Write($"Steam server policy response received, secure: {response.m_bSecure}");
      // TODO: VAC secure and server auth stuff here
    }
    void OnValidateAuthTicketResponse(ValidateAuthTicketResponse_t response) {
      Log.Write($"Steam validate auth ticket response received: {response.m_eAuthSessionResponse}");
    }

    void OnP2PSessionRequest(P2PSessionRequest_t request) {
      Log.Write($"Steam P2P Session Request, remote steam id: {request.m_steamIDRemote}");
    }

    void OnP2PSessionConnectFail(P2PSessionConnectFail_t request) {
      Log.Write($"Steam P2P Session connect failure, remote steam id: {request.m_steamIDRemote}");
    }


    void SendUpdatedServerDetailsToSteam()
    {
      SteamGameServer.SetMaxPlayerCount(64); // TODO: Max players
      SteamGameServer.SetPasswordProtected(false);
      SteamGameServer.SetServerName("TODO server");
      SteamGameServer.SetBotPlayerCount(0); // optional, defaults to zero
      SteamGameServer.SetMapName("TOOD map");
    }

    public override void Update()
    {
      if (!Initialized)
      {
        return;
      }

      GameServer.RunCallbacks();
    }

    public override void Dispose()
    {
      Log.Write("Steam GameServer.Shutdown()");
      if (Initialized) {
        GameServer.Shutdown();
      }
    }

  }
}