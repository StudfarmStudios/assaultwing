using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework.Input;
using AW2.Core.GameComponents;
using AW2.Game;
using AW2.Game.Collisions;
using AW2.Game.GobUtils;
using AW2.Game.Logic;
using AW2.Game.Players;
using AW2.Helpers;
using AW2.Helpers.Serialization;
using AW2.Net;
using AW2.Net.Connections;
using AW2.Net.ManagementMessages;
using AW2.Net.MessageHandling;
using AW2.Net.Messages;
using AW2.UI;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace AW2.Core
{
    [System.Diagnostics.DebuggerDisplay("AssaultWing {Logic}")]
    public class AssaultWing : AssaultWingCore
    {
        private AWTimer _gameSettingsSendTimer;
        private AWTimer _arenaStateSendTimer;
        private AWTimer _frameNumberSynchronizationTimer;
        private byte _nextArenaID;
        private GobDeletionMessage _pendingGobDeletionMessage;
        private ClientGameStateUpdateMessage _pendingClientGameStateUpdateMessage;
        private List<Tuple<GobCreationMessage, int>> _gobCreationMessages = new List<Tuple<GobCreationMessage, int>>();
        private List<CollisionEvent> _collisionEventsToRemote;
        private AWTimer _debugPrintLagTimer;
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

        public string SelectedArenaName { get; set; }
        private ProgramLogic Logic { get; set; }
        public bool LogicStateChanged { get { return Logic.GameStateChanged;} set { Logic.GameStateChanged = value; }}
        public UIEngineImpl UIEngine { get { return (UIEngineImpl)Components.First(c => c is UIEngineImpl); } }
        public NetworkEngine NetworkEngine { get; private set; }
        public MessageHandlers MessageHandlers { get; private set; }
        public WebData WebData { get; private set; }
        public List<Tuple<Control, Action>> CustomControls { get; private set; }
        public BackgroundTask ArenaLoadTask { get; private set; }
        public bool IsReadyToStartArena { get; set; }
        public override bool IsShipControlsEnabled { get { return Logic.IsGameplay; } }
        public Guid GameServerGUID { get; private set; }

        /// <summary>
        /// Errors that occurred when establishing or maintaining a network game instance.
        /// These errors are eventually reported to the user and game state is reset out of networking.
        /// </summary>
        public Queue<string> NetworkingErrors { get; private set; }

        public AssaultWing(GameServiceContainer serviceContainer, CommandLineOptions args)
            : base(serviceContainer, args)
        {
            CustomControls = new List<Tuple<Control, Action>>();
            MessageHandlers = new Net.MessageHandling.MessageHandlers(this);

            Logic = new DedicatedServerLogic(this);

            ArenaLoadTask = new BackgroundTask();
            NetworkingErrors = new Queue<string>();
            _gameSettingsSendTimer = new AWTimer(() => GameTime.TotalRealTime, TimeSpan.FromSeconds(2)) { SkipPastIntervals = true };
            _arenaStateSendTimer = new AWTimer(() => GameTime.TotalRealTime, TimeSpan.FromSeconds(2)) { SkipPastIntervals = true };
            _frameNumberSynchronizationTimer = new AWTimer(() => GameTime.TotalRealTime, TimeSpan.FromSeconds(1)) { SkipPastIntervals = true };

            NetworkEngine = new NetworkEngine(this, 30);
            WebData = new WebData(this, 21);
            Components.Add(NetworkEngine);
            Components.Add(WebData);

            _frameStepControl = new KeyboardKey(Keys.F8);
            _frameRunControl = new KeyboardKey(Keys.F7);
            _frameStep = false;
            _debugPrintLagTimer = new AWTimer(() => GameTime.TotalRealTime, TimeSpan.FromSeconds(1)) { SkipPastIntervals = true };
            DataEngine.SpectatorAdded += SpectatorAddedHandler;
            DataEngine.SpectatorRemoved += SpectatorRemovedHandler;
            NetworkEngine.Enabled = true;

            // Replace the dummy StatsBase by a proper StatsSender.
            Components.Remove(comp => comp is StatsBase);
            Stats = new StatsSender(this, 7);
            Components.Add(Stats);
            Stats.Enabled = true;
        }

        protected override void UpdateImpl()
        {
            HandleGobCreationMessages();
            base.UpdateImpl();
            ProcessPendingRemoteSpectatorsOnServer();
            SendServerStateToStats();
            Logic.Update();
            UpdateCustomControls();
            UpdateDebugKeys();
            DebugPrintLag();
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
                Logic.ShowInfoDialog("Couldn't open browser.\nPlease open this URL manually:\n" + url);
            }
        }


        public void ShowConnectingToGameServerDialog(string shortServerName)
        {
            Logic.ShowCustomDialog(string.Format("Connecting to {0}...\nPress Esc to cancel.", shortServerName), "Connecting to server",
                new TriggeredCallback(TriggeredCallback.CANCEL_CONTROL, CutNetworkConnections));
        }

        /// <summary>
        /// Called after all components are initialized but before the first update in the game loop. 
        /// </summary>
        public override void BeginRun()
        {
            Log.Write("Assault Wing begins to run");
            Spectator.CreateStatsData = spectator => new SpectatorStats(spectator);
            DataEngine.GameplayMode = DataEngine.GetTypeTemplates<GameplayMode>().First();
            SelectedArenaName = DataEngine.GameplayMode.Arenas.First();

            Logic.Initialize();
            base.BeginRun();
        }

        public override void EndRun()
        {
            Logic.EndRun();
            base.EndRun();
        }

        public void PrepareArenaOnClient(CanonicalString gameplayMode, string arenaName, byte arenaIDOnClient, int wallCount)
        {
            Debug.Assert(NetworkMode == Core.NetworkMode.Client);
            DataEngine.GameplayMode = (GameplayMode)DataEngine.GetTypeTemplate(gameplayMode);
            SelectedArenaName = arenaName;
            Logic.ShowEquipMenu();
            LoadSelectedArena(arenaIDOnClient);
            Logic.PrepareArena(wallCount);
        }

        /// <summary>
        /// Prepares a new play session to start from the arena called <see cref="SelectedArenaName"/>.
        /// Call <see cref="StartArena"/> after this method returns to start playing the arena.
        /// </summary>
        public void LoadSelectedArena(byte? arenaIDOnClient = null)
        {
            if (NetworkMode != Core.NetworkMode.Client) DataEngine.RemoveEmptyTeams();
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
            _collisionEventsToRemote = new List<CollisionEvent>();
            PostFrameLogicEngine.DoEveryFrame += AfterEveryFrame;
        }

        public override void StartArena()
        {
            Stats.BasicInfoSent = false;
            WebData.Feed("2" + (int)NetworkMode);
            switch (NetworkMode)
            {
                case NetworkMode.Server:
                    MessageHandlers.ActivateHandlers(MessageHandlers.GetServerGameplayHandlers());
                    _pendingGobDeletionMessage = null;
                    DataEngine.Arena.GobRemoved += GobRemovedFromArenaHandler;
                    break;
                case NetworkMode.Client:
                    _pendingClientGameStateUpdateMessage = new ClientGameStateUpdateMessage();
                    break;
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
            DataEngine.Teams.Clear();
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
            GameServerGUID = Guid.NewGuid();
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
            GameServerGUID = Guid.Empty;
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
            // Note: Clients are supposed to create teams only with local IDs (negative).
            // Remove existing teams because they have global IDs (positive).
            foreach (var spec in DataEngine.Spectators) spec.AssignTeam(null);
            DataEngine.RemoveEmptyTeams();
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

        public void GobCreationMessageReceived(GobCreationMessage message, int framesAgo)
        {
            lock (_gobCreationMessages) _gobCreationMessages.Add(Tuple.Create(message, framesAgo));
        }

        private void HandleGobCreationMessages()
        {
            List<Tuple<GobCreationMessage, int>> messages;
            lock (_gobCreationMessages)
            {
                messages = _gobCreationMessages;
                _gobCreationMessages = new List<Tuple<GobCreationMessage, int>>();
            }
            foreach (var messageAndFramesAgo in messages)
                HandleGobCreationMessage(messageAndFramesAgo.Item1, messageAndFramesAgo.Item2);
        }

        private void HandleGobCreationMessage(GobCreationMessage message, int framesAgo)
        {
            if (message.ArenaID != DataEngine.Arena.ID) return;
            var updatedGobs = new Dictionary<int, Arena.GobUpdateData>();
            message.ReadGobs(framesAgo,
                (typeName, layerIndex) =>
                {
                    if (layerIndex < 0 || layerIndex >= DataEngine.Arena.Layers.Count) return null;
                    var gob = (Gob)Clonable.Instantiate(this, typeName);
                    gob.Game = this;
                    gob.Layer = DataEngine.Arena.Layers[layerIndex];
                    gob.BirthTime = DataEngine.ArenaTotalTime - TargetElapsedTime.Multiply(framesAgo);
                    return gob;
                },
                gob =>
                {
                    DataEngine.Arena.Gobs.Add(gob);
                    updatedGobs.Add(gob.ID, new Arena.GobUpdateData(gob, framesAgo));
                });
            DataEngine.Arena.FinalizeGobUpdatesOnClient(updatedGobs, framesAgo);
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
            var standings = DataEngine.GameplayMode.GetStandings(DataEngine.Spectators);
            Stats.Send(new
            {
                ArenaFinished = standings.Select(st => new
                {
                    st.Name,
                    LoginToken = st.StatsData == null ? "" : ((SpectatorStats)st.StatsData).LoginToken,
                    st.Score,
                    st.Kills,
                    st.Deaths,
                }).ToArray()
            });
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

        private void DebugPrintLag()
        {
            if (!Settings.Net.LagLog || !_debugPrintLagTimer.IsElapsed) return;
            var socketLag = AW2.Net.ConnectionUtils.AWSocket.GetDebugPrintLagStringOrNull();
            var gobUpdateLag = NetworkEngine.GetDebugPrintLagStringOrNull();
            var lagString = gobUpdateLag != null && socketLag != null ? gobUpdateLag + "\t" + socketLag
                : gobUpdateLag ?? socketLag ?? null;
            if (lagString != null) Log.Write(lagString);
        }

        private void SynchronizeFrameNumber()
        {
            if (NetworkMode != NetworkMode.Client) return;
            if (!NetworkEngine.IsConnectedToGameServer) return;
            if (!_frameNumberSynchronizationTimer.IsElapsed) return;
            var remoteFrameNumberOffset = NetworkEngine.GameServerConnection.PingInfo.RemoteFrameNumberOffset;
            DataEngine.Arena.FrameNumber -= remoteFrameNumberOffset;
            NetworkEngine.GameServerConnection.PingInfo.AdjustRemoteFrameNumberOffset(remoteFrameNumberOffset);
        }

        private void GobRemovedFromArenaHandler(Gob gob)
        {
            if (!gob.IsRelevant) return;
            _pendingGobDeletionMessage =_pendingGobDeletionMessage ?? new GobDeletionMessage();
            _pendingGobDeletionMessage.GobIDs.Add(gob.ID);
        }

        private void SpectatorAddedHandler(Spectator spectator)
        {
            //if (NetworkMode == NetworkMode.Server) UpdateGameServerInfoToManagementServer();
            spectator.ArenaStatistics.Rating = () => spectator.GetStats().Rating;
            spectator.ResetForArena();
            if (NetworkMode != NetworkMode.Server || spectator.IsLocal) return;
            var player = spectator as Player;
            if (player == null) return;
            player.IsAllowedToCreateShip = () =>
            {
                if (!player.IsRemote) return false;
                var arenaID = NetworkEngine.GetGameClientConnection(player.ConnectionID).ConnectionStatus.IsRequestingSpawnForArenaID;
                if (!arenaID.HasValue || DataEngine.Arena == null) return false;
                return arenaID.Value == DataEngine.Arena.ID;
            };
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
            Stats.Send(new { RemovePlayer = spectator.GetStats().LoginToken, Name = spectator.Name });
            UpdateGameServerInfoToManagementServer();
            NetworkEngine.SendToGameClients(new SpectatorOrTeamDeletionMessage { SpectatorOrTeamID = spectator.ID });
        }

        private void ConnectionResultOnClientCallback(Result<Connection> result)
        {
            Logic.HideDialog("Connecting to server");
            if (NetworkEngine.IsConnectedToGameServer)
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
                // MessageHandlers.DeactivateHandlers(MessageHandlers.GetStandaloneMenuHandlers(null));
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
            // TODO: Peter: Removed to avoid requiring management server
            //var managementMessage = new UpdateGameServerMessage { CurrentClients = DataEngine.Players.Count() };
            //NetworkEngine.ManagementServerConnection.Send(managementMessage);
        }

        private void AfterEveryFrame()
        {
            if (NetworkMode == NetworkMode.Server) _collisionEventsToRemote.AddRange(
                DataEngine.Arena.GetCollisionEvents().Where(e => e.IrreversibleSideEffectsPerformed));
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
            SendGobCreationMessageOnServer();
            SendGameSettingsOnServer();
            SendGobUpdateMessageOnServer();
            SendSpectatorAndTeamUpdatesOnServer();
            SendGobDeletionsOnServer();
        }

        private void SendMessagesOnClient()
        {
            if (NetworkMode != NetworkMode.Client || !NetworkEngine.IsConnectedToGameServer) return;
            SendGameSettingsOnClient();
            if (!IsShipControlsEnabled) return;
            SetPlayerControls(_pendingClientGameStateUpdateMessage);
            SendGobUpdateMessageOnClient();
        }

        private void SendGobDeletionsOnServer()
        {
            if ((DataEngine.ArenaFrameCount % 3) != 0) return;
            if (_pendingGobDeletionMessage == null) return;
            NetworkEngine.SendToGameClients(_pendingGobDeletionMessage);
            _pendingGobDeletionMessage = null;
        }

        private void SendSpectatorAndTeamUpdatesOnServer()
        {
            if (!_arenaStateSendTimer.IsElapsed) return;
            var message = new SpectatorOrTeamUpdateMessage();
            foreach (var spec in DataEngine.Spectators) message.Add(spec.ID, spec, SerializationModeFlags.VaryingDataFromServer);
            foreach (var team in DataEngine.Teams) message.Add(team.ID, team, SerializationModeFlags.VaryingDataFromServer);
            NetworkEngine.SendToGameClients(message);
        }

        private void SetPlayerControls(ClientGameStateUpdateMessage message)
        {
            var player = DataEngine.LocalPlayer;
            if (player == null || player.ID == Spectator.UNINITIALIZED_ID) return;
            message.PlayerID = player.ID;
            foreach (PlayerControlType controlType in Enum.GetValues(typeof(PlayerControlType)))
                message.AddControlState(controlType, player.Controls[controlType].State);
        }

        private void SendGobCreationMessageOnServer()
        {
            if (ArenaLoadTask.TaskRunning) return; // wait for arena load completion
            if (DataEngine.Arena == null) return; // happens if gobs are created on the frame the arena ends
            foreach (var conn in NetworkEngine.GameClientConnections)
            {
                var gobsToSend = DataEngine.Arena.GobsInRelevantLayers.Where(gob => gob.IsRelevant && !gob.ClientStatus[1 << conn.ID]);
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

        private void SendGobUpdateMessageOnServer()
        {
            if ((DataEngine.ArenaFrameCount % 3) != 0) return;
            var gobUpdateMessage = new GobUpdateMessage();
            PopulateGobUpdateMessage(gobUpdateMessage, DataEngine.Arena.GobsInRelevantLayers, SerializationModeFlags.VaryingDataFromServer);
            if (gobUpdateMessage.HasContent) foreach (var conn in NetworkEngine.GameClientConnections) conn.Send(gobUpdateMessage);
        }

        private void SendGobUpdateMessageOnClient()
        {
            if ((DataEngine.ArenaFrameCount % 2) != 0) return;
            PopulateGobUpdateMessage(_pendingClientGameStateUpdateMessage,
                DataEngine.Minions.Where(gob => gob.Owner != null && gob.Owner.IsLocal),
                SerializationModeFlags.VaryingDataFromClient);
            NetworkEngine.GameServerConnection.Send(_pendingClientGameStateUpdateMessage);
            _pendingClientGameStateUpdateMessage = new ClientGameStateUpdateMessage();
        }

        private void PopulateGobUpdateMessage(GobUpdateMessage gobMessage, IEnumerable<Gob> gobs, SerializationModeFlags serializationMode)
        {
            var now = DataEngine.ArenaTotalTime;
            var debugMessage = Settings.Net.HeavyDebugLog && gobs.OfType<AW2.Game.Gobs.Wall>().Any(wall => wall.ForcedNetworkUpdate)
                ? new System.Text.StringBuilder("Gob update ")
                : null; // DEBUG: catch a rare crash that seems to happen only when serializing walls.
            foreach (var gob in gobs)
            {
                if (!gob.ForcedNetworkUpdate)
                {
                    if (!gob.IsRelevant) continue;
                    if (gob.MoveType == MoveType.Static) continue;
                    if (gob.NetworkUpdatePeriod == TimeSpan.Zero) continue;
                    if (gob.LastNetworkUpdate + gob.NetworkUpdatePeriod > now) continue;
                }
                gob.ForcedNetworkUpdate = false;
                gob.LastNetworkUpdate = now;
                gobMessage.AddGob(gob.ID, gob, serializationMode);
                if (debugMessage != null) debugMessage.AppendFormat("{0} [{1}], ", gob.GetType().Name, gob.TypeName); // DEBUG: catch a rare crash that seems to happen only when serializing walls.
            }
            gobMessage.SetCollisionEvents(_collisionEventsToRemote, serializationMode);
            _collisionEventsToRemote = new List<CollisionEvent>();

            if (debugMessage != null) // DEBUG: catch a rare crash that seems to happen only when serializing walls.
            {
                var writer = new NetworkBinaryWriter(new System.IO.MemoryStream(_debugBuffer));
                gobMessage.Serialize(writer);
                debugMessage.Append(MiscHelper.BytesToString(new ArraySegment<byte>(_debugBuffer, 0, (int)writer.GetBaseStream().Position)));
                Log.Write(debugMessage.ToString());
            }
        }

        private void SendGameSettingsOnServer()
        {
            if (!_gameSettingsSendTimer.IsElapsed) return;
            SendSpectatorSettingsToGameClients(p => p.ID != Spectator.UNINITIALIZED_ID);
            SendTeamSettingsToGameClients();
            var mess = new GameSettingsRequest { ArenaToPlay = SelectedArenaName, GameplayMode = DataEngine.GameplayMode.Name };
            foreach (var conn in NetworkEngine.GameClientConnections) conn.Send(mess);
        }

        private void SendGameSettingsOnClient()
        {
            if (!_gameSettingsSendTimer.IsElapsed) return;
            SendSpectatorSettingsToGameServer(p => p.IsLocal && p.ServerRegistration != Spectator.ServerRegistrationType.Requested);
        }

        private void SendSpectatorSettingsToGameServer(Func<Spectator, bool> sendCriteria)
        {
            Func<Spectator, SpectatorSettingsRequest> newPlayerSettingsRequest = spec => new SpectatorSettingsRequest
            {
                IsRegisteredToServer = spec.ServerRegistration == Spectator.ServerRegistrationType.Yes,
                IsRequestingSpawnForArenaID = Logic.IsGameplay ? DataEngine.Arena.ID : (byte?)null,
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
            SendSpectatorSettingsToRemote(SerializationModeFlags.ConstantDataFromServer, sendCriteria, newPlayerSettingsRequest, NetworkEngine.GameClientConnections);
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
                foreach (var conn in connections) if (spectator.ConnectionID != conn.ID) conn.Send(mess);
            }
        }

        private void SendTeamSettingsToGameClients()
        {
            var mess = new TeamSettingsMessage();
            var serializationMode = SerializationModeFlags.ConstantDataFromServer | SerializationModeFlags.VaryingDataFromServer;
            foreach (var team in DataEngine.Teams) mess.Add(team.ID, team, serializationMode);
            foreach (var conn in NetworkEngine.GameClientConnections) conn.Send(mess);
        }

        public void AddRemoteSpectator(Spectator newSpectator)
        {
            Log.Write("Adding spectator {0}", newSpectator.Name);
            DataEngine.Spectators.Add(newSpectator);
            Stats.Send(new { AddPlayer = newSpectator.GetStats().LoginToken, Name = newSpectator.Name });
        }

        public void ReconnectRemoteSpectatorOnServer(Spectator newSpectator, Spectator oldSpectator)
        {
            Log.Write("Reconnecting spectator {0}", oldSpectator.Name);
            oldSpectator.ReconnectOnServer(newSpectator);
            Stats.Send(new { AddPlayer = oldSpectator.GetStats().LoginToken, Name = oldSpectator.Name });
        }

        public void RefuseRemoteSpectatorOnServer(Spectator newSpectator, Spectator oldSpectator)
        {
            var ipAddress = NetworkEngine.GetConnection(newSpectator.ConnectionID).RemoteTCPEndPoint.Address;
            Log.Write("Refusing spectator {0} from {1} because he's already logged in from {2}.",
                newSpectator.Name, ipAddress, (object)oldSpectator.IPAddress ?? "the local host");
        }

        private void ProcessPendingRemoteSpectatorsOnServer()
        {
            DataEngine.ProcessPendingRemoteSpectatorsOnServer(spectator =>
            {
                var stats = spectator.GetStats();
                var mess = new SpectatorSettingsReply
                {
                    SpectatorLocalID = spectator.LocalID,
                    SpectatorID = Spectator.UNINITIALIZED_ID,
                    FailMessage = "",
                };
                if (stats.IsLoggedIn)
                {
                    if (stats.PilotId == null) return false;
                    var oldSpectator = DataEngine.Spectators.FirstOrDefault(spec =>
                        spec.GetStats().IsLoggedIn && spec.GetStats().PilotId == stats.PilotId);
                    if (oldSpectator == null)
                    {
                        AddRemoteSpectator(spectator);
                        mess.SpectatorID = spectator.ID;
                    }
                    else if (oldSpectator.IsDisconnected)
                    {
                        ReconnectRemoteSpectatorOnServer(spectator, oldSpectator);
                        mess.SpectatorID = oldSpectator.ID;
                    }
                    else
                    {
                        RefuseRemoteSpectatorOnServer(spectator, oldSpectator);
                        mess.FailMessage = "Pilot already in game";
                    }
                }
                else
                {
                    var oldSpectator = DataEngine.Spectators.FirstOrDefault(spec =>
                        !spec.GetStats().IsLoggedIn && spec.IPAddress.Equals(spectator.IPAddress) && spec.Name == spectator.Name);
                    if (oldSpectator == null)
                    {
                        AddRemoteSpectator(spectator);
                        mess.SpectatorID = spectator.ID;
                    }
                    else if (oldSpectator.IsDisconnected)
                    {
                        ReconnectRemoteSpectatorOnServer(spectator, oldSpectator);
                        mess.SpectatorID = oldSpectator.ID;
                    }
                    else
                    {
                        AddRemoteSpectator(spectator);
                        mess.SpectatorID = spectator.ID;
                    }
                }
                NetworkEngine.GetConnection(spectator.ConnectionID).Send(mess);
                return true;
            });
        }

        private void SendServerStateToStats()
        {
            if (NetworkMode != Core.NetworkMode.Server || Stats.BasicInfoSent || DataEngine.Arena == null) return;
            Stats.Send(new { Server = Settings.Net.GameServerName, PID = GameServerGUID });
            var instanceKeyString = MiscHelper.BytesToString(new ArraySegment<byte>(NetworkEngine.GetAssaultWingInstanceKey()));
            var arenaUID = string.Format("{0}:{1}", instanceKeyString, DataEngine.Arena.StartTime);
            Stats.Send(new
            {
                Arena = new
                {
                    Name = DataEngine.Arena.Info.Name.Value,
                    Size = DataEngine.Arena.Info.Dimensions,
                    ID = arenaUID,
                },
                Players = DataEngine.Spectators.Select(Stats.GetStatsObject),
            });
            Stats.BasicInfoSent = true;
        }
    }
}
