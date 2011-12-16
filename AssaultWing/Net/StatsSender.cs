using System;
using System.Net.Sockets;
using System.Text;
using Microsoft.Xna.Framework;
using AW2.Core;
using AW2.Game;
using AW2.Helpers;
using AW2.Net.ConnectionUtils;

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
            Send(new
            {
                Hit = hitter.TypeName.Value,
                HitOwner = hitter.Owner.LoginToken,
                Target = target.TypeName.Value,
                TargetOwner = target.Owner.LoginToken,
                Pos = pos.HasValue ? pos.Value : hitter.Pos,
                BirthPos = hitter.BirthPos,
            });
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
            if (Game.NetworkMode != NetworkMode.Server || _isConnecting || !_connectTimer.IsElapsed) return;
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
            catch (ArgumentException)
            {
                // May happen during MiscHelper.ParseIPEndPoint
                _isConnecting = false;
            }
        }

        private void SendToStatsServer()
        {
            if (_sendQueue.Length == 0) return;
            _statsDataSocket.Send(writer => writer.WriteStringWithoutLength(_sendQueue.ToString()));
            _sendQueue.Clear();
        }

        private void ConnectCompleted(object sender, SocketAsyncEventArgs args)
        {
            if (args.SocketError == SocketError.Success)
            {
                _statsDataSocket = new AWTCPSocket(args.ConnectSocket, null);
                BasicInfoSent = false;
            }
            _isConnecting = false;
        }
    }
}
