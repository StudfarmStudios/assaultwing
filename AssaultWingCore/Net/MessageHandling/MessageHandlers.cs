using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.Xna.Framework;
using AW2.Core;
using AW2.Game;
using AW2.Game.Logic;
using AW2.Game.Players;
using AW2.Helpers;
using AW2.Helpers.Serialization;
using AW2.Net.Connections;
using AW2.Net.Messages;
using AW2.UI;

namespace AW2.Net.MessageHandling
{
    public class MessageHandlers
    {
        public event Action<string> GameServerConnectionClosing; // parameter is info

        private AssaultWingCore Game { get; set; }

        public MessageHandlers(AssaultWingCore game)
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

        // TODO: Peter: Steam network, do we need something like the GetStandaloneMenuHandlers that was here?

        public IEnumerable<MessageHandlerBase> GetClientMenuHandlers()
        {
            yield return new MessageHandler<ConnectionClosingMessage>(MessageHandlerBase.SourceType.Server, HandleConnectionClosingMessage);
            yield return new MessageHandler<StartGameMessage>(MessageHandlerBase.SourceType.Server, HandleStartGameMessage);
            yield return new MessageHandler<SpectatorSettingsReply>(MessageHandlerBase.SourceType.Server, HandleSpectatorSettingsReply);
            yield return new MessageHandler<SpectatorSettingsRequest>(MessageHandlerBase.SourceType.Server, HandleSpectatorSettingsRequestOnClient);
            yield return new MessageHandler<TeamSettingsMessage>(MessageHandlerBase.SourceType.Server, HandleTeamSettingsMessageOnClient);
            yield return new MessageHandler<SpectatorOrTeamDeletionMessage>(MessageHandlerBase.SourceType.Server, HandleSpectatorOrTeamDeletionMessage);
            yield return new MessageHandler<GameSettingsRequest>(MessageHandlerBase.SourceType.Server, HandleGameSettingsRequest);
            yield return new MessageHandler<PlayerMessageMessage>(MessageHandlerBase.SourceType.Server, HandlePlayerMessageMessageOnClient);
            yield return new MessageHandler<ArenaFinishMessage>(MessageHandlerBase.SourceType.Server, HandleArenaFinishMessage);
            yield return new MessageHandler<PilotRankingMessage>(MessageHandlerBase.SourceType.Server, HandlePilotRankingMessageOnClient);
        }

        public IEnumerable<MessageHandlerBase> GetClientGameplayHandlers()
        {
            var networkEngine = Game.NetworkEngine;
            yield return new MessageHandler<SpectatorOrTeamUpdateMessage>(MessageHandlerBase.SourceType.Server, HandleSpectatorOrTeamUpdateMessage);
            yield return new MessageHandler<SpectatorOrTeamDeletionMessage>(MessageHandlerBase.SourceType.Server, HandleSpectatorOrTeamDeletionMessage);
            yield return new GameplayMessageHandler<GobCreationMessage>(MessageHandlerBase.SourceType.Server, networkEngine, Game.GobCreationMessageReceived) { OneMessageAtATime = true };
            yield return new GameplayMessageHandler<GobUpdateMessage>(MessageHandlerBase.SourceType.Server, networkEngine, HandleGobUpdateMessageOnClient);
            yield return new GameplayMessageHandler<GobDeletionMessage>(MessageHandlerBase.SourceType.Server, networkEngine, HandleGobDeletionMessage);
        }

        public IEnumerable<MessageHandlerBase> GetServerMenuHandlers()
        {
            yield return new MessageHandler<GameServerHandshakeRequestTCP>(MessageHandlerBase.SourceType.Client, HandleGameServerHandshakeRequestTCP);
            yield return new MessageHandler<SpectatorSettingsRequest>(MessageHandlerBase.SourceType.Client, HandleSpectatorSettingsRequestOnServer);
            yield return new MessageHandler<PlayerMessageMessage>(MessageHandlerBase.SourceType.Client, HandlePlayerMessageMessageOnServer);
            yield return new MessageHandler<PilotRankingMessage>(MessageHandlerBase.SourceType.Client, HandlePilotRankingMessageOnServer);
        }

        public IEnumerable<MessageHandlerBase> GetServerGameplayHandlers()
        {
            var networkEngine = Game.NetworkEngine;
            yield return new GameplayMessageHandler<ClientGameStateUpdateMessage>(MessageHandlerBase.SourceType.Client, networkEngine, HandleClientGameStateUpdateMessage);
        }

        #region Handler implementations

        // TODO: Peter: Steam network, connecting to selected server

        private void HandleSpectatorSettingsRequestOnClient(SpectatorSettingsRequest mess)
        {
            var spectator = Game.DataEngine.Spectators.FirstOrDefault(
                spec => spec.ID == mess.SpectatorID && spec.ServerRegistration != Spectator.ServerRegistrationType.No);
            bool isLocal = spectator?.IsLocal ?? false;

            // If the spectator is local, we don't want to overwrite locally owned data like his ship selection,
            // but we still wan't to deserialize the ranking data from the server. The KeepLocalClientOwnedData
            // flag protects the locally owned data.
            var spectatorSerializationMode = SerializationModeFlags.ConstantDataFromServer |
                (isLocal ? SerializationModeFlags.KeepLocalClientOwnedData : 0);

            if (spectator == null)
                TryCreateAndAddNewSpectatorOnClient(mess, spectatorSerializationMode);
            else
                mess.Read(spectator, spectatorSerializationMode, 0);
        }

        private void HandleTeamSettingsMessageOnClient(TeamSettingsMessage mess)
        {
            mess.Read(id =>
                {
                    var team = Game.DataEngine.FindTeam(id);
                    if (team == null) Game.DataEngine.Teams.Add(team = new Team("<uninitialised>", Game.DataEngine.FindSpectator) { ID = id });
                    return team;
                },
                SerializationModeFlags.ConstantDataFromServer | SerializationModeFlags.VaryingDataFromServer, 0);
            // Remove teams that were not mentioned in the message.
            if (mess.IDs.Count() != Game.DataEngine.Teams.Count)
                Game.DataEngine.Teams.Remove(team => !mess.IDs.Contains(team.ID));
        }

        private void HandleConnectionClosingMessage(ConnectionClosingMessage mess)
        {
            GameServerConnectionClosing(mess.Info);
        }

        private void HandleStartGameMessage(StartGameMessage mess)
        {
            if (Game.DataEngine.Arena != null && Game.DataEngine.Arena.ID == mess.ArenaID) return;
            Game.DataEngine.ArenaFinishTime = mess.ArenaTimeLeft == TimeSpan.Zero ? TimeSpan.Zero : mess.ArenaTimeLeft + Game.GameTime.TotalRealTime;
            Game.PrepareArenaOnClient(mess.GameplayMode, mess.ArenaToPlay, mess.ArenaID, mess.WallCount);
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
                var oldSpectator = Game.DataEngine.Spectators.FirstOrDefault(spec => spec.ID == spectator.ID && spec != spectator);
                if (oldSpectator != null)
                {
                    spectator.ReconnectOnClient(oldSpectator);
                    Game.DataEngine.Spectators.Remove(oldSpectator);
                }
            }
            else
                Game.NetworkingErrors.Enqueue(string.Format("Server refused {0}:\n{1}", spectator.Name, mess.FailMessage)); // TODO: Proper line wrapping in dialogs
        }

        private void HandleSpectatorOrTeamDeletionMessage(SpectatorOrTeamDeletionMessage mess)
        {
            Game.DataEngine.Spectators.Remove(spec => !spec.IsLocal && spec.ID == mess.SpectatorOrTeamID);
            Game.DataEngine.Teams.Remove(team => team.ID == mess.SpectatorOrTeamID);
        }

        private void HandleGameSettingsRequest(GameSettingsRequest mess)
        {
            Game.DataEngine.GameplayMode = (GameplayMode)Game.DataEngine.GetTypeTemplate(mess.GameplayMode);
            Game.SelectedArenaName = mess.ArenaToPlay;
        }

        private void HandleArenaFinishMessage(ArenaFinishMessage mess)
        {
            Game.FinishArena();
        }

        private void HandleClientGameStateUpdateMessage(ClientGameStateUpdateMessage mess, int framesAgo)
        {
            HandleGobUpdateMessageOnServer(mess, framesAgo);
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

        private void HandlePilotRankingMessageOnServer(PilotRankingMessage mess)
        {
            var player = Game.DataEngine.Players.FirstOrDefault(plr => plr.ID == mess.PlayerID);
            if (player == null || player.ConnectionID != mess.ConnectionID)
            {
                // A client sent ranking for a nonexisting player or some other player.
                // Players are only expected to send updates for them selves.
                return;
            }
            var merged = player.Ranking.Merge(mess.PilotRanking); // Do a merge algorithm because both clients and the server may update it.
            if (!merged.UpToDate)
                Log.Write($"Player {player.Name} PilotRanking: {player.Ranking} -> {merged}");
            player.Ranking = merged; // Needs to be done like this because PilotRanking is a struct.
        }

        private void HandlePlayerMessageMessageOnClient(PlayerMessageMessage mess)
        {
            if (mess.AllPlayers) throw new NotImplementedException("Client cannot broadcast player text messages");
            var player = Game.DataEngine.Players.First(plr => plr.ID == mess.PlayerID);
            if (player != null) player.Messages.Add(mess.Message);
        }

        private void HandlePilotRankingMessageOnClient(PilotRankingMessage mess)
        {
            var player = Game.DataEngine.Players.First(plr => plr.ID == mess.PlayerID);
            if (player == null) return;
            var merged = player.Ranking.Merge(mess.PilotRanking); // Do a merge algorithm because both clients and the server may update it.
            if (!merged.UpToDate)
                Log.Write($"Player {player.Name} PilotRanking: {player.Ranking} -> {merged}");
            player.Ranking = merged; // Needs to be done like this because PilotRanking is a struct.
        }

        private void HandleSpectatorOrTeamUpdateMessage(SpectatorOrTeamUpdateMessage mess)
        {
            mess.Read(id => (INetworkSerializable)Game.DataEngine.FindSpectator(id) ?? Game.DataEngine.FindTeam(id),
                SerializationModeFlags.VaryingDataFromServer, 0);
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
                net.DropClient(connection);
            }
            else
            {
                connection.ConnectionStatus.ClientKey = mess.GameClientKey;
                connection.ConnectionStatus.State = ConnectionUtils.GameClientStatus.StateType.Active;
                net.DoClientUdpHandshake(mess);
            }
        }

        private void HandleSpectatorSettingsRequestOnServer(SpectatorSettingsRequest mess)
        {
            var clientConn = Game.NetworkEngine.GetGameClientConnection(mess.ConnectionID);
            if (clientConn.ConnectionStatus.State == ConnectionUtils.GameClientStatus.StateType.Dropped) return;
            clientConn.ConnectionStatus.IsRequestingSpawnForArenaID = mess.IsRequestingSpawnForArenaID;
            clientConn.ConnectionStatus.IsReadyToStartArena = mess.IsGameClientReadyToStartArena;
            var serializationMode = SerializationModeFlags.ConstantDataFromClient;
            if (!mess.IsRegisteredToServer)
                TryCreateAndAddNewSpectatorOnServer(mess, serializationMode);
            else
            {
                var spectator = Game.DataEngine.FindSpectator(mess.SpectatorID);
                if (spectator == null || spectator.ConnectionID != mess.ConnectionID)
                {
                    // Silently ignoring update of an unknown spectator or
                    // a spectator that doesn't live on the client who sent the update.
                }
                else
                    mess.Read(spectator, serializationMode, 0);
            }
        }

        private void HandleGobUpdateMessageOnClient(GobUpdateMessage mess, int framesAgo)
        {
            var arena = Game.DataEngine.Arena;
            var updatedGobs = new Dictionary<int, Arena.GobUpdateData>();
            var serializationMode = SerializationModeFlags.VaryingDataFromServer;
            mess.ReadGobs(gobID =>
            {
                var theGob = arena.Gobs[gobID];
                var result = theGob == null || theGob.IsDisposed ? null : theGob;
                if (result != null) updatedGobs.Add(result.ID, new Arena.GobUpdateData(result, framesAgo));
                return result;
            }, serializationMode, framesAgo);
            foreach (var collisionEvent in mess.ReadCollisionEvents(id => arena.Gobs[id], serializationMode, framesAgo))
            {
                collisionEvent.SkipReversibleSideEffects = true;
                collisionEvent.Handle();
            }
            arena.FinalizeGobUpdatesOnClient(updatedGobs, framesAgo);
        }

        private void HandleGobUpdateMessageOnServer(GobUpdateMessage mess, int framesAgo)
        {
            var arena = Game.DataEngine.Arena;
            var messOwner = Game.DataEngine.Spectators.SingleOrDefault(plr => plr.ConnectionID == mess.ConnectionID);
            if (messOwner == null) return;
            mess.ReadGobs(gobID =>
            {
                var theGob = arena.Gobs[gobID];
                return theGob == null || theGob.IsDisposed || theGob.Owner != messOwner ? null : theGob;
            }, SerializationModeFlags.VaryingDataFromClient, framesAgo);
            // Note: Game server intentionally doesn't call mess.ReadCollisionEvents.
        }

        private void HandleGobDeletionMessage(GobDeletionMessage mess, int framesAgo)
        {
            Game.LogicEngine.KillGobsOnClient(mess.GobIDs);
        }

        #endregion

        private void TryCreateAndAddNewSpectatorOnServer(SpectatorSettingsRequest mess, SerializationModeFlags mode)
        {
            var newSpectator = GetSpectator(mess, mode);
            newSpectator.LocalID = mess.SpectatorID;
            Game.DataEngine.AddPendingRemoteSpectatorOnServer(newSpectator);
        }

        private void TryCreateAndAddNewSpectatorOnClient(SpectatorSettingsRequest mess, SerializationModeFlags mode)
        {
            var newSpectator = GetSpectator(mess, mode);
            Game.AddRemoteSpectator(newSpectator);
            newSpectator.ID = mess.SpectatorID;
            newSpectator.ServerRegistration = Spectator.ServerRegistrationType.Yes;
        }

        private Spectator GetSpectator(SpectatorSettingsRequest mess, SerializationModeFlags mode)
        {
            Spectator newSpectator;
            switch (mess.Subclass)
            {
                case SpectatorSettingsRequest.SubclassType.Player:
                    newSpectator = new Player(Game,
                        pilotId: "<uninitialised pilotId>",
                        name: "<uninitialised>",
                        shipTypeName: CanonicalString.Null,
                        weapon2Name: CanonicalString.Null,
                        extraDeviceName: CanonicalString.Null,
                        connectionId: mess.ConnectionID);
                    break;
                case SpectatorSettingsRequest.SubclassType.BotPlayer:
                    newSpectator = new BotPlayer(Game, pilotId: "<uninitialised bot pilotId>", connectionID: mess.ConnectionID);
                    break;
                default: throw new ApplicationException("Unexpected spectator subclass " + mess.Subclass);
            }
            mess.Read(newSpectator, mode, 0);
            return newSpectator;
        }
    }
}
