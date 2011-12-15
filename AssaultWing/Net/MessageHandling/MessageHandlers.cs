using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.Xna.Framework;
using AW2.Core;
using AW2.Core.OverlayComponents;
using AW2.Game;
using AW2.Helpers;
using AW2.Helpers.Serialization;
using AW2.Net.ManagementMessages;
using AW2.Net.Messages;
using AW2.UI;
using AW2.Net.Connections;

namespace AW2.Net.MessageHandling
{
    public class MessageHandlers
    {
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
            yield return new MessageHandler<PlayerSettingsReply>(MessageHandlerBase.SourceType.Server, HandlePlayerSettingsReply);
            yield return new MessageHandler<SpectatorSettingsRequest>(MessageHandlerBase.SourceType.Server, HandleSpectatorSettingsRequestOnClient);
            yield return new MessageHandler<PlayerDeletionMessage>(MessageHandlerBase.SourceType.Server, HandlePlayerDeletionMessage);
            yield return new MessageHandler<GameSettingsRequest>(MessageHandlerBase.SourceType.Server, HandleGameSettingsRequest);
            yield return new MessageHandler<PlayerMessageMessage>(MessageHandlerBase.SourceType.Server, HandlePlayerMessageMessageOnClient);
            yield return new MessageHandler<ArenaFinishMessage>(MessageHandlerBase.SourceType.Server, HandleArenaFinishMessage);
        }

        public IEnumerable<MessageHandlerBase> GetClientGameplayHandlers()
        {
            var networkEngine = Game.NetworkEngine;
            yield return new MessageHandler<PlayerUpdateMessage>(MessageHandlerBase.SourceType.Server, HandlePlayerUpdateMessage);
            yield return new MessageHandler<PlayerDeletionMessage>(MessageHandlerBase.SourceType.Server, HandlePlayerDeletionMessage);
            yield return new GameplayMessageHandler<GobCreationMessage>(MessageHandlerBase.SourceType.Server, networkEngine, Game.HandleGobCreationMessage) { OneMessageAtATime = true };
            yield return new GameplayMessageHandler<GobUpdateMessage>(MessageHandlerBase.SourceType.Server, networkEngine, HandleGobUpdateMessageOnClient);
            yield return new GameplayMessageHandler<GobDeletionMessage>(MessageHandlerBase.SourceType.Server, networkEngine, HandleGobDeletionMessage);
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
                Game.StartClient(mess.GameServerEndPoints, ConnectionResultOnClientCallback);
            }
            else
            {
                Log.Write("Couldn't connect to server: " + mess.FailMessage);
                Game.ShowInfoDialog("Couldn't connect to server:\n" + mess.FailMessage); // TODO: Proper line wrapping in dialogs
            }
        }

        private void HandlePingMessage(PingMessage mess)
        {
            Game.NetworkEngine.ManagementServerConnection.Send(new PongMessage());
            Game.NetworkEngine.ManagementServerConnection.OnPingReceived();
        }

        private void HandleSpectatorSettingsRequestOnClient(SpectatorSettingsRequest mess)
        {
            var spectator = Game.DataEngine.Spectators.FirstOrDefault(
                spec => spec.ID == mess.SpectatorID && spec.ServerRegistration != Spectator.ServerRegistrationType.No);
            if (spectator == null)
            {
                var newPlayer = CreateAndAddNewSpectator(mess, SerializationModeFlags.ConstantDataFromServer);
                newPlayer.ID = mess.SpectatorID;
                newPlayer.ServerRegistration = Spectator.ServerRegistrationType.Yes;
            }
            else if (spectator.IsRemote)
            {
                mess.Read(spectator, SerializationModeFlags.ConstantDataFromServer, 0);
            }
            else
            {
                if (mess.Subclass != SpectatorSettingsRequest.SubclassType.Player) throw new ApplicationException("Unexpected Spectator subclass " + mess.Subclass);
                // Be careful not to overwrite our most recent name and equipment choices
                // with something older from the server.
                var tempPlayer = GetTempPlayer();
                mess.Read(tempPlayer, SerializationModeFlags.ConstantDataFromServer, 0);
                if (spectator is Player) ((Player)spectator).Color = tempPlayer.Color;
            }
        }

        private void HandleConnectionClosingMessage(ConnectionClosingMessage mess)
        {
            Log.Write("Server is going to close the connection because {0}.", mess.Info);
            var dialogData = new CustomOverlayDialogData(Game, "Server closed connection because\n" + mess.Info + ".",
                new TriggeredCallback(TriggeredCallback.PROCEED_CONTROL, Game.ShowMainMenuAndResetGameplay));
            Game.ShowDialog(dialogData);
        }

        private void HandleStartGameMessage(StartGameMessage mess)
        {
            if (Game.IsLoadingArena) return;
            Game.ShowEquipMenu();
            Game.SelectedArenaName = mess.ArenaToPlay;
            Game.MenuEngine.ProgressBar.Start(mess.WallCount);
            Game.MenuEngine.ProgressBarAction(
                () => Game.PrepareSelectedArena(mess.ArenaID),
                () =>
                {
                    // The network connection may have been cut during arena loading.
                    if (Game.NetworkMode != NetworkMode.Client) return;
                    ActivateHandlers(GetClientGameplayHandlers());
                    Game.IsClientAllowedToStartArena = true;
                    Game.StartArenaButStayInMenu();
                });
        }

        private void HandlePlayerSettingsReply(PlayerSettingsReply mess)
        {
            var player = Game.DataEngine.Spectators.FirstOrDefault(plr => plr.LocalID == mess.PlayerLocalID);
            if (player == null) throw new ApplicationException("Cannot find unregistered local player with local ID " + mess.PlayerLocalID);
            player.ServerRegistration = Spectator.ServerRegistrationType.Yes;
            player.ID = mess.PlayerID;
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

        private void HandlePlayerUpdateMessage(PlayerUpdateMessage mess)
        {
            var framesAgo = Game.NetworkEngine.GetMessageAge(mess);
            var player = Game.DataEngine.Spectators.FirstOrDefault(plr => plr.ID == mess.PlayerID);
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
            if (clientConn.ConnectionStatus.IsDropped) return;
            clientConn.ConnectionStatus.IsPlayingArena = mess.IsGameClientPlayingArena;
            clientConn.ConnectionStatus.IsReadyToStartArena = mess.IsGameClientReadyToStartArena;
            if (!mess.IsRegisteredToServer)
            {
                var newSpectator = CreateAndAddNewSpectator(mess, SerializationModeFlags.ConstantDataFromClient);
                var reply = new PlayerSettingsReply
                {
                    PlayerLocalID = mess.SpectatorID,
                    PlayerID = newSpectator.ID
                };
                clientConn.Send(reply);
            }
            else
            {
                var player = Game.DataEngine.Spectators.FirstOrDefault(plr => plr.ID == mess.SpectatorID);
                if (player == null) throw new NetworkException("Settings update for unknown spectator ID " + mess.SpectatorID);
                if (player.ConnectionID != mess.ConnectionID)
                {
                    // Silently ignoring update of a player that doesn't live on the client who sent the update.
                }
                else
                {
                    // Be careful not to overwrite the player's color with something silly from the client.
                    var oldColor = player is Player ? (Color?)((Player)player).Color : null;
                    mess.Read(player, SerializationModeFlags.ConstantDataFromClient, 0);
                    if (oldColor.HasValue) ((Player)player).Color = oldColor.Value;
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
                    gob1.Collide(area1, area2, collisionEvent.Stuck, Arena.CollisionSideEffectType.Irreversible);
                    if (collisionEvent.CollideBothWays)
                        gob2.Collide(area2, area1, collisionEvent.Stuck, Arena.CollisionSideEffectType.Irreversible);
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
            Game.LogicEngine.GobsToKillOnClient.Add(mess.GobID);
        }

        #endregion

        private Player GetTempPlayer()
        {
            return new Player(Game, "dummy", CanonicalString.Null, CanonicalString.Null, CanonicalString.Null, new AW2.UI.PlayerControls());
        }

        private Spectator CreateAndAddNewSpectator(SpectatorSettingsRequest mess, SerializationModeFlags mode)
        {
            Spectator newSpectator = null;
            switch (mess.Subclass)
            {
                case SpectatorSettingsRequest.SubclassType.Player:
                    newSpectator = new Player(Game, "<uninitialised>", CanonicalString.Null, CanonicalString.Null, CanonicalString.Null, mess.ConnectionID);
                    break;
                case SpectatorSettingsRequest.SubclassType.BotPlayer:
                    newSpectator = new BotPlayer(Game, mess.ConnectionID);
                    break;
                default: throw new ApplicationException("Unexpected spectator subclass " + mess.Subclass);
            }
            mess.Read(newSpectator, mode, 0);
            Game.DataEngine.Spectators.Add(newSpectator);
            Game.Stats.Send(new { AddPlayer = newSpectator.LoginToken });
            return newSpectator;
        }

        private void ConnectionResultOnClientCallback(Result<Connection> result)
        {
            var net = Game.NetworkEngine;
            if (net.GameServerConnection != null)
            {
                // Silently ignore extra server connection attempts.
                if (result.Successful) result.Value.Dispose();
                return;
            }

            if (!result.Successful)
            {
                Log.Write("Failed to connect to server: " + result.Error);
                Game.StopClient("Failed to connect to server.");
            }
            else
            {
                DeactivateHandlers(GetStandaloneMenuHandlers(null));
                net.GameServerConnection = result.Value;
                ActivateHandlers(GetClientMenuHandlers());
                var joinRequest = new GameServerHandshakeRequestTCP
                {
                    CanonicalStrings = CanonicalString.CanonicalForms,
                    GameClientKey = net.GetAssaultWingInstanceKey(),
                };
                net.GameServerConnection.Send(joinRequest);
                Game.MenuEngine.Activate(AW2.Menu.MenuComponentType.Equip);
            }
        }
    }
}
