using Steamworks;
using AW2.Helpers;

namespace AW2.Core
{
    public class SteamServerComponent : AWGameComponent
    {
        // To keep the callback registrations live
        private List<IDisposable> callbacks = new List<IDisposable>();

        private static readonly TimeSpan GAME_SERVER_DETAILS_UPDATE_INTERVAL = TimeSpan.FromSeconds(30);
        private TimeSpan _lastNetworkItemsUpdate;

        private bool EnabledMirror;
        private bool ConsoleServer;

        public  SteamServerComponent(AssaultWingCore game, int updateOrder, bool consoleServer, ushort port)
            : base(game, updateOrder)
        {
            SteamGameServerService? steamGameServerService = Game.Services.GetService<SteamGameServerService>();
            if (steamGameServerService is null)
            {
                steamGameServerService = new SteamGameServerService(Game.Services.GetService<SteamApiService>(), port: port);
                Game.Services.AddService(steamGameServerService);
            }
            ConsoleServer = consoleServer;
            SetupCallbacks();
        }

        protected override void EnabledOrVisibleChanged()
        {
            if (Enabled)
                Enable();
            else
                Disable();
        }

        private void Enable()
        {
            if (!Game.IsSteam)
            {
                return;
            }
            if (EnabledMirror)
                return;
            EnabledMirror = true;

            // SteamGameServer.SetModDir(...); do we need this? Space war example sets this to game dir, but docs say it is default empty which is ok
            SteamGameServer.SetModDir("AssaultWing");
            SteamGameServer.SetProduct("Assault Wing");
            SteamGameServer.SetGameDescription("A fast-paced physics-based shooter for many players over the internet.");
            SteamGameServer.SetDedicatedServer(ConsoleServer); // Our terminology does not match the steams.
            SendUpdatedServerDetailsToSteam();

            // TODO: Support for server accounts? (LogOn and not LogOnAnonymous)
            Log.Write("Logging in steam server");
            SteamGameServer.LogOnAnonymous(); // does not work?
            SteamGameServer.SetAdvertiseServerActive(true);
        }

        public void Disable()
        {
            if (!EnabledMirror)
                return;
            EnabledMirror = false;

            SteamGameServer.SetAdvertiseServerActive(false);
            Log.Write("SteamGameServer.LogOff()");
            SteamGameServer.LogOff();
        }

        private void ClearCallbacks()
        {
            foreach (var callback in callbacks)
            {
                callback.Dispose();
            }
            callbacks.Clear();
        }

        private void SetupCallbacks()
        {
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
            var steamId = SteamGameServer.GetSteamID();
            Log.Write($"Server logged on to Steam, Steam id:{steamId}, anonAccount: {steamId.BAnonAccount()}, gameServerAccount: {steamId.BGameServerAccount()}, individualAccount: {steamId.BIndividualAccount()}");
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
        void OnValidateAuthTicketResponse(ValidateAuthTicketResponse_t response)
        {
            Log.Write($"Steam validate auth ticket response received: {response.m_eAuthSessionResponse}");
        }

        void OnP2PSessionRequest(P2PSessionRequest_t request)
        {
            Log.Write($"Steam P2P Session Request, remote steam id: {request.m_steamIDRemote}");
        }

        void OnP2PSessionConnectFail(P2PSessionConnectFail_t request)
        {
            Log.Write($"Steam P2P Session connect failure, remote steam id: {request.m_steamIDRemote}");
        }


        public void SendUpdatedServerDetailsToSteam()
        {
            if (!EnabledMirror)
            {
                return;
            }

            _lastNetworkItemsUpdate = Game.GameTime.TotalRealTime;

            SteamGameServer.SetServerName(Game.Settings.Net.GameServerName);
            SteamGameServer.SetMapName(Game.SelectedArenaName);
            SteamGameServer.SetKeyValue("GameplayMode", Game.DataEngine.GameplayMode?.Name ?? "");
            SteamGameServer.SetKeyValue("WaitingPlayerCount", $"{Game.DataEngine.Spectators.Count - Game.DataEngine.Players.Count()}");
            SteamGameServer.SetBotPlayerCount(Game.DataEngine.Minions.Count());
            SteamGameServer.SetMaxPlayerCount(Game.Settings.Net.GameServerMaxPlayers);
            SteamGameServer.SetPasswordProtected(false);
        }

        public override void Update()
        {
            if (!EnabledMirror)
            {
                return;
            }

            GameServer.RunCallbacks();

            if (_lastNetworkItemsUpdate + GAME_SERVER_DETAILS_UPDATE_INTERVAL <= Game.GameTime.TotalRealTime)
            {
                SendUpdatedServerDetailsToSteam(); // periodically publish things like player count
            }
        }

        public override void Dispose()
        {
            if (EnabledMirror)
            {
                Disable();
            }
            ClearCallbacks();
        }

    }
}
