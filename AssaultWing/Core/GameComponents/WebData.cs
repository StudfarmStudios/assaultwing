using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using AW2.Core;
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

        public void LoginPilots(bool reportFailure = false)
        {
            if (ServicePointManager.ServerCertificateValidationCallback == null)
                ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, errors) => true;
            if (reportFailure && Game.Settings.Net.StatsServerAddress == "")
            {
                EnqueueLoginError("Login server not specified.", "");
                return;
            }
            foreach (var spec in Game.DataEngine.Spectators)
            {
                if (!string.IsNullOrEmpty(spec.LoginToken)) continue;
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

        public void UnloginPilots()
        {
            foreach (var spec in Game.DataEngine.Spectators) spec.LoginToken = "";
        }

        private void BeginRequestPlayerLoginToken(string name, string password)
        {
            var net = Game.Settings.Net;
            var loginRequest = WebRequest.Create(new UriBuilder("https", net.StatsServerAddress, net.StatsHttpsPort, "login")
            {
                Query = string.Format("username={0}&password={1}", name, password)
            }.Uri);
            loginRequest.BeginGetResponse(LoginRequestDone, loginRequest);
        }

        private void NextScheduledGameRequestDone(IAsyncResult result)
        {
            RequestDone(result, "next scheduled game request", response =>
            {
                var dateTime = DateTime.Parse(response, CultureInfo.InvariantCulture);
                NextScheduledGame = dateTime;
            });
        }

        private void LoginRequestDone(IAsyncResult result)
        {
            RequestDone(result, "pilot login", responseString =>
            {
                var response = JObject.Parse(responseString);
                var error = response["error"];
                if (error != null) EnqueueLoginError(error + ".", GetJsonString(response, "data", "username"));
                var token = response["token"];
                if (token != null)
                {
                    var player = Game.DataEngine.Spectators.FirstOrDefault(plr => plr.Name == response["username"].ToString());
                    if (player != null) player.LoginToken = token.ToString();
                }
            });
        }

        private string GetJsonString(JObject root, params string[] path)
        {
            if (path == null || path.Length == 0) throw new ArgumentException("Invalid JSON path");
            var element = root[path[0]];
            if (element == null) return "";
            foreach (var step in path.Skip(1)){
                if (element[step] == null) return "";
                element = element[step];
            }
            return element.ToString();
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
