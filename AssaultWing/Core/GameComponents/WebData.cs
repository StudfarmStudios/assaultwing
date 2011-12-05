using System;
using System.Linq;
using System.Globalization;
using System.Net;
using System.Text;
using AW2.Core;
using AW2.Helpers;
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

        public WebData(AssaultWingCore game, int updateOrder)
            : base(game, updateOrder)
        {
        }

        public void RequestData()
        {
            var nextScheduledGameUri = Game.Settings.Net.DataServerAddress + "/nextgame";
            var nextScheduledGameRequest = WebRequest.Create(nextScheduledGameUri);
            nextScheduledGameRequest.BeginGetResponse(NextScheduledGameRequestDone, nextScheduledGameRequest);
        }

        public void LoginPilot()
        {
            if (ServicePointManager.ServerCertificateValidationCallback == null)
                ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, errors) => true;
            var net = Game.Settings.Net;
            var player = Game.Settings.Players.Player1;
            var loginRequest = WebRequest.Create(new UriBuilder("https", net.StatsServerAddress, net.StatsHttpsPort, "login") { Query = string.Format("username={0}&password={1}", player.Name, player.Password) }.Uri);
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
                if (error != null)
                {
                    Log.Write("Login error: " + error);
                    // TODO !!! show popup
                }
                var token = response["token"];
                if (token != null)
                {
                    var player = Game.DataEngine.Players.FirstOrDefault(plr => plr.Name == response["username"].ToString());
                    if (player != null) player.LoginToken = token.ToString();
                }
            });
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
