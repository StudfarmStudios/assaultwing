using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Input;
using AW2.Core.GameComponents;
using AW2.Core.OverlayComponents;
using AW2.Game;
using AW2.Helpers;
using AW2.Helpers.Serialization;
using AW2.Menu;
using AW2.Net;
using AW2.Net.Connections;
using AW2.Net.ManagementMessages;
using AW2.Net.MessageHandling;
using AW2.Net.Messages;
using AW2.UI;
using AW2.Graphics;

namespace AW2.Core
{
    [System.Diagnostics.DebuggerDisplay("AssaultWing {Logic}")]
    public class AssaultWing : AssaultWingCore
    {
        private TimeSpan _lastGameSettingsSent;
        private TimeSpan _lastFrameNumberSynchronization;
        private byte _nextArenaID;
        private GobDeletionMessage _pendingGobDeletionMessage;
        private byte[] _debugBuffer = new byte[65536]; // DEBUG: catch a rare crash that seems to happen only when serializing walls.

        // Debug keys, used only #if DEBUG
        private Control _frameStepControl;
        private Control _frameRunControl;
        private bool _frameStep;

        /// <summary>
        /// The AssaultWing instance. Avoid using this remnant of the old times.
        /// </summary>
        public static new AssaultWing Instance { get { return (AssaultWing)AssaultWingCore.Instance; } }
        public bool IsClientAllowedToStartArena { get; set; }
        public Control ChatStartControl { get; set; }

        public string SelectedArenaName { get; set; }
        private ProgramLogic Logic { get; set; }
        public UIEngineImpl UIEngine { get { return (UIEngineImpl)Components.First(c => c is UIEngineImpl); } }
        public NetworkEngine NetworkEngine { get; private set; }
        public MessageHandlers MessageHandlers { get; private set; }
        public WebData WebData { get; private set; }
        public List<Tuple<Control, Action>> CustomControls { get; private set; }
        public BackgroundTask ArenaLoadTask { get; private set; }
        public bool IsReadyToStartArena { get; set; }
        public override bool IsShipControlsEnabled { get { return Logic.IsGameplay; } }

        public AssaultWing(GraphicsDeviceService graphicsDeviceService, CommandLineOptions args)
            : base(graphicsDeviceService, args)
        {
            CustomControls = new List<Tuple<Control, Action>>();
            MessageHandlers = new Net.MessageHandling.MessageHandlers(this);
            if (CommandLineOptions.DedicatedServer)
                Logic = new DedicatedServerLogic(this);
            else if (CommandLineOptions.QuickStart != null)
                Logic = new QuickStartLogic(this, CommandLineOptions.QuickStart);
            else
                Logic = new UserControlledLogic(this);
            ArenaLoadTask = new BackgroundTask();

            NetworkEngine = new NetworkEngine(this, 0);
            WebData = new WebData(this, 21);
            Components.Add(NetworkEngine);
            Components.Add(WebData);
            ChatStartControl = Settings.Controls.Chat.GetControl();
            _frameStepControl = new KeyboardKey(Keys.F8);
            _frameRunControl = new KeyboardKey(Keys.F7);
            _frameStep = false;
            DataEngine.SpectatorAdded += SpectatorAddedHandler;
            DataEngine.SpectatorRemoved += SpectatorRemovedHandler;
            NetworkEngine.Enabled = true;
            AW2.Graphics.PlayerViewport.CustomOverlayCreators.Add(viewport => new SystemStatusOverlay(viewport));

            // Replace the dummy StatsBase by a proper StatsSender.
            Components.Remove(comp => comp is StatsBase);
            Stats = new StatsSender(this, 7);
            Components.Add(Stats);
            Stats.Enabled = true;
        }

        public override void Update(AWGameTime gameTime)
        {
            base.Update(gameTime);
            Logic.Update();
            UpdateCustomControls();
            UpdateDebugKeys();
            SendGobCreationMessage();
            SendGameSettings();
        }

        /// <summary>
        /// Opens a URL (usually in the default web browser). Exceptions are caught and the user is notified.
        /// </summary>
        public void OpenURL(string url)
        {
            try
            {
                System.Diagnostics.Process.Start(url);
            }
            catch (Exception)
            {
                ShowInfoDialog("Couldn't open browser.\nPlease open this URL manually:\n" + url);
            }
        }

        // TODO !!! Inline >>>
        public void ShowDialog(OverlayDialogData dialogData) { Logic.ShowDialog(dialogData); }
        public void ShowCustomDialog(string text, string groupName, params TriggeredCallback[] actions) { Logic.ShowCustomDialog(text, groupName, actions); }
        public void ShowInfoDialog(string text, string groupName = null) { Logic.ShowInfoDialog(text, groupName); }
        public void HideDialog(string groupName = null) { Logic.HideDialog(groupName); }
        // TODO !!! Inline <<<

        public void ShowConnectingToGameServerDialog(string shortServerName)
        {
            ShowCustomDialog(string.Format("Connecting to {0}...\nPress Esc to cancel.", shortServerName), "Connecting to server",
                new TriggeredCallback(TriggeredCallback.CANCEL_CONTROL, CutNetworkConnections));
        }

        [Obsolete("Move to Logic")]
        public void ShowMainMenuAndResetGameplay()
        {
            Logic.ShowMainMenuAndResetGameplay();
        }

        /// <summary>
        /// Called after all components are initialized but before the first update in the game loop. 
        /// </summary>
        public override void BeginRun()
        {
            Log.Write("Assault Wing begins to run");
            Spectator.CreateStatsData = spectator => new SpectatorStats(spectator);
            var arenas = DataEngine.GetTypeTemplates<Arena>();
            if (!arenas.Any()) throw new ApplicationException("No arenas found");
            SelectedArenaName = arenas.First().Info.Name;
            DataEngine.GameplayMode = new GameplayMode();
            DataEngine.GameplayMode.ShipTypes = new[] { "Windlord", "Bugger", "Plissken" };
            DataEngine.GameplayMode.ExtraDeviceTypes = new[] { "blink", "repulsor", "catmoflage", "shield" };
            DataEngine.GameplayMode.Weapon2Types = new[] { "bazooka", "rockets", "hovermine", "power cone" };
            if (CommandLineOptions.DedicatedServer)
                WebData.Feed("1D");
            else if (CommandLineOptions.QuickStart != null)
            {
                WebData.Feed("1Q");
            }
            else
            {
                WebData.Feed("1");
            }
            Logic.Initialize();
            base.BeginRun();
        }

        public override void EndRun()
        {
            Logic.EndRun();
            base.EndRun();
        }

        public void PrepareArena(string arenaName, byte arenaIDOnClient)
        {
            SelectedArenaName = arenaName;
            Logic.ShowEquipMenu();
            LoadSelectedArena(arenaIDOnClient);
            Logic.PrepareArena();
        }

        /// <summary>
        /// Prepares a new play session to start from the arena called <see cref="SelectedArenaName"/>.
        /// Call <see cref="StartArena"/> after this method returns to start playing the arena.
        /// This method usually takes a long time to run. It's therefore a good
        /// idea to make it run in a background thread.
        /// </summary>
        public void LoadSelectedArena(byte? arenaIDOnClient = null)
        {
            var arenaTemplate = (Arena)DataEngine.GetTypeTemplate((CanonicalString)SelectedArenaName);
            // Note: Must create a new Arena instance and not use the existing template
            // because playing an arena will modify it.
            var arena = Arena.FromFile(this, arenaTemplate.Info.FileName);
            arena.ID = arenaIDOnClient.HasValue ? arenaIDOnClient.Value : _nextArenaID++;
            arena.Bin.Load(System.IO.Path.Combine(Paths.ARENAS, arena.BinFilename));
            arena.IsForPlaying = true;
            DataEngine.Arena = arena;
        }

        public void StartArenaBase() // TODO !!! Figure out a better name.
        {
            base.StartArena();
            PostFrameLogicEngine.DoEveryFrame += AfterEveryFrame;
        }

        public override void StartArena()
        {
            Stats.BasicInfoSent = false;
            WebData.Feed("2" + (int)NetworkMode);
            if (NetworkMode == NetworkMode.Server)
            {
                MessageHandlers.ActivateHandlers(MessageHandlers.GetServerGameplayHandlers());
                _pendingGobDeletionMessage = new GobDeletionMessage();
                DataEngine.Arena.GobRemoved += GobRemovedFromArenaHandler;
            }
            Logic.StartArena();
        }

        public override void RefreshGameSettings()
        {
            base.RefreshGameSettings();
            WebData.LoginPilots();
        }

        public void InitializePlayers(int count)
        {
            Settings.Players.Validate(this);
            var players = new[]
            {
                new Player(this, Settings.Players.Player1.Name,
                    (CanonicalString)Settings.Players.Player1.ShipName,
                    (CanonicalString)Settings.Players.Player1.Weapon2Name,
                    (CanonicalString)Settings.Players.Player1.ExtraDeviceName,
                    PlayerControls.FromSettings(Settings.Controls.Player1)),
                new Player(this, Settings.Players.Player2.Name,
                    (CanonicalString)Settings.Players.Player2.ShipName,
                    (CanonicalString)Settings.Players.Player2.Weapon2Name,
                    (CanonicalString)Settings.Players.Player2.ExtraDeviceName,
                    PlayerControls.FromSettings(Settings.Controls.Player2))
            };
            DataEngine.Spectators.Clear();
            foreach (var plr in players.Take(count)) DataEngine.Spectators.Add(plr);
        }

        /// <summary>
        /// Turns this game instance into a game server to whom other game instances
        /// can connect as game clients. Returns null on success, short error description on failure.
        /// </summary>
        public string StartServer()
        {
            if (NetworkMode != NetworkMode.Standalone)
                throw new InvalidOperationException("Cannot start server while in mode " + NetworkMode);
            NetworkMode = NetworkMode.Server;
            RefreshGameSettings();
            try
            {
                // TODO: Allow rejoin even if there are no free slots.
                NetworkEngine.StartServer(result => MessageHandlers.IncomingConnectionHandlerOnServer(result,
                    allowNewConnection: () => DataEngine.Players.Count() < Settings.Net.GameServerMaxPlayers));
                MessageHandlers.ActivateHandlers(MessageHandlers.GetServerMenuHandlers());
                return null;
            }
            catch (Exception e)
            {
                Log.Write("Could not start server: " + e);
                NetworkMode = NetworkMode.Standalone;
                var socketException = e as System.Net.Sockets.SocketException;
                if (socketException != null) return socketException.SocketErrorCode.ToString(); // TODO !!! Line wrapping and: return socketException.Message;
                return e.GetType().Name;
            }
        }

        public void StopServer()
        {
            Logic.StopServer();
        }

        /// <summary>
        /// Turns this game instance into a game client by connecting to a game server.
        /// </summary>
        public void StartClient(AWEndPoint[] serverEndPoints)
        {
            if (NetworkMode != NetworkMode.Standalone)
                throw new InvalidOperationException("Cannot start client while in mode " + NetworkMode);
            NetworkMode = NetworkMode.Client;
            RefreshGameSettings();
            IsClientAllowedToStartArena = false;
            try
            {
                NetworkEngine.StartClient(this, serverEndPoints, ConnectionResultOnClientCallback);
                foreach (var spectator in DataEngine.Spectators) spectator.ResetForClient();
            }
            catch (System.Net.Sockets.SocketException e)
            {
                Log.Write("Could not start client: " + e.Message);
                StopClient(null);
            }
        }

        public void StopClient(string errorOrNull)
        {
            Logic.StopClient(errorOrNull);
        }

        public void CutNetworkConnections()
        {
            switch (NetworkMode)
            {
                case NetworkMode.Client: StopClient(null); break;
                case NetworkMode.Server: StopServer(); break;
                case NetworkMode.Standalone: break;
                default: throw new ApplicationException("Unexpected NetworkMode: " + NetworkMode);
            }
        }

        public void SendMessageToAllPlayers(string text, Player from)
        {
            var messageContent = text.Trim();
            if (messageContent == "") return;
            var preText = from.Name + ">";
            var textColor = from.Color;
            var message = new PlayerMessage(preText, messageContent, textColor);
            switch (NetworkMode)
            {
                case NetworkMode.Server:
                    foreach (var plr in DataEngine.Players) plr.Messages.Add(message);
                    break;
                case NetworkMode.Client:
                    foreach (var plr in DataEngine.Players.Where(plr => plr.IsLocal)) plr.Messages.Add(message);
                    NetworkEngine.GameServerConnection.Send(new PlayerMessageMessage
                    {
                        PlayerID = -1,
                        Message = message,
                    });
                    break;
                default: throw new InvalidOperationException("Text messages not supported in mode " + NetworkMode);
            }
        }

        public void HandleGobCreationMessage(GobCreationMessage message, int framesAgo)
        {
            if (message.ArenaID != DataEngine.Arena.ID) return;
            message.ReadGobs(framesAgo,
                (typeName, layerIndex) =>
                {
                    if (layerIndex < 0 || layerIndex >= DataEngine.Arena.Layers.Count) return null;
                    var gob = (Gob)Clonable.Instantiate(typeName);
                    gob.Game = this;
                    gob.Layer = DataEngine.Arena.Layers[layerIndex];
                    return gob;
                },
                DataEngine.Arena.Gobs.Add);
        }

        protected override string GetStatusText()
        {
            string myStatusText = "";
            if (NetworkMode == NetworkMode.Client && NetworkEngine.IsConnectedToGameServer)
                myStatusText = string.Format(" [{0} ms lag]",
                    (int)NetworkEngine.ServerPingTime.TotalMilliseconds);
            if (NetworkMode == NetworkMode.Server)
                myStatusText = string.Join(" ", NetworkEngine.GameClientConnections
                    .Select(conn => string.Format(" [#{0}: {1} ms lag]", conn.ID, (int)conn.PingInfo.PingTime.TotalMilliseconds)
                    ).ToArray());
            return base.GetStatusText() + myStatusText;
        }

        protected override void FinishArenaImpl()
        {
            IsClientAllowedToStartArena = false;
            MessageHandlers.DeactivateHandlers(MessageHandlers.GetClientGameplayHandlers());
            MessageHandlers.DeactivateHandlers(MessageHandlers.GetServerGameplayHandlers());
            var standings = DataEngine.GameplayMode.GetStandings(DataEngine.Spectators).ToArray(); // ToArray takes a copy
            Stats.Send(new { ArenaFinished = standings.Select(st => new { st.Name, ((SpectatorStats)st.StatsData).LoginToken, st.Score, st.Kills, st.Deaths }).ToArray() });
            foreach (var spec in DataEngine.Spectators) if (spec.IsLocal) WebData.UpdatePilotRanking(spec);
            Logic.FinishArena();
#if NETWORK_PROFILING
            ProfilingNetworkBinaryWriter.DumpStats();
#endif
        }

        public void ApplyInGameGraphicsSettings()
        {
            if (Window == null) return;
            if (Settings.Graphics.IsVerticalSynced)
                Window.Impl.EnableVerticalSync();
            else
                Window.Impl.DisableVerticalSync();
            if (Settings.Graphics.InGameFullscreen)
                Window.Impl.SetFullScreen(Settings.Graphics.FullscreenWidth, Settings.Graphics.FullscreenHeight);
            else
                Window.Impl.SetWindowed();
        }

        private void UpdateCustomControls()
        {
            foreach (var controlAction in CustomControls)
                if (controlAction.Item1.Pulse) controlAction.Item2();
        }

        [System.Diagnostics.Conditional("DEBUG")]
        private void UpdateDebugKeys()
        {
            // Frame stepping (for debugging)
            if (_frameRunControl.Pulse)
            {
                LogicEngine.Enabled = PreFrameLogicEngine.Enabled = PostFrameLogicEngine.Enabled = true;
                _frameStep = false;
            }
            if (_frameStep)
            {
                if (_frameStepControl.Pulse)
                    LogicEngine.Enabled = PreFrameLogicEngine.Enabled = PostFrameLogicEngine.Enabled = true;
                else
                    LogicEngine.Enabled = PreFrameLogicEngine.Enabled = PostFrameLogicEngine.Enabled = false;
            }
            else if (_frameStepControl.Pulse)
            {
                LogicEngine.Enabled = PreFrameLogicEngine.Enabled = PostFrameLogicEngine.Enabled = false;
                _frameStep = true;
            }
        }

        private void SynchronizeFrameNumber()
        {
            if (NetworkMode != NetworkMode.Client) return;
            if (!NetworkEngine.IsConnectedToGameServer) return;
            if (_lastFrameNumberSynchronization + TimeSpan.FromSeconds(1) > GameTime.TotalRealTime) return;
            _lastFrameNumberSynchronization = GameTime.TotalRealTime;
            var remoteFrameNumberOffset = NetworkEngine.GameServerConnection.PingInfo.RemoteFrameNumberOffset;
            DataEngine.Arena.FrameNumber -= remoteFrameNumberOffset;
            NetworkEngine.GameServerConnection.PingInfo.AdjustRemoteFrameNumberOffset(remoteFrameNumberOffset);
        }

        private void SendGobCreationMessage()
        {
            if (NetworkMode != NetworkMode.Server) return;
            if (ArenaLoadTask.TaskRunning) return; // wait for arena load completion
            if (DataEngine.Arena == null) return; // happens if gobs are created on the frame the arena ends
            foreach (var conn in NetworkEngine.GameClientConnections)
            {
                var gobsToSend = DataEngine.Arena.Gobs.GameplayLayer.Gobs.Where(gob => gob.IsRelevant && !gob.ClientStatus[1 << conn.ID]);
                if (!gobsToSend.Any()) continue;
                var message = new GobCreationMessage { ArenaID = DataEngine.Arena.ID };
                foreach (var gob in gobsToSend.Take(10)) // Avoid sending lots of gobs in one frame.
                {
                    message.AddGob(gob);
                    gob.ClientStatus[1 << conn.ID] = true;
                }
                conn.Send(message);
            }
        }

        private void GobRemovedFromArenaHandler(Gob gob)
        {
            if (!gob.IsRelevant) return;
            _pendingGobDeletionMessage.GobIDs.Add(gob.ID);
        }

        private void SpectatorAddedHandler(Spectator spectator)
        {
            if (NetworkMode == NetworkMode.Server) UpdateGameServerInfoToManagementServer();
            spectator.ArenaStatistics.Rating = () => spectator.GetStats().Rating;
            spectator.ResetForArena();
            if (NetworkMode != NetworkMode.Server || spectator.IsLocal) return;
            var player = spectator as Player;
            if (player == null) return;
            player.IsAllowedToCreateShip = () => player.IsRemote && NetworkEngine.GetGameClientConnection(player.ConnectionID).ConnectionStatus.IsRequestingSpawn;
            player.Messages.NewChatMessage += mess => SendPlayerMessageToRemoteSpectator(mess, player);
            player.Messages.NewCombatLogMessage += mess => SendPlayerMessageToRemoteSpectator(mess, player);
        }

        private void SendPlayerMessageToRemoteSpectator(PlayerMessage message, Player player)
        {
            if (!player.IsRemote) return;
            try
            {
                var messageMessage = new PlayerMessageMessage { PlayerID = player.ID, Message = message };
                NetworkEngine.GetGameClientConnection(player.ConnectionID).Send(messageMessage);
            }
            catch (InvalidOperationException)
            {
                // The connection of the player doesn't exist any more. Just don't send the message then.
            }
        }

        private void SpectatorRemovedHandler(Spectator spectator)
        {
            if (NetworkMode != NetworkMode.Server) return;
            UpdateGameServerInfoToManagementServer();
            var clientMessage = new PlayerDeletionMessage { PlayerID = spectator.ID };
            NetworkEngine.SendToGameClients(clientMessage);
        }

        private void ConnectionResultOnClientCallback(Result<Connection> result)
        {
            HideDialog("Connecting to server");
            if (NetworkEngine.GameServerConnection != null)
            {
                // Silently ignore extra server connection attempts.
                if (result.Successful) result.Value.Dispose();
                return;
            }

            if (!result.Successful)
            {
                Log.Write("Failed to connect to server: " + result.Error);
                StopClient("Failed to connect to server.");
            }
            else
            {
                MessageHandlers.DeactivateHandlers(MessageHandlers.GetStandaloneMenuHandlers(null));
                NetworkEngine.GameServerConnection = result.Value;
                MessageHandlers.ActivateHandlers(MessageHandlers.GetClientMenuHandlers());
                var joinRequest = new GameServerHandshakeRequestTCP
                {
                    CanonicalStrings = CanonicalString.CanonicalForms,
                    GameClientKey = NetworkEngine.GetAssaultWingInstanceKey(),
                };
                NetworkEngine.GameServerConnection.Send(joinRequest);
            }
        }

        public void UpdateGameServerInfoToManagementServer()
        {
            var managementMessage = new UpdateGameServerMessage { CurrentClients = DataEngine.Players.Count() };
            NetworkEngine.ManagementServerConnection.Send(managementMessage);
        }

        private void AfterEveryFrame()
        {
#if NETWORK_PROFILING
            if (DataEngine.Arena.FrameNumber == 1) ProfilingNetworkBinaryWriter.Reset();
            using (new NetworkProfilingScope(string.Format("Frame {0:0000}", DataEngine.Arena.FrameNumber)))
#endif
            {
                SendMessagesOnServer();
                SendMessagesOnClient();
            }
            SynchronizeFrameNumber();
        }

        private void SendMessagesOnServer()
        {
            if (NetworkMode != NetworkMode.Server) return;
            SendGobUpdatesToRemote(DataEngine.Arena.Gobs.GameplayLayer.Gobs,
                SerializationModeFlags.VaryingDataFromServer, NetworkEngine.GameClientConnections);
            SendPlayerUpdatesOnServer();
            SendGobDeletionsOnServer();
            SendArenaStatisticsOnServer();
        }

        private void SendMessagesOnClient()
        {
            if (NetworkMode != NetworkMode.Client) return;
            SendGobUpdatesToRemote(DataEngine.Minions.Where(gob => gob.Owner != null && gob.Owner.IsLocal),
                SerializationModeFlags.VaryingDataFromClient, new[] { NetworkEngine.GameServerConnection });
            SendPlayerUpdatesOnClient();
        }

        private void SendArenaStatisticsOnServer()
        {
            if (!DataEngine.NextArenaStatisticsToClients.HasValue) return;
            if (DataEngine.NextArenaStatisticsToClients.Value > GameTime.TotalRealTime) return;
            DataEngine.CheckArenaStatisticsToClients();
            var message = new ArenaStatisticsMessage();
            foreach (var spec in DataEngine.Spectators) message.AddSpectatorStatistics(spec.ID, spec.ArenaStatistics);
            NetworkEngine.SendToGameClients(message);
        }

        private void SendGobDeletionsOnServer()
        {
            if ((DataEngine.ArenaFrameCount % 3) != 0) return;
            if (!_pendingGobDeletionMessage.GobIDs.Any()) return;
            NetworkEngine.SendToGameClients(_pendingGobDeletionMessage);
            _pendingGobDeletionMessage = new GobDeletionMessage();
        }

        private void SendPlayerUpdatesOnServer()
        {
            foreach (var spectator in DataEngine.Spectators)
            {
                if (spectator.ClientUpdateRequest == Spectator.ClientUpdateType.None) continue;
                var plrMessage = new SpectatorUpdateMessage();
                plrMessage.SpectatorID = spectator.ID;
                plrMessage.Write(spectator, SerializationModeFlags.VaryingDataFromServer);
                if (spectator.ClientUpdateRequest.HasFlag(Player.ClientUpdateType.ToEveryone))
                    NetworkEngine.SendToGameClients(plrMessage);
                else if (spectator.ClientUpdateRequest.HasFlag(Player.ClientUpdateType.ToOwnerOnly) && spectator.IsRemote)
                    NetworkEngine.GetGameClientConnection(spectator.ConnectionID).Send(plrMessage);
                spectator.ClientUpdateRequest = Spectator.ClientUpdateType.None;
            }
        }

        private void SendPlayerUpdatesOnClient()
        {
            if (!IsShipControlsEnabled) return;
            foreach (var player in DataEngine.Players.Where(plr => plr.IsLocal && plr.ID != Spectator.UNINITIALIZED_ID))
            {
                var message = new PlayerControlsMessage();
                message.PlayerID = player.ID;
                foreach (PlayerControlType controlType in Enum.GetValues(typeof(PlayerControlType)))
                    message.SetControlState(controlType, player.Controls[controlType].State);
                NetworkEngine.GameServerConnection.Send(message);
            }
        }

        private void SendGobUpdatesToRemote(IEnumerable<Gob> gobs, SerializationModeFlags serializationMode, IEnumerable<Connection> connections)
        {
            if (serializationMode.HasFlag(SerializationModeFlags.VaryingDataFromServer) && (DataEngine.ArenaFrameCount % 3) != 0) return;
            var now = DataEngine.ArenaTotalTime;
            var gobMessage = new GobUpdateMessage();
            var debugMessage = gobs.OfType<AW2.Game.Gobs.Wall>().Any(wall => wall.ForcedNetworkUpdate)
                ? new System.Text.StringBuilder("Gob update ")
                : null; // DEBUG: catch a rare crash that seems to happen only when serializing walls.
            foreach (var gob in gobs)
            {
                if (!gob.ForcedNetworkUpdate)
                {
                    if (!gob.IsRelevant) continue;
                    if (!gob.Movable) continue;
                    if (gob.NetworkUpdatePeriod == TimeSpan.Zero) continue;
                    if (gob.LastNetworkUpdate + gob.NetworkUpdatePeriod > now) continue;
                }
                gob.ForcedNetworkUpdate = false;
                gob.LastNetworkUpdate = now;
                gobMessage.AddGob(gob.ID, gob, serializationMode);
                if (debugMessage != null) debugMessage.AppendFormat("{0} [{1}], ", gob.GetType().Name, gob.TypeName); // DEBUG: catch a rare crash that seems to happen only when serializing walls.
            }
            gobMessage.CollisionEvents = DataEngine.Arena.GetCollisionEvents();
            foreach (var conn in connections) conn.Send(gobMessage);

            if (Settings.Net.HeavyDebugLog && connections.Any() && debugMessage != null) // DEBUG: catch a rare crash that seems to happen only when serializing walls.
            {
                var writer = new NetworkBinaryWriter(new System.IO.MemoryStream(_debugBuffer));
                gobMessage.Serialize(writer);
                debugMessage.Append(MiscHelper.BytesToString(new ArraySegment<byte>(_debugBuffer, 0, (int)writer.GetBaseStream().Position)));
                Log.Write(debugMessage.ToString());
            }
        }

        private void SendGameSettings()
        {
            if (_lastGameSettingsSent.SecondsAgoRealTime() < 1) return;
            _lastGameSettingsSent = GameTime.TotalRealTime;
            switch (NetworkMode)
            {
                case NetworkMode.Client:
                    if (NetworkEngine.IsConnectedToGameServer)
                        SendSpectatorSettingsToGameServer(p => p.IsLocal && p.ServerRegistration != Spectator.ServerRegistrationType.Requested);
                    break;
                case NetworkMode.Server:
                    SendSpectatorSettingsToGameClients(p => p.ID != Spectator.UNINITIALIZED_ID);
                    SendGameSettingsToRemote(NetworkEngine.GameClientConnections);
                    break;
            }
        }

        private void SendGameSettingsToRemote(IEnumerable<Connection> connections)
        {
            var mess = new GameSettingsRequest { ArenaToPlay = SelectedArenaName };
            foreach (var conn in connections) conn.Send(mess);
        }

        private void SendSpectatorSettingsToGameServer(Func<Spectator, bool> sendCriteria)
        {
            Func<Spectator, SpectatorSettingsRequest> newPlayerSettingsRequest = spec => new SpectatorSettingsRequest
            {
                IsRegisteredToServer = spec.ServerRegistration == Spectator.ServerRegistrationType.Yes,
                IsRequestingSpawn = Logic.IsGameplay,
                IsGameClientReadyToStartArena = IsReadyToStartArena,
                SpectatorID = spec.ServerRegistration == Spectator.ServerRegistrationType.Yes ? spec.ID : spec.LocalID,
                Subclass = SpectatorSettingsRequest.GetSubclassType(spec),
            };
            SendSpectatorSettingsToRemote(SerializationModeFlags.ConstantDataFromClient, sendCriteria, newPlayerSettingsRequest, new[] { NetworkEngine.GameServerConnection });
        }

        private void SendSpectatorSettingsToGameClients(Func<Spectator, bool> sendCriteria)
        {
            Func<Spectator, SpectatorSettingsRequest> newPlayerSettingsRequest = spec => new SpectatorSettingsRequest
            {
                IsRegisteredToServer = true,
                SpectatorID = spec.ID,
                Subclass = SpectatorSettingsRequest.GetSubclassType(spec),
            };
            SendSpectatorSettingsToRemote(SerializationModeFlags.ConstantDataFromServer | SerializationModeFlags.VaryingDataFromServer,
                sendCriteria, newPlayerSettingsRequest, NetworkEngine.GameClientConnections);
            foreach (var conn in NetworkEngine.GameClientConnections) conn.ConnectionStatus.HasPlayerSettings = true;
        }

        private void SendSpectatorSettingsToRemote(SerializationModeFlags mode, Func<Spectator, bool> sendCriteria, Func<Spectator, SpectatorSettingsRequest> newSpectatorSettingsRequest, IEnumerable<Connection> connections)
        {
            foreach (var spectator in DataEngine.Spectators.Where(sendCriteria))
            {
                var mess = newSpectatorSettingsRequest(spectator);
                mess.Write(spectator, mode);
                if (spectator.ServerRegistration == Spectator.ServerRegistrationType.No)
                    spectator.ServerRegistration = Spectator.ServerRegistrationType.Requested;
                foreach (var conn in connections) conn.Send(mess);
            }
        }
    }
}
