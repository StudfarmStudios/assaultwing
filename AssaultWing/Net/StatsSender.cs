using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Xna.Framework;
using AW2.Core;
using AW2.Game;
using AW2.Helpers;
using AW2.Net.ConnectionUtils;
using Newtonsoft.Json;

namespace AW2.Net
{
    /// <summary>
    /// Collects game statistics to a remote statistics server.
    /// </summary>
    public class StatsSender : StatsBase
    {
        // TODO !!! Investigate using class Connection to handle the connection to the stats server.
        private bool _disposed;
        private AWTCPSocket _statsDataSocket;
        private StringBuilder _sendQueue;
        private AWTimer _sendTimer;
        private AWTimer _connectTimer;
        private bool _isConnecting;

        private bool Connected { get { return _statsDataSocket != null && !_statsDataSocket.IsDisposed; } }

        public StatsSender(AssaultWingCore game, int updateOrder)
            : base(game, updateOrder)
        {
            _connectTimer = new AWTimer(game, TimeSpan.FromSeconds(5));
            _sendTimer = new AWTimer(game, TimeSpan.FromSeconds(1));
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
            if (Game.NetworkMode != NetworkMode.Server || !Connected) return;
            _sendQueue.Append(Newtonsoft.Json.JsonConvert.SerializeObject(obj)).Append("\r\n");
        }

        public override void SendHit(Gob hitter, Gob target, Vector2? pos)
        {
            if (hitter.Owner == null || target.Owner == null) return;
            var game = hitter.Game;
            Send(new
            {
                Hit = hitter.TypeName.Value,
                HitOwner = hitter.Owner.LoginToken,
                Target = target.TypeName.Value,
                TargetOwner = target.Owner.LoginToken,
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
                LoginToken = spectator.LoginToken,
                Connected = !spectator.IsDisconnected,
            };
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
            if (Game.NetworkMode != NetworkMode.Server || _isConnecting || !_connectTimer.IsElapsed ||
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
                    var obj = JsonConvert.DeserializeObject(str);
                    // TODO: Interpret obj like this:
                    // {PlayerDetails: pilot.token, Rating: pilot.rating}
                    // {NewRating: this.token, Rating: rating}
                }
                catch (ArgumentException) { } // Encoding.GetString failed
                catch (JsonReaderException) { } // JsonConvert.DeserializeObject failed
                bytesRead += nextStartIndex - startIndex;
                startIndex = nextStartIndex;
            }
            return bytesRead;
        }
    }
}
