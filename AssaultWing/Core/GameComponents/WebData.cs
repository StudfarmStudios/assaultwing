using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using AW2.Core;
using AW2.Game;
using AW2.Helpers;
using AW2.Net.ConnectionUtils;
using Newtonsoft.Json.Linq;

namespace AW2.Net
{
    /// <summary>
    /// Provides data from the web.
    /// </summary>
    public class WebData : AWGameComponent
    {
        /// <summary>
        /// Date and time of the next scheduled game, or null if not known.
        /// </summary>
        public DateTime? NextScheduledGame { get; private set; }

        /// <summary>
        /// Errors that have occurred during pilot login attempts.
        /// </summary>
        public ThreadSafeWrapper<Queue<string>> LoginErrors { get; private set; }

        public new AssaultWing Game { get { return (AssaultWing)base.Game; } }

        public WebData(AssaultWingCore game, int updateOrder)
            : base(game, updateOrder)
        {
            LoginErrors = new ThreadSafeWrapper<Queue<string>>(new Queue<string>());
        }

        public void RequestData()
        {
            var nextScheduledGameUri = Game.Settings.Net.DataServerAddress + "/nextgame";
            var nextScheduledGameRequest = WebRequest.Create(nextScheduledGameUri);
            nextScheduledGameRequest.BeginGetResponse(NextScheduledGameRequestDone, nextScheduledGameRequest);
        }

        public void Feed(string tag)
        {
            RequestFromStats(FeedDone, "feed", "t=" + tag);
        }

        public void LoginPilots(bool reportFailure = false)
        {
            if (Game.Settings.Net.StatsServerAddress == "")
            {
                if (reportFailure) EnqueueLoginError("Login server not specified.", "");
                return;
            }
            foreach (var spec in Game.DataEngine.Spectators)
            {
                if (spec.GetStats().IsLoggedIn) continue;
                var plrs = Game.Settings.Players;
                var password = plrs.Player1.Name == spec.Name ? plrs.Player1.Password :
                    spec.Name == AW2.Settings.PlayerSettings.BOTS_NAME ? plrs.BotsPassword :
                    "";
                if (password == "")
                {
                    if (reportFailure) EnqueueLoginError("No password given.", spec.Name);
                }
                else
                    BeginRequestPlayerLoginToken(spec.Name, password);
            }
        }

        public void UpdatePilotData(Spectator spectator, string loginToken)
        {
            var requestPath = string.Format("pilot/token/{0}", loginToken);
            RequestFromStats(response => PilotDataRequestDone(response, spectator, "pilot data"), requestPath);
        }

        public void UpdatePilotRanking(Spectator spectator)
        {
            if (spectator.GetStats().PilotId == null) return;
            var requestPath = string.Format("pilot/id/{0}/rankings", spectator.GetStats().PilotId);
            RequestFromStats(response => PilotDataRequestDone(response, spectator, "pilot ranking"), requestPath);
        }

        public void UnloginPilots()
        {
            foreach (var spec in Game.DataEngine.Spectators) spec.GetStats().Logout();
        }

        private void RequestFromStats(AsyncCallback callback, string path, string query = null)
        {
            if (Game.Settings.Net.StatsServerAddress == "") return;
            if (ServicePointManager.ServerCertificateValidationCallback == null)
                ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, errors) => true;
            var uriBuilder = new UriBuilder("https", Game.Settings.Net.StatsServerAddress, Game.Settings.Net.StatsHttpsPort, path)
            {
                Query = query,
            };
            var loginRequest = WebRequest.Create(uriBuilder.Uri);
            loginRequest.BeginGetResponse(callback, loginRequest);
        }

        private void BeginRequestPlayerLoginToken(string name, string password)
        {
            RequestFromStats(LoginRequestDone, "login", string.Format("username={0}&password={1}", name, password));
        }

        private void NextScheduledGameRequestDone(IAsyncResult result)
        {
            RequestDone(result, "next scheduled game request", response =>
            {
                var dateTime = DateTime.Parse(response, CultureInfo.InvariantCulture);
                NextScheduledGame = dateTime;
            });
        }

        private void FeedDone(IAsyncResult result)
        {
            RequestDone(result, "feed", responseString =>
            {
                // Response is whatever.
            });
        }

        private void LoginRequestDone(IAsyncResult result)
        {
            RequestDone(result, "pilot login", responseString =>
            {
                var response = JObject.Parse(responseString);
                if (response["error"] != null) EnqueueLoginError(response["error"] + ".", response.GetString("data", "username"));
                var username = response.GetString("username");
                if (username == "") return;
                var spectator = Game.DataEngine.Spectators.FirstOrDefault(plr => plr.IsLocal && plr.Name == username);
                if (spectator == null) return;
                spectator.GetStats().LoginTime = Game.GameTime.TotalRealTime;
                spectator.GetStats().Update(response);
                UpdatePilotRanking(spectator);
            });
        }

        private void PilotDataRequestDone(IAsyncResult result, Spectator spectator, string requestName)
        {
            RequestDone(result, requestName, responseString =>
            {
                var response = JObject.Parse(responseString);
                if (response["error"] != null) Log.Write("Error in {0} query: {1}", requestName, response["error"]);
                spectator.GetStats().Update(response);
            });
        }

        private void EnqueueLoginError(string error, string username)
        {
            var errorPrelude = username != ""
                ? "Login error for " + username
                : "Login error";
            Log.Write("{0}: {1}", errorPrelude, error);
            LoginErrors.Do(queue => queue.Enqueue(errorPrelude + ".\n" + error));
        }

        private void RequestDone(IAsyncResult result, string requestName, Action<string> handleResponse)
        {
            try
            {
                var request = (WebRequest)result.AsyncState;
                var response = request.EndGetResponse(result);
                var buffer = new byte[response.ContentLength];
                using (var stream = response.GetResponseStream()) stream.Read(buffer, 0, buffer.Length);
                handleResponse(UTF8Encoding.UTF8.GetString(buffer));
            }
            catch (Exception e)
            {
                Log.Write("Problem during " + requestName, e);
            }
        }
    }
}
