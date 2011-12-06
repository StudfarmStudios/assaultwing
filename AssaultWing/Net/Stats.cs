using System;
using System.Net.Sockets;
using AW2.Helpers;
using AW2.Core;
using AW2.Net.ConnectionUtils;

namespace AW2.Net
{
    /// <summary>
    /// Sends game statistics to statistics server.
    /// </summary>
    public class Stats : StatsBase
    {
        private bool _disposed;
        private AWTCPSocket _statsDataSocket;

        public Stats(AssaultWingCore game)
        {
            var statsDataSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var statsEndPoint = MiscHelper.ParseIPEndPoint(game.Settings.Net.StatsServerAddress);
            statsEndPoint.Port = game.Settings.Net.StatsDataPort;
            statsDataSocket.Connect(statsEndPoint);
            _statsDataSocket = new AWTCPSocket(statsDataSocket, null);
        }

        public override void Dispose()
        {
            if (_disposed) return;
            _statsDataSocket.Dispose();
            _statsDataSocket = null;
            _disposed = true;
        }

        public override void Send(object obj)
        {
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(obj) + "\r\n";
            _statsDataSocket.Send(writer => writer.WriteStringWithoutLength(json));
        }

    }
}
