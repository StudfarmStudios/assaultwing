using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Core;
using AW2.Core.OverlayDialogs;
using AW2.Game;
using AW2.Helpers;
using AW2.Helpers.Serialization;
using AW2.Net.ManagementMessages;
using AW2.Net.Messages;
using AW2.UI;
using AW2.Net.Connections;

namespace AW2.Net.MessageHandling
{
    public static class MessageHandlers
    {
        public static void ActivateHandlers(IEnumerable<MessageHandlerBase> handlers)
        {
            AssaultWing.Instance.NetworkEngine.MessageHandlers.AddRange(handlers);
        }

        public static void DeactivateHandlers(IEnumerable<MessageHandlerBase> handlers)
        {
            var net = AssaultWing.Instance.NetworkEngine;
            var handlerTypesToRemove = handlers.Select(handler => handler.GetType());
            foreach (var handler in net.MessageHandlers)
                if (handlerTypesToRemove.Contains(handler.GetType())) handler.Dispose();
        }

        public static IEnumerable<MessageHandlerBase> GetStandaloneMenuHandlers(Action<GameServerListReply> handleGameServerListReply)
        {
            yield return new MessageHandler<GameServerListReply>(MessageHandlerBase.SourceType.Management, handleGameServerListReply);
            yield return new MessageHandler<JoinGameServerReply>(MessageHandlerBase.SourceType.Management, HandleJoinGameServerReply);

            // These messages only game servers receive
            yield return new MessageHandler<PingMessage>(MessageHandlerBase.SourceType.Management, HandlePingMessage);
        }

        public static IEnumerable<MessageHandlerBase> GetClientMenuHandlers()
        {
            yield return new MessageHandler<ConnectionClosingMessage>(MessageHandlerBase.SourceType.Server, HandleConnectionClosingMessage);
            yield return new MessageHandler<StartGameMessage>(MessageHandlerBase.SourceType.Server, HandleStartGameMessage);
            yield return new MessageHandler<PlayerSettingsReply>(MessageHandlerBase.SourceType.Server, HandlePlayerSettingsReply);
            yield return new MessageHandler<PlayerSettingsRequest>(MessageHandlerBase.SourceType.Server, HandlePlayerSettingsRequestOnClient);
            yield return new MessageHandler<PlayerDeletionMessage>(MessageHandlerBase.SourceType.Server, HandlePlayerDeletionMessage);
            yield return new MessageHandler<GameSettingsRequest>(MessageHandlerBase.SourceType.Server, HandleGameSettingsRequest);
            yield return new MessageHandler<PlayerMessageMessage>(MessageHandlerBase.SourceType.Server, HandlePlayerMessageMessageOnClient);
        }

        public static IEnumerable<MessageHandlerBase> GetClientGameplayHandlers(GameplayMessageHandler<GobCreationMessage>.GameplayMessageAction handleGobCreationMessage)
        {
            var networkEngine = AssaultWing.Instance.NetworkEngine;
            yield return new MessageHandler<ArenaFinishMessage>(MessageHandlerBase.SourceType.Server, HandleArenaFinishMessage);
            yield return new MessageHandler<PlayerUpdateMessage>(MessageHandlerBase.SourceType.Server, HandlePlayerUpdateMessage);
            yield return new MessageHandler<PlayerDeletionMessage>(MessageHandlerBase.SourceType.Server, HandlePlayerDeletionMessage);
            yield return new GameplayMessageHandler<GobCreationMessage>(MessageHandlerBase.SourceType.Server, networkEngine, handleGobCreationMessage);
            yield return new GameplayMessageHandler<GobUpdateMessage>(MessageHandlerBase.SourceType.Server, networkEngine, HandleGobUpdateMessage);
            yield return new GameplayMessageHandler<GobDeletionMessage>(MessageHandlerBase.SourceType.Server, networkEngine, HandleGobDeletionMessage);
        }

        public static IEnumerable<MessageHandlerBase> GetServerMenuHandlers()
        {
            yield return new MessageHandler<GameServerHandshakeRequestTCP>(MessageHandlerBase.SourceType.Client, HandleGameServerHandshakeRequestTCP);
            yield return new MessageHandler<PlayerSettingsRequest>(MessageHandlerBase.SourceType.Client, HandlePlayerSettingsRequestOnServer);
            yield return new MessageHandler<PlayerMessageMessage>(MessageHandlerBase.SourceType.Client, HandlePlayerMessageMessageOnServer);
        }

        public static IEnumerable<MessageHandlerBase> GetServerGameplayHandlers()
        {
            yield return new MessageHandler<PlayerControlsMessage>(MessageHandlerBase.SourceType.Client, HandlePlayerControlsMessage);
        }

        public static void IncomingConnectionHandlerOnServer(Result<AW2.Net.Connections.Connection> result, Func<bool> allowNewConnection)
        {
            if (!result.Successful)
                Log.Write("Some client failed to connect: " + result.Error);
            else if (allowNewConnection())
            {
                AssaultWing.Instance.NetworkEngine.GameClientConnections.Add((GameClientConnection)result.Value);
                Log.Write("Server obtained connection from " + result.Value.RemoteTCPEndPoint);
            }
            else
            {
                var mess = new ConnectionClosingMessage { Info = "game server refused joining" };
                result.Value.Send(mess);
                Log.Write("Server refused connection from " + result.Value.RemoteTCPEndPoint);
            }
        }

        #region Handler implementations

        private static void HandleJoinGameServerReply(JoinGameServerReply mess)
        {
            var game = AssaultWing.Instance;
            if (mess.Success)
            {
                DeactivateHandlers(GetStandaloneMenuHandlers(null));
                game.SoundEngine.PlaySound("MenuChangeItem");
                game.StartClient(mess.GameServerEndPoints, ConnectionResultOnClientCallback);
            }
            else
            {
                Log.Write("Couldn't connect to server: " + mess.FailMessage);
                var dialogData = new CustomOverlayDialogData(game, "Couldn't connect to server:\n" + mess.FailMessage,
                    new TriggeredCallback(TriggeredCallback.PROCEED_CONTROL, game.ShowMainMenuAndResetGameplay));
                game.ShowDialog(dialogData);
            }
        }

        private static void HandlePingMessage(PingMessage mess)
        {
            var pong = new PongMessage();
            AssaultWing.Instance.NetworkEngine.ManagementServerConnection.Send(pong);
        }

        private static void HandlePlayerSettingsRequestOnClient(PlayerSettingsRequest mess)
        {
            var spectator = AssaultWingCore.Instance.DataEngine.Spectators.FirstOrDefault(
                spec => spec.ID == mess.PlayerID && spec.ServerRegistration != Spectator.ServerRegistrationType.No);
            if (spectator == null)
            {
                var newPlayer = CreateAndAddNewPlayer(mess);
                newPlayer.ID = mess.PlayerID;
                newPlayer.ServerRegistration = Spectator.ServerRegistrationType.Yes;
            }
            else if (spectator.IsRemote)
            {
                mess.Read(spectator, SerializationModeFlags.ConstantData, 0);
            }
            else
            {
                // Be careful not to overwrite our most recent name and equipment choices
                // with something older from the server.
                var tempPlayer = GetTempPlayer();
                mess.Read(tempPlayer, SerializationModeFlags.ConstantData, 0);
                if (spectator is Player) ((Player)spectator).PlayerColor = tempPlayer.PlayerColor;
            }
        }

        private static void HandleConnectionClosingMessage(ConnectionClosingMessage mess)
        {
            var game = AssaultWing.Instance;
            Log.Write("Server is going to close the connection because {0}.", mess.Info);
            var dialogData = new CustomOverlayDialogData(game, "Server closed connection because\n" + mess.Info + ".",
                new TriggeredCallback(TriggeredCallback.PROCEED_CONTROL, game.ShowMainMenuAndResetGameplay));
            game.ShowDialog(dialogData);
        }

        private static void HandleStartGameMessage(StartGameMessage mess)
        {
            var game = AssaultWing.Instance;
            if (game.IsLoadingArena) return;
            game.SelectedArenaName = mess.ArenaToPlay;
            game.MenuEngine.ProgressBarAction(
                () => game.PrepareArena(game.SelectedArenaName),
                () =>
                {
                    // The network connection may have been cut during arena loading.
                    if (game.NetworkMode != NetworkMode.Client) return;
                    ActivateHandlers(GetClientGameplayHandlers(game.HandleGobCreationMessage));
                    game.IsClientAllowedToStartArena = true;
                    game.StartArenaButStayInMenu();
                });
        }

        private static void HandlePlayerSettingsReply(PlayerSettingsReply mess)
        {
            var player = AssaultWingCore.Instance.DataEngine.Spectators.FirstOrDefault(plr => plr.LocalID == mess.PlayerLocalID);
            if (player == null) throw new ApplicationException("Cannot find unregistered local player with local ID " + mess.PlayerLocalID);
            player.ServerRegistration = Spectator.ServerRegistrationType.Yes;
            player.ID = mess.PlayerID;
        }

        private static void HandlePlayerDeletionMessage(PlayerDeletionMessage mess)
        {
            AssaultWingCore.Instance.DataEngine.Spectators.Remove(spec => spec.IsRemote && spec.ID == mess.PlayerID);
        }

        private static void HandleGameSettingsRequest(GameSettingsRequest mess)
        {
            AssaultWing.Instance.SelectedArenaName = mess.ArenaToPlay;
        }

        private static void HandleArenaFinishMessage(ArenaFinishMessage mess)
        {
            AssaultWingCore.Instance.FinishArena();
        }

        private static void HandlePlayerControlsMessage(PlayerControlsMessage mess)
        {
            var player = AssaultWingCore.Instance.DataEngine.Spectators.First(plr => plr.ID == mess.PlayerID);
            if (player.ConnectionID != mess.ConnectionID)
            {
                // A client sent controls for a player that lives on another game instance.
                // We silently ignore the controls.
                return;
            }
            Action<RemoteControl, ControlState> setRemoteControlState =
                (control, state) => control.SetControlState(state.Force, state.Pulse);
            foreach (PlayerControlType control in System.Enum.GetValues(typeof(PlayerControlType)))
                setRemoteControlState((RemoteControl)player.Controls[control], mess.GetControlState(control));
            var playerPlayer = player as Player;
            if (playerPlayer != null && playerPlayer.Ship != null)
                playerPlayer.Ship.LocationPredicter.StoreControlStates(mess.ControlStates, AssaultWing.Instance.NetworkEngine.GetMessageGameTime(mess));
        }

        private static void HandlePlayerMessageMessageOnServer(PlayerMessageMessage mess)
        {
            if (mess.AllPlayers)
            {
                var otherPlayers = AssaultWingCore.Instance.DataEngine.Players
                    .Where(plr => plr.ConnectionID != mess.ConnectionID);
                foreach (var player in otherPlayers) player.Messages.Add(mess.Message);
            }
            else
            {
                var player = AssaultWingCore.Instance.DataEngine.Players.FirstOrDefault(plr => plr.ID == mess.PlayerID);
                if (player != null)
                {
                    if (player.IsRemote)
                        AssaultWing.Instance.NetworkEngine.GetGameClientConnection(player.ConnectionID).Send(mess);
                    else
                        HandlePlayerMessageMessageOnClient(mess);
                }
            }
        }

        private static void HandlePlayerMessageMessageOnClient(PlayerMessageMessage mess)
        {
            if (mess.AllPlayers) throw new NotImplementedException("Client cannot broadcast player text messages");
            var player = AssaultWingCore.Instance.DataEngine.Players.First(plr => plr.ID == mess.PlayerID);
            if (player != null) player.Messages.Add(mess.Message);
        }

        private static void HandlePlayerUpdateMessage(PlayerUpdateMessage mess)
        {
            var framesAgo = AssaultWing.Instance.NetworkEngine.GetMessageAge(mess);
            var player = AssaultWingCore.Instance.DataEngine.Spectators.FirstOrDefault(plr => plr.ID == mess.PlayerID);
            if (player == null) return; // Silently ignoring update for an unknown player
            mess.Read(player, SerializationModeFlags.VaryingData, framesAgo);
        }

        private static void HandleGameServerHandshakeRequestTCP(GameServerHandshakeRequestTCP mess)
        {
            string clientDiff, serverDiff;
            int diffIndex;
            bool differ = MiscHelper.FirstDifference(mess.CanonicalStrings, CanonicalString.CanonicalForms, out clientDiff, out serverDiff, out diffIndex);
            var connection = AssaultWing.Instance.NetworkEngine.GetGameClientConnection(mess.ConnectionID);
            if (differ)
            {
                var mismatchInfo = string.Format("First mismatch is index: {0}, client: {1}, server: {2}",
                    diffIndex, clientDiff ?? "<missing>", serverDiff ?? "<missing>");
                var extraInfo = diffIndex == 0 ? "" : string.Format(", client previous: {0}, server previous: {1}",
                    mess.CanonicalStrings[diffIndex - 1], CanonicalString.CanonicalForms[diffIndex - 1]);
                Log.Write("Client's CanonicalStrings don't match ours. " + mismatchInfo + extraInfo);
                var reply = new ConnectionClosingMessage { Info = "of version mismatch (canonical strings)." };
                connection.Send(reply);
                AssaultWing.Instance.NetworkEngine.DropClient(mess.ConnectionID, false);
            }
            else
            {
                connection.ConnectionStatus.ClientKey = mess.GameClientKey;
            }
        }

        private static void HandlePlayerSettingsRequestOnServer(PlayerSettingsRequest mess)
        {
            var clientConn = AssaultWing.Instance.NetworkEngine.GetGameClientConnection(mess.ConnectionID);
            if (clientConn.ConnectionStatus.IsDropped) return;
            clientConn.ConnectionStatus.IsPlayingArena = mess.IsGameClientPlayingArena;
            if (!mess.IsRegisteredToServer)
            {
                var newPlayer = CreateAndAddNewPlayer(mess);
                var reply = new PlayerSettingsReply
                {
                    PlayerLocalID = mess.PlayerID,
                    PlayerID = newPlayer.ID
                };
                clientConn.Send(reply);
            }
            else
            {
                var player = AssaultWingCore.Instance.DataEngine.Spectators.FirstOrDefault(plr => plr.ID == mess.PlayerID);
                if (player == null) throw new NetworkException("Settings update for unknown player ID " + mess.PlayerID);
                if (player.ConnectionID != mess.ConnectionID)
                {
                    // Silently ignoring update of a player that doesn't live on the client who sent the update.
                }
                else
                {
                    // Be careful not to overwrite the player's color with something silly from the client.
                    var oldColor = player is Player ? (Color?)((Player)player).PlayerColor : null;
                    mess.Read(player, SerializationModeFlags.ConstantData, 0);
                    if (oldColor.HasValue) ((Player)player).PlayerColor = oldColor.Value;
                }
            }
        }

        private static void HandleGobUpdateMessage(GobUpdateMessage mess, int framesAgo)
        {
            var arena = AssaultWingCore.Instance.DataEngine.Arena;
            mess.ReadGobs(gobId =>
            {
                var theGob = arena.Gobs.FirstOrDefault(gob => gob.ID == gobId);
                return theGob == null || theGob.IsDisposed ? null : theGob;
            }, SerializationModeFlags.VaryingData, framesAgo);
        }

        private static void HandleGobDeletionMessage(GobDeletionMessage mess, int framesAgo)
        {
            AssaultWingCore.Instance.LogicEngine.GobsToKillOnClient.Add(mess.GobId);
        }

        #endregion

        private static Player GetTempPlayer()
        {
            return new Player(AssaultWing.Instance, "dummy", CanonicalString.Null, CanonicalString.Null, CanonicalString.Null, new AW2.UI.PlayerControls());
        }

        private static Player CreateAndAddNewPlayer(PlayerSettingsRequest mess)
        {
            var newPlayer = new Player(AssaultWing.Instance, "<uninitialised>", CanonicalString.Null, CanonicalString.Null, CanonicalString.Null, mess.ConnectionID);
            mess.Read(newPlayer, SerializationModeFlags.ConstantData, 0);
            AssaultWingCore.Instance.DataEngine.Spectators.Add(newPlayer);
            return newPlayer;
        }

        private static void ConnectionResultOnClientCallback(Result<Connection> result)
        {
            var net = AssaultWing.Instance.NetworkEngine;
            if (net.GameServerConnection != null)
            {
                // Silently ignore extra server connection attempts.
                if (result.Successful) result.Value.Dispose();
                return;
            }

            if (!result.Successful)
            {
                Log.Write("Failed to connect to server: " + result.Error);
                AssaultWing.Instance.StopClient("Failed to connect to server");
            }
            else
            {
                net.GameServerConnection = result.Value;
                ActivateHandlers(GetClientMenuHandlers());

                // HACK: Force one local player.
                AssaultWing.Instance.DataEngine.Spectators.Remove(player => AssaultWing.Instance.DataEngine.Spectators.Count > 1);

                var joinRequest = new GameServerHandshakeRequestTCP
                {
                    CanonicalStrings = CanonicalString.CanonicalForms,
                    GameClientKey = net.GetAssaultWingInstanceKey(),
                };
                net.GameServerConnection.Send(joinRequest);
                AssaultWing.Instance.MenuEngine.ActivateComponent(AW2.Menu.MenuComponentType.Equip);
            }
        }
    }
}
