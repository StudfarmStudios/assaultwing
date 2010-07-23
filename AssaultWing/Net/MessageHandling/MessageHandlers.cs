using System;
using System.Collections.Generic;
using System.Linq;
using AW2.Game;
using AW2.Helpers;
using AW2.Net.Messages;
using Microsoft.Xna.Framework.Graphics;

namespace AW2.Net.MessageHandling
{
    public static class MessageHandlers
    {
        public static void ActivateHandlers(IEnumerable<IMessageHandler> handlers)
        {
            AssaultWing.Instance.NetworkEngine.MessageHandlers.AddRange(handlers);
        }

        public static void DeactivateHandlers(IEnumerable<IMessageHandler> handlers)
        {
            var net = AssaultWing.Instance.NetworkEngine;
            var handlerTypesToRemove = handlers.Select(handler => handler.GetType());
            foreach (var handler in net.MessageHandlers)
                if (handlerTypesToRemove.Contains(handler.GetType())) handler.Dispose();
        }

        public static IEnumerable<IMessageHandler> GetClientMenuHandlers(Action joinGameReplyAction)
        {
            yield return new MessageHandler<ConnectionClosingMessage>(true, IMessageHandler.SourceType.Server, HandleConnectionClosingMessage);
            yield return new MessageHandler<StartGameMessage>(false, IMessageHandler.SourceType.Server, HandleStartGameMessage);
            yield return new MessageHandler<PlayerSettingsReply>(false, IMessageHandler.SourceType.Server, HandlePlayerSettingsReply);
            yield return new MessageHandler<PlayerSettingsRequest>(false, IMessageHandler.SourceType.Server, HandlePlayerSettingsRequestOnClient);
            yield return new MessageHandler<PlayerDeletionMessage>(false, IMessageHandler.SourceType.Server, HandlePlayerDeletionMessage);
            yield return new MessageHandler<GameSettingsRequest>(false, IMessageHandler.SourceType.Server, HandleGameSettingsRequest);
            yield return new MessageHandler<JoinGameReply>(true, IMessageHandler.SourceType.Server, mess => joinGameReplyAction());
        }

        public static IEnumerable<IMessageHandler> GetClientGameplayHandlers()
        {
            yield return new MessageHandler<ConnectionClosingMessage>(true, IMessageHandler.SourceType.Server, HandleConnectionClosingMessage);
            yield return new MessageHandler<WallHoleMessage>(false, IMessageHandler.SourceType.Server, HandleWallHoleMessage);
            yield return new GameplayMessageHandler<GobCreationMessage>(false, IMessageHandler.SourceType.Server, AssaultWing.Instance.DataEngine.ProcessGobCreationMessage);
            yield return new MessageHandler<ArenaStartRequest>(false, IMessageHandler.SourceType.Server, HandleArenaStartRequest);
            yield return new MessageHandler<ArenaFinishMessage>(false, IMessageHandler.SourceType.Server, HandleArenaFinishMessage);
            yield return new MessageHandler<PlayerMessageMessage>(false, IMessageHandler.SourceType.Server, HandlePlayerMessageMessage);
            yield return new MessageHandler<PlayerUpdateMessage>(false, IMessageHandler.SourceType.Server, HandlePlayerUpdateMessage);
            yield return new MessageHandler<GobDamageMessage>(false, IMessageHandler.SourceType.Server, HandleGobDamageMessage);
            yield return new MessageHandler<PlayerDeletionMessage>(false, IMessageHandler.SourceType.Server, HandlePlayerDeletionMessage);
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
            yield return new MessageHandler<ArenaStartReply>(false, IMessageHandler.SourceType.Client, mess => idRegisterer(mess.ConnectionID));
        }

        public static void IncomingConnectionHandlerOnServer(Result<AW2.Net.Connections.Connection> result)
        {
            if (!result.Successful)
                Log.Write("Some client failed to connect: " + result.Error);
            else
            {
                Log.Write("Server obtained connection from " + result.Value.RemoteTCPEndPoint);
                if (AssaultWing.Instance.GameState == AW2.Core.GameState.Gameplay ||
                    AssaultWing.Instance.GameState == AW2.Core.GameState.OverlayDialog)
                {
                    var mess = new ConnectionClosingMessage { Info = "Game is already running, you're late!" };
                    result.Value.Send(mess);
                    result.Value.Dispose();
                }
            }
        }

        #region Handler implementations

        private static void HandleConnectionClosingMessage(ConnectionClosingMessage mess)
        {
            Log.Write("Server is going to close the connection, reason: " + mess.Info);
            var dialogData = new AW2.Graphics.OverlayComponents.CustomOverlayDialogData("Server closed connection.\n" + mess.Info,
                new AW2.UI.TriggeredCallback(AW2.UI.TriggeredCallback.GetProceedControl(), AssaultWing.Instance.ShowMenu));
            AssaultWing.Instance.ShowDialog(dialogData);
        }

        private static void HandleStartGameMessage(StartGameMessage mess)
        {
            AssaultWing.Instance.DataEngine.ArenaPlaylist = new AW2.Helpers.Collections.Playlist(mess.ArenaPlaylist);
            MessageHandlers.DeactivateHandlers(MessageHandlers.GetClientMenuHandlers(null));

            // Prepare and start playing the game.
            var menuEngine = AssaultWing.Instance.MenuEngine;
            menuEngine.ProgressBarAction(AssaultWing.Instance.PrepareFirstArena,
                () => MessageHandlers.ActivateHandlers(MessageHandlers.GetClientGameplayHandlers()));
            menuEngine.Deactivate();
        }

        private static void HandlePlayerSettingsRequestOnClient(PlayerSettingsRequest mess)
        {
            var spectator = AssaultWing.Instance.DataEngine.Spectators.FirstOrDefault(
                spec => spec.ID == mess.PlayerID && spec.ServerRegistration != Spectator.ServerRegistrationType.No);
            if (spectator == null)
            {
                var newPlayer = CreateAndAddNewPlayer(mess);
                newPlayer.ID = mess.PlayerID;
                newPlayer.ServerRegistration = Spectator.ServerRegistrationType.Yes;
            }
            else if (spectator.IsRemote)
            {
                mess.Read(spectator, SerializationModeFlags.ConstantData, TimeSpan.Zero);
            }
            else
            {
                // Be careful not to overwrite our most recent name and equipment choices
                // with something older from the server.
                var tempPlayer = GetTempPlayer();
                mess.Read(tempPlayer, SerializationModeFlags.ConstantData, TimeSpan.Zero);
                if (spectator is Player) ((Player)spectator).PlayerColor = tempPlayer.PlayerColor;
            }
        }

        private static void HandlePlayerSettingsReply(PlayerSettingsReply mess)
        {
            var player = AssaultWing.Instance.DataEngine.Spectators.FirstOrDefault(plr => ClientPlayerCriteria(plr, mess.OldPlayerID));
            if (player == null) throw new ApplicationException("Cannot find unregistered local player with ID " + mess.OldPlayerID);
            player.ServerRegistration = Spectator.ServerRegistrationType.Yes;
            player.ID = mess.NewPlayerID;
        }

        private static void HandlePlayerDeletionMessage(PlayerDeletionMessage mess)
        {
            AssaultWing.Instance.DataEngine.Spectators.Remove(spec => spec.ID == mess.PlayerID);
        }

        private static void HandleGameSettingsRequest(GameSettingsRequest mess)
        {
            AssaultWing.Instance.DataEngine.ArenaPlaylist = new AW2.Helpers.Collections.Playlist(mess.ArenaPlaylist);
        }

        private static void HandleWallHoleMessage(WallHoleMessage mess)
        {
            var wall = (AW2.Game.Gobs.Wall)AssaultWing.Instance.DataEngine.Arena.Gobs.FirstOrDefault(gob => gob.ID == mess.GobID);
            if (wall == null)
                Log.Write("WARNING: Cannot find wall ID " + mess.GobID + " for WallHoleMessage");
            else
                wall.MakeHole(mess.TriangleIndices);
        }

        private static void HandleArenaStartRequest(ArenaStartRequest mess)
        {
            AssaultWing.Instance.NetworkEngine.GameServerConnection.Send(new ArenaStartReply());
            AssaultWing.Instance.StartArena();
        }

        private static void HandleArenaFinishMessage(ArenaFinishMessage mess)
        {
            AssaultWing.Instance.FinishArena();
        }

        private static void HandlePlayerMessageMessage(PlayerMessageMessage mess)
        {
            var player = AssaultWing.Instance.DataEngine.Spectators.First(spec => spec.ID == mess.PlayerID) as Player;
            if (player == null) throw new NetworkException("Text message for spectator " + mess.PlayerID + " who is not a Player");
            player.SendMessage(mess.Text, mess.Color);
        }

        private static void HandlePlayerUpdateMessage(PlayerUpdateMessage mess)
        {
            var messageAge = AssaultWing.Instance.NetworkEngine.GetMessageAge(mess);
            var player = AssaultWing.Instance.DataEngine.Spectators.FirstOrDefault(plr => plr.ID == mess.PlayerID);
            if (player == null) throw new NetworkException("Update for unknown player ID " + mess.PlayerID);
            mess.Read(player, SerializationModeFlags.VaryingData, messageAge);
        }

        private static void HandleGobDamageMessage(GobDamageMessage mess)
        {
            Gob gob = AssaultWing.Instance.DataEngine.Arena.Gobs.FirstOrDefault(gobb => gobb.ID == mess.GobID);
            if (gob == null) return; // Skip updates for gobs we haven't yet created.
            gob.DamageLevel = mess.DamageLevel;
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
                AssaultWing.Instance.NetworkEngine.GetGameClientConnection(mess.ConnectionID).Send(reply);
                AssaultWing.Instance.NetworkEngine.DropClient(mess.ConnectionID, false);
            }
            else
            {
                var reply = new JoinGameReply();
                AssaultWing.Instance.NetworkEngine.GetGameClientConnection(mess.ConnectionID).Send(reply);
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
                AssaultWing.Instance.NetworkEngine.GetGameClientConnection(mess.ConnectionID).Send(reply);
            }
            else
            {
                var player = AssaultWing.Instance.DataEngine.Spectators.FirstOrDefault(plr => plr.ID == mess.PlayerID);
                if (player == null) throw new NetworkException("Settings update for unknown player ID " + mess.PlayerID);
                if (player.ConnectionID != mess.ConnectionID)
                {
                    // Silently ignoring update of a player that doesn't live on the client who sent the update.
                }
                else
                {
                    // Be careful not to overwrite the player's color with something silly from the client.
                    var oldColor = player is Player ? (Color?)((Player)player).PlayerColor : null;
                    mess.Read(player, SerializationModeFlags.ConstantData, TimeSpan.Zero);
                    if (oldColor.HasValue) ((Player)player).PlayerColor = oldColor.Value;
                }
            }
        }

        #endregion

        private static Player GetTempPlayer()
        {
            return new Player("dummy", CanonicalString.Null, CanonicalString.Null, CanonicalString.Null, new AW2.UI.PlayerControls());
        }

        private static bool ClientPlayerCriteria(Spectator spectator, int oldPlayerID)
        {
            return spectator.ServerRegistration == Spectator.ServerRegistrationType.Requested &&
                spectator.ID == oldPlayerID;
        }

        private static Player CreateAndAddNewPlayer(PlayerSettingsRequest mess)
        {
            var newPlayer = new Player("<uninitialised>", CanonicalString.Null, CanonicalString.Null, CanonicalString.Null, mess.ConnectionID);
            mess.Read(newPlayer, SerializationModeFlags.ConstantData, TimeSpan.Zero);
            AssaultWing.Instance.DataEngine.Spectators.Add(newPlayer);
            return newPlayer;
        }
    }
}
