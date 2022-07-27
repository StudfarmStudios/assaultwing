using AW2.Helpers;
using Steamworks;

namespace AW2.Core
{

    public class SteamServerBrowser : IDisposable
    {
        private readonly AppId_t AppId;

        private ISteamMatchmakingServerListResponse ServerListResponse { get; init; }

        private HServerListRequest? ServerListRequest;

        public SteamServerBrowser()
        {
            ServerListResponse = new ISteamMatchmakingServerListResponse(ServerResponded, ServerFailedToRespond, RefreshComplete);
            AppId = SteamUtils.GetAppID();
        }

        private void ServerResponded(HServerListRequest request, int serverIndex)
        {
            Log.Write($"ServerResponded {serverIndex}");
        }

        private void ServerFailedToRespond(HServerListRequest request, int serverIndex)
        {
            Log.Write($"ServerFailedToRespond {serverIndex}");
            var details = SteamMatchmakingServers.GetServerDetails(request, serverIndex);
            Log.Write($"server details {serverIndex} version:{details.m_nServerVersion} map:{details.GetMap()} name:{details.GetServerName()} addr:{details.m_NetAdr.GetConnectionAddressString()}");
        }

        private void RefreshComplete(HServerListRequest request, EMatchMakingServerResponse response)
        {
            Log.Write($"RefreshComplete {response}");
            Log.Write($"GetServerCount {SteamMatchmakingServers.GetServerCount(request)}");
        }

        public void RequestServerList()
        {
            Cancel();
            ServerListRequest = SteamMatchmakingServers.RequestInternetServerList(AppId, new MatchMakingKeyValuePair_t[] { }, 0, ServerListResponse);
        }

        public void Cancel()
        {
            if (ServerListRequest is not null)
            {
                var query = ServerListRequest;
                ServerListRequest = null;
                SteamMatchmakingServers.CancelQuery(ServerListRequest.Value);
            }
        }

        public void Dispose()
        {
            Cancel();
        }
    }
}