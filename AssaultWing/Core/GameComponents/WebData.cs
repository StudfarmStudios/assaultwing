using System;
using System.Globalization;
using System.Net;
using System.Text;
using AW2.Core;
using AW2.Helpers;

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
            nextScheduledGameRequest.BeginGetResponse(RequestDone, nextScheduledGameRequest);
        }

        private void RequestDone(IAsyncResult result)
        {
            try
            {
                var request = (WebRequest)result.AsyncState;
                var response = request.EndGetResponse(result);
                var buffer = new byte[response.ContentLength];
                using (var stream = response.GetResponseStream()) stream.Read(buffer, 0, buffer.Length);
                var dateTime = DateTime.Parse(UTF8Encoding.UTF8.GetString(buffer), CultureInfo.InvariantCulture);
                NextScheduledGame = dateTime;
            }
            catch (Exception e)
            {
                Log.Write("Error while requesting time of next scheduled game", e);
            }
        }
    }
}
