using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using AW2.Core;
using AW2.Game;
using AW2.Game.Players;
using AW2.Helpers;
using AW2.Net.ConnectionUtils;

namespace AW2.Net
{
    /// <summary>
    /// Collects game statistics to a remote statistics server.
    /// </summary>
    public class StatsSender : StatsBase
    {
        private static readonly byte[] STATS_NOT_ALLOWED_MESSAGE = Encoding.UTF8.GetBytes("NOT ALLOWED");

        // TODO !!! Investigate using class Connection to handle the connection to the stats server.
        private bool _disposed;
        private AWTCPSocket _statsDataSocket;
        private StringBuilder _sendQueue;
        private AWTimer _sendTimer;
        private AWTimer _connectTimer;
        private bool _isConnecting;
        private bool _statsNotAllowed;

        private bool Connected { get { return _statsDataSocket != null && !_statsDataSocket.IsDisposed; } }

        public StatsSender(AssaultWingCore game, int updateOrder)
            : base(game, updateOrder)
        {
            _connectTimer = new AWTimer(() => game.GameTime.TotalGameTime, TimeSpan.FromSeconds(5)) { SkipPastIntervals = true };
            _sendTimer = new AWTimer(() => game.GameTime.TotalGameTime, TimeSpan.FromSeconds(1)) { SkipPastIntervals = true };
            _sendQueue = new StringBuilder(2048);
        }

        public override void Update()
        {
            if (!Connected)
                ConnectToStatsServer();
            else
            {
                if (_sendTimer.IsElapsed) SendToStatsServer();
                HandleSocketErrors();
            }
        }

        public override void Dispose()
        {
            if (_disposed) return;
            if (_statsDataSocket != null) _statsDataSocket.Dispose();
            _statsDataSocket = null;
            _disposed = true;
        }

        public override void Send(object obj)
        {
            if (Game.NetworkMode != NetworkMode.Server || !Connected || !Game.Settings.Net.StatsReportingEnabled) return;
            _sendQueue.Append(Newtonsoft.Json.JsonConvert.SerializeObject(obj)).Append("\r\n");
        }

        public override void SendHit(Gob hitter, Gob target, Vector2? pos)
        {
            if (hitter.Owner == null || target.Owner == null) return;
            var game = hitter.Game;
            Send(new
            {
                Hit = hitter.TypeName.Value,
                HitOwner = hitter.Owner.StatsData.LoginToken,
                Target = target.TypeName.Value,
                TargetOwner = target.Owner.StatsData.LoginToken,
                Pos = pos.HasValue ? pos.Value : hitter.Pos,
                BirthPos = hitter.BirthPos,
                Type = game.DataEngine.Minions.Contains(target) ? "Minion" : "Misc",
            });
        }

        public override object GetStatsObject(Spectator spectator)
        {
            return new
            {
                Name = spectator.Name,
                LoginToken = spectator.StatsData.LoginToken,
                Connected = !spectator.IsDisconnected,
            };
        }

        public override string GetStatsString(Spectator spectator)
        {
            return spectator.StatsData.LoginToken;
        }

        private void HandleSocketErrors()
        {
            var errorsFound = false;
            _statsDataSocket.Errors.Do(queue =>
            {
                while (queue.Count > 0)
                {
                    var e = queue.Dequeue();
                    Log.Write("Stats server connection: " + e);
                    errorsFound = true;
                }
            });
            if (errorsFound) _statsDataSocket.Dispose();
        }

        private void ConnectToStatsServer()
        {
            if (Game.NetworkMode != NetworkMode.Server || _statsNotAllowed || _isConnecting || !_connectTimer.IsElapsed ||
                Game.Settings.Net.StatsServerAddress == "") return;
            _isConnecting = true;
            try
            {
                var statsDataSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                var statsEndPoint = MiscHelper.ParseIPEndPoint(Game.Settings.Net.StatsServerAddress);
                statsEndPoint.Port = Game.Settings.Net.StatsDataPort;
                var args = new SocketAsyncEventArgs { RemoteEndPoint = statsEndPoint };
                args.Completed += ConnectCompleted;
                statsDataSocket.ConnectAsync(args);
            }
            catch (NotSupportedException)
            {
                // May happen during Socket.ConnectAsync
                _isConnecting = false;
            }
            catch (ArgumentException)
            {
                // May happen during MiscHelper.ParseIPEndPoint
                _isConnecting = false;
            }
        }

        private void SendToStatsServer()
        {
            if (_sendQueue.Length == 0) return;
            _statsDataSocket.AddToSendBuffer(writer => writer.WriteStringWithoutLength(_sendQueue.ToString()));
            _statsDataSocket.FlushSendBuffer(); // TODO !!! Replace _sendQueue by smart use of FlushSendBuffer()
            _sendQueue.Clear();
        }

        private void ConnectCompleted(object sender, SocketAsyncEventArgs args)
        {
            if (args.SocketError == SocketError.Success)
            {
                Log.Write("Connection established to statistics server");
                _statsDataSocket = new AWTCPSocket(args.ConnectSocket, StatsMessageHandler);
                BasicInfoSent = false;
            }
            _isConnecting = false;
        }

        private int StatsMessageHandler(ArraySegment<byte> messageHeaderAndBody, IPEndPoint remoteEndPoint)
        {
            // "\r\n" marks the end of a JSON serialized object.
            var bytesRead = 0;
            var startIndex = messageHeaderAndBody.Offset;
            for (int i = messageHeaderAndBody.Offset; i < messageHeaderAndBody.Offset + messageHeaderAndBody.Count - 1; i++)
            {
                if (messageHeaderAndBody.Array[i] != '\r' || messageHeaderAndBody.Array[i + 1] != '\n') continue;
                var nextStartIndex = i + 2;
                try
                {
                    var str = Encoding.UTF8.GetString(messageHeaderAndBody.Array, startIndex, i - startIndex);
                    if (StatsNotAllowed(messageHeaderAndBody))
                    {
                        Log.Write("Battlefront statistics won't be collected from this server.");
                        _statsDataSocket.Dispose();
                        _statsNotAllowed = true;
                        return messageHeaderAndBody.Count;
                    }
                    var obj = JObject.Parse(str);
                    var loginToken = obj.GetString("token");
                    var announcement = obj.GetString("announcement");
                    if (loginToken != "")
                    {
                        var spectator = GetSpectatorOrNull(loginToken);
                        if (spectator != null) spectator.StatsData.Update(obj);
                    }
                    else if (announcement != "")
                    {
                        var message = new PlayerMessage(obj.GetString("announcement"), PlayerMessage.DEFAULT_COLOR);
                        if (obj.GetString("all") == "true")
                            foreach (var plr in Game.DataEngine.Players) plr.Messages.Add(message);
                        else
                        {
                            var plr = GetSpectatorOrNull(obj.GetString("to")) as Player;
                            if (plr != null) plr.Messages.Add(message);
                        }
                    }
                }
                catch (ArgumentException) { } // Encoding.GetString failed
                catch (JsonReaderException) { } // JsonConvert.DeserializeObject failed
                bytesRead += nextStartIndex - startIndex;
                startIndex = nextStartIndex;
            }
            return bytesRead;
        }

        private bool StatsNotAllowed(ArraySegment<byte> messageHeaderAndBody)
        {
            if (messageHeaderAndBody.Count < STATS_NOT_ALLOWED_MESSAGE.Length) return false;
            for (int i = 0; i < STATS_NOT_ALLOWED_MESSAGE.Length; i++)
                if (messageHeaderAndBody.Array[messageHeaderAndBody.Offset + i] != STATS_NOT_ALLOWED_MESSAGE[i])
                    return false;
            return true;
        }

        private Spectator GetSpectatorOrNull(string loginToken)
        {
            return Game.DataEngine.Spectators.FirstOrDefault(spec => spec.StatsData.LoginToken == loginToken);
        }
    }
}
