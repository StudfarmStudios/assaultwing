using AW2.Helpers;
using Steamworks;
using AW2.Net;

namespace AW2.Core
{

    public class SteamServerBrowser : IDisposable
    {
        public delegate void HandleServerDelegate(GameServerInfo info);
        private readonly AppId_t AppId;

        private readonly HandleServerDelegate HandleServer;

        private ISteamMatchmakingServerListResponse ServerListResponse { get; init; }

        private HServerListRequest? ServerListRequest;

        public SteamServerBrowser(HandleServerDelegate handleServer)
        {
            HandleServer = handleServer;
            ServerListResponse = new ISteamMatchmakingServerListResponse(ServerResponded, ServerFailedToRespond, RefreshComplete);
            AppId = SteamUtils.GetAppID();
        }

        private void ServerResponded(HServerListRequest request, int serverIndex)
        {
            var server = GameServerInfoForRequest(request, serverIndex);
            Log.Write($"ServerResponded {server}");
            HandleServer(server);
        }

        private void ServerFailedToRespond(HServerListRequest request, int serverIndex)
        {
            var server = GameServerInfoForRequest(request, serverIndex);
            Log.Write($"ServerFailedToRespond {server}");
            HandleServer(server);
        }

        private void RefreshComplete(HServerListRequest request, EMatchMakingServerResponse response)
        {
            Log.Write($"RefreshComplete {response} GetServerCount {SteamMatchmakingServers.GetServerCount(request)}");
        }

        public GameServerInfo GameServerInfoForRequest(HServerListRequest request, int serverIndex)
        {
            return new GameServerInfo
            {
                ServerIndex = serverIndex,
                SteamDetails = SteamMatchmakingServers.GetServerDetails(request, serverIndex),
                AWVersion = MiscHelper.Version,
            };
        }

        public void RequestInternetServerList()
        {
            Cancel();
            Log.Write("RequestInternetServerList");
            ServerListRequest = SteamMatchmakingServers.RequestInternetServerList(AppId, new MatchMakingKeyValuePair_t[] { }, 0, ServerListResponse);
        }

        public void RequestLanServerList()
        {
            Cancel();
            Log.Write("RequestLANServerList");
            ServerListRequest = SteamMatchmakingServers.RequestLANServerList(AppId, ServerListResponse);
        }

        public void Cancel()
        {
            var query = ServerListRequest;
            if (query is not null)
            {
                ServerListRequest = null;
                Log.Write("Cancel ServerListRequest");
                SteamMatchmakingServers.CancelQuery(query.Value);
            }
        }

        public void Dispose()
        {
            Cancel();
        }
    }
}
