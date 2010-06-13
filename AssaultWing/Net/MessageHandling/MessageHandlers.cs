using System;
using System.Collections.Generic;
using System.Linq;
using AW2.Game;
using AW2.Helpers;
using AW2.Net.Messages;

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

        public static IEnumerable<IMessageHandler> GetClientMenuHandlers(IConnection gameServerConnection)
        {
            yield return new MessageHandler<StartGameMessage>(false, IMessageHandler.SourceType.Server, HandleStartGameMessage);
        }

        public static IEnumerable<IMessageHandler> GetClientGameplayHandlers(PingedConnection gameServerConnection)
        {
            yield return new MessageHandler<WallHoleMessage>(false, IMessageHandler.SourceType.Server, HandleWallHoleMessage);
            yield return new GameplayMessageHandler<GobCreationMessage>(false, IMessageHandler.SourceType.Server, AssaultWing.Instance.DataEngine.ProcessGobCreationMessage);
            yield return new MessageHandler<ArenaStartRequest>(false, IMessageHandler.SourceType.Server, HandleArenaStartRequest);
            yield return new MessageHandler<ArenaFinishMessage>(false, IMessageHandler.SourceType.Server, HandleArenaFinishMessage);
            yield return new MessageHandler<PlayerMessageMessage>(false, IMessageHandler.SourceType.Server, HandlePlayerMessageMessage);
            yield return new MessageHandler<PlayerUpdateMessage>(false, IMessageHandler.SourceType.Server, HandlePlayerUpdateMessage);
            yield return new MessageHandler<GobDamageMessage>(false, IMessageHandler.SourceType.Server, HandleGobDamageMessage);
        }

        public static IEnumerable<IMessageHandler> GetServerMenuHandlers(IConnection clientConnections)
        {
            yield return new MessageHandler<JoinGameRequest>(false, IMessageHandler.SourceType.Client, HandleJoinGameRequest);
        }

        public static IEnumerable<IMessageHandler> GetServerGameplayHandlers(IConnection clientConnections)
        {
            yield return new MessageHandler<PlayerControlsMessage>(false, IMessageHandler.SourceType.Client, AW2.UI.UIEngineImpl.HandlePlayerControlsMessage);
        }

        public static IEnumerable<IMessageHandler> GetServerArenaStartHandlers(IConnection clientConnections, Action<int> idRegisterer)
        {
            yield return new MessageHandler<ArenaStartReply>(false, IMessageHandler.SourceType.Client, mess => idRegisterer(mess.ConnectionId));
        }

        #region Handler implementations

        private static void HandleStartGameMessage(StartGameMessage mess)
        {
            mess.DeserializePlayers(playerID =>
            {
                var player = (Player)AssaultWing.Instance.DataEngine.Spectators.FirstOrDefault(p => p.Id == playerID);
                if (player == null)
                {
                    player = new Player("uninitialised", CanonicalString.Null, CanonicalString.Null, CanonicalString.Null, 0x7ea1eaf);
                    AssaultWing.Instance.DataEngine.Spectators.Add(player);
                }
                return player;
            });
            AssaultWing.Instance.DataEngine.ArenaPlaylist = new AW2.Helpers.Collections.Playlist(mess.ArenaPlaylist);
            MessageHandlers.DeactivateHandlers(MessageHandlers.GetClientMenuHandlers(null));

            // Prepare and start playing the game.
            var menuEngine = AssaultWing.Instance.MenuEngine;
            menuEngine.ProgressBarAction(AssaultWing.Instance.PrepareFirstArena,
                () => MessageHandlers.ActivateHandlers(MessageHandlers.GetClientGameplayHandlers((PingedConnection)AssaultWing.Instance.NetworkEngine.GameServerConnection)));
            menuEngine.Deactivate();
        }

        private static void HandleWallHoleMessage(WallHoleMessage mess)
        {
            var wall = (AW2.Game.Gobs.Wall)AssaultWing.Instance.DataEngine.Arena.Gobs.FirstOrDefault(gob => gob.Id == mess.GobId);
            if (wall == null)
                Log.Write("WARNING: Cannot find wall ID " + mess.GobId + " for WallHoleMessage");
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
            var player = AssaultWing.Instance.DataEngine.Spectators.First(spec => spec.Id == mess.PlayerId) as Player;
            if (player == null) throw new NetworkException("Text message for spectator " + mess.PlayerId + " who is not a Player");
            player.SendMessage(mess.Text, mess.Color);
        }

        private static void HandlePlayerUpdateMessage(PlayerUpdateMessage mess)
        {
            var messageAge = AssaultWing.Instance.NetworkEngine.GetMessageAge(mess);
            var player = AssaultWing.Instance.DataEngine.Spectators.FirstOrDefault(plr => plr.Id == mess.PlayerId);
            if (player == null) throw new NetworkException("Update for unknown player ID " + mess.PlayerId);
            mess.Read(player, SerializationModeFlags.VaryingData, messageAge);
        }

        private static void HandleGobDamageMessage(GobDamageMessage mess)
        {
            Gob gob = AssaultWing.Instance.DataEngine.Arena.Gobs.FirstOrDefault(gobb => gobb.Id == mess.GobId);
            if (gob == null) return; // Skip updates for gobs we haven't yet created.
            gob.DamageLevel = mess.DamageLevel;
        }

        private static void HandleJoinGameRequest(JoinGameRequest mess)
        {
            // Send player ID changes for new players, if any. A join game request
            // may also update the chosen equipment of a previously added player.
            var reply = new JoinGameReply();
            var playerIdChanges = new List<JoinGameReply.IdChange>();
            foreach (var info in mess.PlayerInfos)
            {
                var oldPlayer = AssaultWing.Instance.DataEngine.Players.FirstOrDefault(
                    plr => plr.ConnectionId == mess.ConnectionId && plr.Id == info.id);
                if (oldPlayer != null)
                {
                    oldPlayer.Name = info.name;
                    oldPlayer.ShipName = info.shipTypeName;
                    oldPlayer.Weapon2Name = info.weapon2TypeName;
                    oldPlayer.ExtraDeviceName = info.extraDeviceTypeName;
                }
                else
                {
                    Player player = new Player(info.name, info.shipTypeName, info.weapon2TypeName, info.extraDeviceTypeName, mess.ConnectionId);
                    AssaultWing.Instance.DataEngine.Spectators.Add(player);
                    playerIdChanges.Add(new JoinGameReply.IdChange { oldId = info.id, newId = player.Id });
                }
            }
            if (playerIdChanges.Count > 0)
            {
                reply.CanonicalStrings = AW2.Helpers.CanonicalString.CanonicalForms;
                reply.PlayerIdChanges = playerIdChanges.ToArray();
                AssaultWing.Instance.NetworkEngine.GameClientConnections[mess.ConnectionId].Send(reply);
            }
        }

        #endregion
    }
}
