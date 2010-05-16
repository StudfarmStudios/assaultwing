using System;
using System.Collections.Generic;
using System.Linq;
using AW2.Game;
using AW2.Helpers;
using AW2.Net.Messages;

namespace AW2.Net
{
    public static class MessageHandlers
    {
        public static IEnumerable<IMessageHandler> GetGameplayHandlers(PingedConnection gameServerConnection)
        {
            yield return new MessageHandler<WallHoleMessage>(false, gameServerConnection, HandleWallHoleMessage);
            yield return new GameplayMessageHandler<GobCreationMessage>(false, gameServerConnection, AssaultWing.Instance.DataEngine.ProcessGobCreationMessage);
            yield return new MessageHandler<ArenaStartRequest>(false, gameServerConnection, HandleArenaStartRequest);
            yield return new MessageHandler<PlayerMessageMessage>(false, gameServerConnection, HandlePlayerMessageMessage);
        }

        // TODO !!! someone must call this when gameplay ends
        public static void DeactivateGameplayHandlers()
        {
            var net = AssaultWing.Instance.NetworkEngine;
            var handlerTypesToRemove = GetGameplayHandlers(null).Select(handler => handler.GetType());
            foreach (var handler in net.MessageHandlers)
                if (handlerTypesToRemove.Contains(handler.GetType())) handler.Dispose();
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

        private static void HandlePlayerMessageMessage(PlayerMessageMessage mess)
        {
            var player = AssaultWing.Instance.DataEngine.Spectators.First(spec => spec.Id == mess.PlayerId) as Player;
            if (player == null) throw new ApplicationException("Text message for spectator " + mess.PlayerId + " who is not a Player");
            player.SendMessage(mess.Text);
        }
    }
}