using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.Xna.Framework;
using AW2.Core;
using AW2.Game;
using AW2.Helpers;
using AW2.Helpers.Serialization;
using AW2.Net.Connections;
using AW2.Net.ManagementMessages;
using AW2.Net.Messages;
using AW2.UI;

namespace AW2.Net.MessageHandling
{
    public class MessageHandlers
    {
        public event Action<string> GameServerConnectionClosing; // parameter is info

        private AssaultWing Game { get; set; }

        public MessageHandlers(AssaultWing game)
        {
            Game = game;
        }

        public void ActivateHandlers(IEnumerable<MessageHandlerBase> handlers)
        {
            Game.NetworkEngine.MessageHandlers.AddRange(handlers);
        }

        public void DeactivateHandlers(IEnumerable<MessageHandlerBase> handlers)
        {
            var handlerTypesToRemove = handlers.Select(handler => handler.GetType());
            foreach (var handler in Game.NetworkEngine.MessageHandlers)
                if (handlerTypesToRemove.Contains(handler.GetType())) handler.Dispose();
        }

        public IEnumerable<MessageHandlerBase> GetStandaloneMenuHandlers(Action<GameServerListReply> handleGameServerListReply)
        {
            yield return new MessageHandler<GameServerListReply>(MessageHandlerBase.SourceType.Management, handleGameServerListReply);
            yield return new MessageHandler<JoinGameServerReply>(MessageHandlerBase.SourceType.Management, HandleJoinGameServerReply);

            // These messages only game servers receive
            yield return new MessageHandler<PingMessage>(MessageHandlerBase.SourceType.Management, HandlePingMessage);
        }

        public IEnumerable<MessageHandlerBase> GetClientMenuHandlers()
        {
            yield return new MessageHandler<ConnectionClosingMessage>(MessageHandlerBase.SourceType.Server, HandleConnectionClosingMessage);
            yield return new MessageHandler<StartGameMessage>(MessageHandlerBase.SourceType.Server, HandleStartGameMessage);
            yield return new MessageHandler<SpectatorSettingsReply>(MessageHandlerBase.SourceType.Server, HandleSpectatorSettingsReply);
            yield return new MessageHandler<SpectatorSettingsRequest>(MessageHandlerBase.SourceType.Server, HandleSpectatorSettingsRequestOnClient);
            yield return new MessageHandler<PlayerDeletionMessage>(MessageHandlerBase.SourceType.Server, HandlePlayerDeletionMessage);
            yield return new MessageHandler<GameSettingsRequest>(MessageHandlerBase.SourceType.Server, HandleGameSettingsRequest);
            yield return new MessageHandler<PlayerMessageMessage>(MessageHandlerBase.SourceType.Server, HandlePlayerMessageMessageOnClient);
            yield return new MessageHandler<ArenaFinishMessage>(MessageHandlerBase.SourceType.Server, HandleArenaFinishMessage);
        }

        public IEnumerable<MessageHandlerBase> GetClientGameplayHandlers()
        {
            var networkEngine = Game.NetworkEngine;
            yield return new MessageHandler<SpectatorUpdateMessage>(MessageHandlerBase.SourceType.Server, HandleSpectatorUpdateMessage);
            yield return new MessageHandler<PlayerDeletionMessage>(MessageHandlerBase.SourceType.Server, HandlePlayerDeletionMessage);
            yield return new GameplayMessageHandler<GobCreationMessage>(MessageHandlerBase.SourceType.Server, networkEngine, Game.HandleGobCreationMessage) { OneMessageAtATime = true };
            yield return new GameplayMessageHandler<GobUpdateMessage>(MessageHandlerBase.SourceType.Server, networkEngine, HandleGobUpdateMessageOnClient);
            yield return new GameplayMessageHandler<GobDeletionMessage>(MessageHandlerBase.SourceType.Server, networkEngine, HandleGobDeletionMessage);
            yield return new MessageHandler<ArenaStatisticsMessage>(MessageHandlerBase.SourceType.Server, HandleArenaStatisticsMessage);
        }

        public IEnumerable<MessageHandlerBase> GetServerMenuHandlers()
        {
            yield return new MessageHandler<GameServerHandshakeRequestTCP>(MessageHandlerBase.SourceType.Client, HandleGameServerHandshakeRequestTCP);
            yield return new MessageHandler<SpectatorSettingsRequest>(MessageHandlerBase.SourceType.Client, HandleSpectatorSettingsRequestOnServer);
            yield return new MessageHandler<PlayerMessageMessage>(MessageHandlerBase.SourceType.Client, HandlePlayerMessageMessageOnServer);
        }

        public IEnumerable<MessageHandlerBase> GetServerGameplayHandlers()
        {
            var networkEngine = Game.NetworkEngine;
            yield return new MessageHandler<PlayerControlsMessage>(MessageHandlerBase.SourceType.Client, HandlePlayerControlsMessage);
            yield return new GameplayMessageHandler<GobUpdateMessage>(MessageHandlerBase.SourceType.Client, networkEngine, HandleGobUpdateMessageOnServer);
        }

        public void IncomingConnectionHandlerOnServer(Result<AW2.Net.Connections.Connection> result, Func<bool> allowNewConnection)
        {
            if (!result.Successful)
                Log.Write("Some client failed to connect: " + result.Error);
            else if (allowNewConnection())
            {
                Game.NetworkEngine.GameClientConnections.Add((GameClientConnection)result.Value);
                Log.Write("Server obtained {0} from {1}", result.Value.Name, result.Value.RemoteTCPEndPoint);
            }
            else
            {
                var mess = new ConnectionClosingMessage { Info = "game server refused joining" };
                result.Value.Send(mess);
                Log.Write("Server refused connection from " + result.Value.RemoteTCPEndPoint);
            }
        }

        #region Handler implementations

        private void HandleJoinGameServerReply(JoinGameServerReply mess)
        {
            if (Game.NetworkMode == NetworkMode.Client) return;
            if (mess.Success)
            {
                Game.SoundEngine.PlaySound("MenuChangeItem");
                Game.StartClient(mess.GameServerEndPoints);
            }
            else
                Game.NetworkingErrors.Enqueue("Couldn't connect to server:\n" + mess.FailMessage); // TODO: Proper line wrapping in dialogs
        }

        private void HandlePingMessage(PingMessage mess)
        {
            Game.NetworkEngine.ManagementServerConnection.Send(new PongMessage());
            Game.NetworkEngine.ManagementServerConnection.OnPingReceived();
        }

        private void HandleSpectatorSettingsRequestOnClient(SpectatorSettingsRequest mess)
        {
            var spectatorSerializationMode = SerializationModeFlags.ConstantDataFromServer | SerializationModeFlags.VaryingDataFromServer;
            var spectator = Game.DataEngine.Spectators.FirstOrDefault(
                spec => spec.ID == mess.SpectatorID && spec.ServerRegistration != Spectator.ServerRegistrationType.No);
            if (spectator == null)
            {
                var newSpectator = TryCreateAndAddNewSpectator(mess, spectatorSerializationMode);
                newSpectator.ID = mess.SpectatorID;
                newSpectator.ServerRegistration = Spectator.ServerRegistrationType.Yes;
            }
            else if (spectator.IsRemote)
            {
                mess.Read(spectator, spectatorSerializationMode, 0);
            }
            else
            {
                if (mess.Subclass != SpectatorSettingsRequest.SubclassType.Player) throw new ApplicationException("Unexpected Spectator subclass " + mess.Subclass);
                // Be careful not to overwrite our most recent name and equipment choices
                // with something older from the server.
                // TODO !!! Instead of creating a temp player, serialize only Player.Color when mode is ConstantDataFromServer
                // and the player lives at a remote client.
                var tempPlayer = GetTempPlayer();
                mess.Read(tempPlayer, spectatorSerializationMode, 0);
                if (spectator is Player) ((Player)spectator).Color = tempPlayer.Color;
            }
        }

        private void HandleConnectionClosingMessage(ConnectionClosingMessage mess)
        {
            GameServerConnectionClosing(mess.Info);
        }

        private void HandleStartGameMessage(StartGameMessage mess)
        {
            if (Game.DataEngine.Arena != null && Game.DataEngine.Arena.ID == mess.ArenaID) return;
            Game.DataEngine.ArenaFinishTime = mess.ArenaTimeLeft == TimeSpan.Zero ? TimeSpan.Zero : mess.ArenaTimeLeft + Game.GameTime.TotalRealTime;
            Game.PrepareArena(mess.ArenaToPlay, mess.ArenaID, mess.WallCount);
        }

        private void HandleSpectatorSettingsReply(SpectatorSettingsReply mess)
        {
            var spectator = Game.DataEngine.Spectators.FirstOrDefault(plr => plr.LocalID == mess.SpectatorLocalID);
            if (spectator == null) throw new ApplicationException("Cannot find unregistered local spectator with local ID " + mess.SpectatorLocalID);
            if (mess.Success)
            {
                spectator.ServerRegistration = Spectator.ServerRegistrationType.Yes;
                spectator.ID = mess.SpectatorID;
                // If we reconnected, remove the duplicate spectator that was sent by the server earlier.
                Game.DataEngine.Spectators.Remove(spec => spec.ID == spectator.ID && spec != spectator);
            }
            else
                Game.NetworkingErrors.Enqueue(string.Format("Server refused {0}:\n{1}", spectator.Name, mess.FailMessage)); // TODO: Proper line wrapping in dialogs
        }

        private void HandlePlayerDeletionMessage(PlayerDeletionMessage mess)
        {
            Game.DataEngine.Spectators.Remove(spec => spec.IsRemote && spec.ID == mess.PlayerID);
        }

        private void HandleGameSettingsRequest(GameSettingsRequest mess)
        {
            Game.SelectedArenaName = mess.ArenaToPlay;
        }

        private void HandleArenaFinishMessage(ArenaFinishMessage mess)
        {
            Game.FinishArena();
        }

        private void HandlePlayerControlsMessage(PlayerControlsMessage mess)
        {
            var player = Game.DataEngine.Players.FirstOrDefault(plr => plr.ID == mess.PlayerID);
            if (player == null || player.ConnectionID != mess.ConnectionID)
            {
                // A client sent controls for a nonexisting player or a player that
                // lives on another game instance. We silently ignore the controls.
                return;
            }
            Action<RemoteControl, ControlState> setRemoteControlState =
                (control, state) => control.SetControlState(state.Force, state.Pulse);
            foreach (PlayerControlType control in System.Enum.GetValues(typeof(PlayerControlType)))
                setRemoteControlState((RemoteControl)player.Controls[control], mess.GetControlState(control));
            var playerPlayer = player as Player;
            if (playerPlayer != null && playerPlayer.Ship != null && playerPlayer.Ship.LocationPredicter != null)
                playerPlayer.Ship.LocationPredicter.StoreControlStates(mess.ControlStates, Game.NetworkEngine.GetMessageGameTime(mess));
        }

        private void HandlePlayerMessageMessageOnServer(PlayerMessageMessage mess)
        {
            if (mess.AllPlayers)
            {
                var otherPlayers = Game.DataEngine.Players.Where(plr => plr.ConnectionID != mess.ConnectionID);
                foreach (var player in otherPlayers) player.Messages.Add(mess.Message);
            }
            else
            {
                var player = Game.DataEngine.Players.FirstOrDefault(plr => plr.ID == mess.PlayerID);
                if (player != null)
                {
                    if (player.IsRemote)
                        Game.NetworkEngine.GetGameClientConnection(player.ConnectionID).Send(mess);
                    else
                        HandlePlayerMessageMessageOnClient(mess);
                }
            }
        }

        private void HandlePlayerMessageMessageOnClient(PlayerMessageMessage mess)
        {
            if (mess.AllPlayers) throw new NotImplementedException("Client cannot broadcast player text messages");
            var player = Game.DataEngine.Players.First(plr => plr.ID == mess.PlayerID);
            if (player != null) player.Messages.Add(mess.Message);
        }

        private void HandleSpectatorUpdateMessage(SpectatorUpdateMessage mess)
        {
            var framesAgo = Game.NetworkEngine.GetMessageAge(mess);
            var player = Game.DataEngine.Spectators.FirstOrDefault(plr => plr.ID == mess.SpectatorID);
            if (player == null) return; // Silently ignoring update for an unknown player
            mess.Read(player, SerializationModeFlags.VaryingDataFromServer, framesAgo);
        }

        private void HandleGameServerHandshakeRequestTCP(GameServerHandshakeRequestTCP mess)
        {
            var net = Game.NetworkEngine;
            string clientDiff, serverDiff;
            int diffIndex;
            bool differ = MiscHelper.FirstDifference(mess.CanonicalStrings, CanonicalString.CanonicalForms, out clientDiff, out serverDiff, out diffIndex);
            var connection = net.GetGameClientConnection(mess.ConnectionID);
            if (differ)
            {
                var mismatchInfo = string.Format("First mismatch is index: {0}, client: {1}, server: {2}",
                    diffIndex, clientDiff ?? "<missing>", serverDiff ?? "<missing>");
                var extraInfo = diffIndex == 0 ? "" : string.Format(", client previous: {0}, server previous: {1}",
                    mess.CanonicalStrings[diffIndex - 1], CanonicalString.CanonicalForms[diffIndex - 1]);
                Log.Write("Client's CanonicalStrings don't match ours. " + mismatchInfo + extraInfo);
                var reply = new ConnectionClosingMessage { Info = "of version mismatch (canonical strings).\nPlease install the latest version from\nwww.assaultwing.com" };
                connection.Send(reply);
                net.DropClient(mess.ConnectionID);
            }
            else
            {
                connection.ConnectionStatus.ClientKey = mess.GameClientKey;
                connection.ConnectionStatus.State = ConnectionUtils.GameClientStatus.StateType.Active;
                // Send dummy UDP packets to probable UDP end points of the client to increase
                // probability of our NAT forwarding UDP packets from the client to us.
                var ping = new PingRequestMessage();
                for (int port = NetworkEngine.UDP_CONNECTION_PORT_FIRST; port <= NetworkEngine.UDP_CONNECTION_PORT_LAST; port++)
                    net.UDPSocket.Send(ping.Serialize, new IPEndPoint(net.GetConnection(mess.ConnectionID).RemoteTCPEndPoint.Address, port));
            }
        }

        private void HandleSpectatorSettingsRequestOnServer(SpectatorSettingsRequest mess)
        {
            var clientConn = Game.NetworkEngine.GetGameClientConnection(mess.ConnectionID);
            if (clientConn.ConnectionStatus.State == ConnectionUtils.GameClientStatus.StateType.Dropped) return;
            clientConn.ConnectionStatus.IsRequestingSpawn = mess.IsRequestingSpawn;
            clientConn.ConnectionStatus.IsReadyToStartArena = mess.IsGameClientReadyToStartArena;
            if (!mess.IsRegisteredToServer)
            {
                var newSpectator = TryCreateAndAddNewSpectator(mess, SerializationModeFlags.ConstantDataFromClient);
                var reply = new SpectatorSettingsReply
                {
                    SpectatorLocalID = mess.SpectatorID,
                    SpectatorID = newSpectator != null ? newSpectator.ID : Spectator.UNINITIALIZED_ID,
                    FailMessage = newSpectator != null ? "" : "Pilot already in game",
                };
                clientConn.Send(reply);
                Game.DataEngine.EnqueueArenaStatisticsToClients();
            }
            else
            {
                var spectator = Game.DataEngine.Spectators.FirstOrDefault(plr => plr.ID == mess.SpectatorID);
                if (spectator == null || spectator.ConnectionID != mess.ConnectionID)
                {
                    // Silently ignoring update of an unknown spectator or
                    // a spectator that doesn't live on the client who sent the update.
                }
                else
                {
                    // Be careful not to overwrite the player's color with something silly from the client.
                    // TODO !!! Implement this by Player.Serialize writing everything but the colour when mode is ConstantFromClient.
                    var oldColor = spectator is Player ? (Color?)((Player)spectator).Color : null;
                    mess.Read(spectator, SerializationModeFlags.ConstantDataFromClient, 0);
                    if (oldColor.HasValue) ((Player)spectator).Color = oldColor.Value;
                }
            }
        }

        private void HandleGobUpdateMessageOnClient(GobUpdateMessage mess, int framesAgo)
        {
            var arena = Game.DataEngine.Arena;
            foreach (var collisionEvent in mess.CollisionEvents)
            {
                var gob1 = arena.Gobs.FirstOrDefault(gob => gob.ID == collisionEvent.Gob1ID);
                var gob2 = arena.Gobs.FirstOrDefault(gob => gob.ID == collisionEvent.Gob2ID);
                if (gob1 == null || gob2 == null) continue;
                var area1 = gob1.GetCollisionArea(collisionEvent.Area1ID);
                var area2 = gob2.GetCollisionArea(collisionEvent.Area2ID);
                if (area1 != null && area2 != null)
                {
                    gob1.CollideIrreversible(area1, area2, collisionEvent.Stuck);
                    if (collisionEvent.CollideBothWays)
                        gob2.CollideIrreversible(area2, area1, collisionEvent.Stuck);
                }
                if ((collisionEvent.Sound & Arena.CollisionSoundTypes.WallCollision) != 0)
                    arena.Game.SoundEngine.PlaySound("Collision", gob1);
                if ((collisionEvent.Sound & Arena.CollisionSoundTypes.ShipCollision) != 0)
                    arena.Game.SoundEngine.PlaySound("Shipcollision", gob1);
            }
            mess.ReadGobs(gobId =>
            {
                var theGob = arena.Gobs.FirstOrDefault(gob => gob.ID == gobId);
                return theGob == null || theGob.IsDisposed ? null : theGob;
            }, framesAgo, SerializationModeFlags.VaryingDataFromServer);
        }

        private void HandleGobUpdateMessageOnServer(GobUpdateMessage mess, int framesAgo)
        {
            var arena = Game.DataEngine.Arena;
            var messOwner = Game.DataEngine.Spectators.SingleOrDefault(plr => plr.ConnectionID == mess.ConnectionID);
            if (messOwner == null) return;
            mess.ReadGobs(gobId =>
            {
                var theGob = arena.Gobs.FirstOrDefault(gob => gob.ID == gobId);
                return theGob == null || theGob.IsDisposed || theGob.Owner != messOwner ? null : theGob;
            }, framesAgo, SerializationModeFlags.VaryingDataFromClient);
        }

        private void HandleGobDeletionMessage(GobDeletionMessage mess, int framesAgo)
        {
            Game.LogicEngine.GobsToKillOnClient.AddRange(mess.GobIDs);
        }

        private void HandleArenaStatisticsMessage(ArenaStatisticsMessage mess)
        {
            mess.ReadSpectatorStatistics(specID =>
            {
                var spectator = Game.DataEngine.Spectators.FirstOrDefault(spec => spec.ID == specID);
                return spectator == null ? null : spectator.ArenaStatistics;
            });
        }

        #endregion

        private Player GetTempPlayer()
        {
            return new Player(Game, "dummy", CanonicalString.Null, CanonicalString.Null, CanonicalString.Null, new AW2.UI.PlayerControls());
        }

        private Spectator TryCreateAndAddNewSpectator(SpectatorSettingsRequest mess, SerializationModeFlags mode)
        {
            var ipAddress = Game.NetworkEngine.GetConnection(mess.ConnectionID).RemoteTCPEndPoint.Address;
            Spectator newSpectator = null;
            switch (mess.Subclass)
            {
                case SpectatorSettingsRequest.SubclassType.Player:
                    newSpectator = new Player(Game, "<uninitialised>", CanonicalString.Null, CanonicalString.Null, CanonicalString.Null, mess.ConnectionID, ipAddress);
                    break;
                case SpectatorSettingsRequest.SubclassType.BotPlayer:
                    newSpectator = new BotPlayer(Game, mess.ConnectionID, ipAddress);
                    break;
                default: throw new ApplicationException("Unexpected spectator subclass " + mess.Subclass);
            }
            mess.Read(newSpectator, mode, 0);
            if (newSpectator.GetStats().IsLoggedIn && Game.DataEngine.Spectators.Any(
                spec => spec.GetStats().IsLoggedIn && spec.GetStats().PilotId == newSpectator.GetStats().PilotId))
            {
                Log.Write("Refusing spectator {0} because he's already logged in", newSpectator.Name);
                return null;
            }
            var oldSpectator = Game.DataEngine.Spectators.FirstOrDefault(
                spec => spec.IsDisconnected && spec.IPAddress.Equals(ipAddress) && spec.Name == newSpectator.Name);
            if (oldSpectator == null)
            {
                Game.DataEngine.Spectators.Add(newSpectator);
            }
            else
            {
                // This can happen only on a game server.
                Log.Write("Reconnecting spectator {0}", oldSpectator.Name);
                oldSpectator.Reconnect(newSpectator);
                newSpectator = oldSpectator;
            }
            Game.Stats.Send(new { AddPlayer = newSpectator.GetStats().LoginToken, Name = newSpectator.Name });
            return newSpectator;
        }
    }
}
