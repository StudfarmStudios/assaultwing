using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Net.Sockets;
using AW2.Helpers;
using AW2.Core;
using AW2.Net.ConnectionUtils;

namespace AW2.Net
{
    /// <summary>
    /// Sends game statistics to statistics server.
    /// </summary>
    public static class Stats
    {
        private static bool g_initialized;
        private static AWTCPSocket g_statsDataSocket;

        public static void Initialize(AssaultWing game)
        {
            if (g_initialized) throw new InvalidOperationException("Already initialized");
            var statsDataSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var statsEndPoint = MiscHelper.ParseIPEndPoint(game.Settings.Net.StatsServerAddress);
            statsEndPoint.Port = game.Settings.Net.StatsDataPort;
            statsDataSocket.Connect(statsEndPoint);
            g_statsDataSocket = new AWTCPSocket(statsDataSocket, null);
            g_initialized = true;
        }

        public static void Dispose()
        {
            if (!g_initialized) throw new InvalidOperationException("Not initialized");
            g_statsDataSocket.Dispose();
            g_initialized = false;
        }

        public static void Send(object obj)
        {
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(obj) + "\r\n";
            Log.Write("!!! Sending JSON '{0}'", json);
            g_statsDataSocket.Send(writer => writer.WriteStringWithoutLength(json));
        }

    }
}
