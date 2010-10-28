using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.Xna.Framework.Graphics;
using AW2.Game;
using AW2.Helpers;
using AW2.Helpers.Serialization;
using AW2.Net.ManagementMessages;
using AW2.Net.Messages;

namespace AW2.Net.MessageHandling
{
    public static class MessageHandlers
    {
        public static void ActivateHandlers(IEnumerable<IMessageHandler> handlers)
        {
            AssaultWingCore.Instance.NetworkEngine.MessageHandlers.AddRange(handlers);
        }

        public static void DeactivateHandlers(IEnumerable<IMessageHandler> handlers)
        {
            var net = AssaultWingCore.Instance.NetworkEngine;
            var handlerTypesToRemove = handlers.Select(handler => handler.GetType());
            foreach (var handler in net.MessageHandlers)
                if (handlerTypesToRemove.Contains(handler.GetType())) handler.Dispose();
        }

        public static IEnumerable<IMessageHandler> GetStandaloneMenuHandlers(Action<GameServerListReply> handleGameServerListReply, Action<JoinGameServerReply> handleJoinGameServerReply)
        {
            yield return new MessageHandler<GameServerListReply>(false, IMessageHandler.SourceType.Management, handleGameServerListReply);
            yield return new MessageHandler<JoinGameServerReply>(false, IMessageHandler.SourceType.Management, handleJoinGameServerReply);

            // ClientJoinMessage is only for game servers
            yield return new MessageHandler<ClientJoinMessage>(false, IMessageHandler.SourceType.Management, HandleClientJoinMessage);
        }

        public static IEnumerable<IMessageHandler> GetClientMenuHandlers(Action joinGameReplyAction, Action<StartGameMessage> handleStartGameMessage, Action<ConnectionClosingMessage> handleConnectionClosingMessage)
        {
            yield return new MessageHandler<ConnectionClosingMessage>(true, IMessageHandler.SourceType.Server, handleConnectionClosingMessage);
            yield return new MessageHandler<StartGameMessage>(false, IMessageHandler.SourceType.Server, handleStartGameMessage);
            yield return new MessageHandler<PlayerSettingsReply>(false, IMessageHandler.SourceType.Server, HandlePlayerSettingsReply);
            yield return new MessageHandler<PlayerSettingsRequest>(false, IMessageHandler.SourceType.Server, HandlePlayerSettingsRequestOnClient);
            yield return new MessageHandler<PlayerDeletionMessage>(false, IMessageHandler.SourceType.Server, HandlePlayerDeletionMessage);
            yield return new MessageHandler<GameSettingsRequest>(false, IMessageHandler.SourceType.Server, HandleGameSettingsRequest);
            yield return new MessageHandler<JoinGameReply>(true, IMessageHandler.SourceType.Server, mess => joinGameReplyAction());
        }

        public static IEnumerable<IMessageHandler> GetClientGameplayHandlers(Action<ConnectionClosingMessage> handleConnectionClosingMessage, GameplayMessageHandler<GobCreationMessageBase>.GameplayMessageAction handleGobCreationMessage)
        {
            yield return new MessageHandler<ArenaStartRequest>(false, IMessageHandler.SourceType.Server, m => HandleArenaStartRequest(m, handleGobCreationMessage));
            yield return new MessageHandler<ArenaFinishMessage>(false, IMessageHandler.SourceType.Server, HandleArenaFinishMessage);
            yield return new MessageHandler<PlayerMessageMessage>(false, IMessageHandler.SourceType.Server, HandlePlayerMessageMessage);
            yield return new MessageHandler<PlayerUpdateMessage>(false, IMessageHandler.SourceType.Server, HandlePlayerUpdateMessage);
            yield return new MessageHandler<PlayerDeletionMessage>(false, IMessageHandler.SourceType.Server, HandlePlayerDeletionMessage);
            yield return new GameplayMessageHandler<GobPreCreationMessage>(false, IMessageHandler.SourceType.Server, (m, f) => handleGobCreationMessage((GobPreCreationMessage)m, f));
        }

        public static IEnumerable<IMessageHandler> GetClientArenaActionHandlers(GameplayMessageHandler<GobCreationMessageBase>.GameplayMessageAction handleGobCreationMessage)
        {
            yield return new MessageHandler<WallHoleMessage>(false, IMessageHandler.SourceType.Server, HandleWallHoleMessage);
            yield return new GameplayMessageHandler<GobCreationMessage>(false, IMessageHandler.SourceType.Server, (m, f) => handleGobCreationMessage((GobCreationMessage)m, f));
        }

        public static IEnumerable<IMessageHandler> GetServerMenuHandlers()
        {
            yield return new MessageHandler<JoinGameRequest>(false, IMessageHandler.SourceType.Client, HandleJoinGameRequest);
            yield return new MessageHandler<PlayerSettingsRequest>(false, IMessageHandler.SourceType.Client, HandlePlayerSettingsRequestOnServer);
        }

        public static IEnumerable<IMessageHandler> GetServerGameplayHandlers()
        {
            yield return new MessageHandler<PlayerControlsMessage>(false, IMessageHandler.SourceType.Client, AW2.UI.UIEngineImpl.HandlePlayerControlsMessage);
        }

        public static IEnumerable<IMessageHandler> GetServerArenaStartHandlers(Action<int> idRegisterer)
        {
            yield return new MessageHandler<ArenaLoadedMessage>(false, IMessageHandler.SourceType.Client, mess => idRegisterer(mess.ConnectionID));
        }

        public static void IncomingConnectionHandlerOnServer(Result<AW2.Net.Connections.Connection> result, Func<bool> allowNewConnection)
        {
            if (!result.Successful)
                Log.Write("Some client failed to connect: " + result.Error);
            else
            {
                Log.Write("Server obtained connection from " + result.Value.RemoteTCPEndPoint);
                if (!allowNewConnection())
                {
                    var mess = new ConnectionClosingMessage { Info = "Game server doesn't allow joining right now" };
                    result.Value.Send(mess);
                    AssaultWingCore.Instance.NetworkEngine.DropClient(result.Value.ID, false);
                }
            }
        }

        #region Handler implementations

        private static void HandleClientJoinMessage(ClientJoinMessage mess)
        {
            IPEndPoint matchingEndPoint = null;
            var connection = AssaultWingCore.Instance.NetworkEngine.GameClientConnections.FirstOrDefault(conn =>
            {
                matchingEndPoint = mess.ClientUDPEndPoints.FirstOrDefault(endPoint => endPoint.Address.Equals(conn.RemoteIPAddress));
                return matchingEndPoint != null;
            });
            if (connection == null)
            {
                // Received game client UDP end point before the connection to the game client was created.
                // TODO: The connection is probably going to be created soon. Store the port somewhere else.
                // OR, more likely the connection was closed immediately (e.g. because the game server is full).
            }
            else
            {
                connection.RemoteUDPEndPoint = matchingEndPoint;
            }
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

        private static void HandlePlayerSettingsReply(PlayerSettingsReply mess)
        {
            var player = AssaultWingCore.Instance.DataEngine.Spectators.FirstOrDefault(plr => ClientPlayerCriteria(plr, mess.OldPlayerID));
            if (player == null) throw new ApplicationException("Cannot find unregistered local player with ID " + mess.OldPlayerID);
            player.ServerRegistration = Spectator.ServerRegistrationType.Yes;
            player.ID = mess.NewPlayerID;
        }

        private static void HandlePlayerDeletionMessage(PlayerDeletionMessage mess)
        {
            AssaultWingCore.Instance.DataEngine.Spectators.Remove(spec => spec.ID == mess.PlayerID);
        }

        private static void HandleGameSettingsRequest(GameSettingsRequest mess)
        {
            AssaultWingCore.Instance.DataEngine.ArenaPlaylist = new AW2.Helpers.Collections.Playlist(mess.ArenaPlaylist);
        }

        private static void HandleWallHoleMessage(WallHoleMessage mess)
        {
            var wall = (AW2.Game.Gobs.Wall)AssaultWingCore.Instance.DataEngine.Arena.Gobs.FirstOrDefault(gob => gob.ID == mess.GobID);
            if (wall == null)
                Log.Write("WARNING: Cannot find wall ID " + mess.GobID + " for WallHoleMessage");
            else
                wall.MakeHole(mess.TriangleIndices);
        }

        private static void HandleArenaStartRequest(ArenaStartRequest mess, GameplayMessageHandler<GobCreationMessageBase>.GameplayMessageAction handleGobCreationMessage)
        {
            MessageHandlers.ActivateHandlers(MessageHandlers.GetClientArenaActionHandlers(handleGobCreationMessage));
            AssaultWingCore.Instance.StartArena(mess.StartDelay);
        }

        private static void HandleArenaFinishMessage(ArenaFinishMessage mess)
        {
            AssaultWingCore.Instance.FinishArena();
        }

        private static void HandlePlayerMessageMessage(PlayerMessageMessage mess)
        {
            var player = AssaultWingCore.Instance.DataEngine.Spectators.First(spec => spec.ID == mess.PlayerID) as Player;
            if (player == null) throw new NetworkException("Text message for spectator " + mess.PlayerID + " who is not a Player");
            player.SendMessage(mess.Text, mess.Color);
        }

        private static void HandlePlayerUpdateMessage(PlayerUpdateMessage mess)
        {
            var framesAgo = AssaultWingCore.Instance.NetworkEngine.GetMessageAge(mess);
            var player = AssaultWingCore.Instance.DataEngine.Spectators.FirstOrDefault(plr => plr.ID == mess.PlayerID);
            if (player == null) throw new NetworkException("Update for unknown player ID " + mess.PlayerID);
            mess.Read(player, SerializationModeFlags.VaryingData, framesAgo);
        }

        private static void HandleJoinGameRequest(JoinGameRequest mess)
        {
            string clientDiff, serverDiff;
            bool differ = MiscHelper.FirstDifference(mess.CanonicalStrings, CanonicalString.CanonicalForms, out clientDiff, out serverDiff);
            if (differ)
            {
                string mismatchInfo = string.Format("First mismatch is client: {0}, server: {1}",
                    clientDiff ?? "<missing>", serverDiff ?? "<missing>");
                Log.Write("Client's CanonicalStrings don't match ours. " + mismatchInfo);
                var reply = new ConnectionClosingMessage
                {
                    Info = "Cannot join server due to mismatching canonical strings!\n" + mismatchInfo
                };
                AssaultWingCore.Instance.NetworkEngine.GetGameClientConnection(mess.ConnectionID).Send(reply);
                AssaultWingCore.Instance.NetworkEngine.DropClient(mess.ConnectionID, false);
            }
            else
            {
                var reply = new JoinGameReply();
                AssaultWingCore.Instance.NetworkEngine.GetGameClientConnection(mess.ConnectionID).Send(reply);
            }
        }

        private static void HandlePlayerSettingsRequestOnServer(PlayerSettingsRequest mess)
        {
            if (!mess.IsRegisteredToServer)
            {
                var newPlayer = CreateAndAddNewPlayer(mess);
                var reply = new PlayerSettingsReply
                {
                    OldPlayerID = mess.PlayerID,
                    NewPlayerID = newPlayer.ID
                };
                AssaultWingCore.Instance.NetworkEngine.GetGameClientConnection(mess.ConnectionID).Send(reply);
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

        #endregion

        private static Player GetTempPlayer()
        {
            return new Player(null, "dummy", CanonicalString.Null, CanonicalString.Null, CanonicalString.Null, new AW2.UI.PlayerControls());
        }

        private static bool ClientPlayerCriteria(Spectator spectator, int oldPlayerID)
        {
            return spectator.ServerRegistration == Spectator.ServerRegistrationType.Requested &&
                spectator.ID == oldPlayerID;
        }

        private static Player CreateAndAddNewPlayer(PlayerSettingsRequest mess)
        {
            var newPlayer = new Player(null, "<uninitialised>", CanonicalString.Null, CanonicalString.Null, CanonicalString.Null, mess.ConnectionID);
            mess.Read(newPlayer, SerializationModeFlags.ConstantData, 0);
            AssaultWingCore.Instance.DataEngine.Spectators.Add(newPlayer);
            return newPlayer;
        }
    }
}
