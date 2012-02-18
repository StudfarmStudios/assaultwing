using System;
using System.Activities.Presentation.PropertyEditing;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using AW2.Core;
using AW2.Game;
using AW2.Game.Arenas;
using AW2.Game.Gobs;
using AW2.Graphics;
using AW2.Helpers;
using AW2.Helpers.Serialization;
using AW2.UI.WPF.PropertyValueEditors;
using Point = System.Drawing.Point;

namespace AW2.UI.WPF
{
    /// <summary>
    /// Assault Wing Arena Editor main window.
    /// </summary>
    public partial class ArenaEditorWindow : System.Windows.Window
    {
        private class LayerReference
        {
            public ArenaLayer Value { get; set; }
            public bool Visible { get; set; }

            public LayerReference(ArenaLayer value)
            {
                Value = value;
                Visible = true;
            }
        }

        private GraphicsDeviceService _graphicsDeviceService;
        private AssaultWingCore _game;
        private AWGameRunner _runner;
        private System.Windows.Forms.MouseButtons _mouseButtons;
        private Point _lastMouseLocation, _dragStartLocation;
        private bool _isDragging;
        private Gob _selectedGob;
        private GobSelectionPopup _gobSelector;
        private object _properContent;

        public Gob SelectedGob
        {
            get { return _selectedGob; }
            set
            {
                if (_selectedGob != null) _selectedGob.BleachValue = 0;
                _selectedGob = value;
                PropertyEditor.SelectedObject = _selectedGob;
                if (_selectedGob != null) _selectedGob.BleachValue = 0.35f;
            }
        }

        private EditorSpectator Spectator { get { return (EditorSpectator)_game.DataEngine.Spectators.First(); } }
        private double ZoomRatio { get { return Math.Pow(0.5, ZoomSlider.Value); } }
        private ArenaLayer SelectedLayer { get { return (ArenaLayer)LayerNames.SelectedValue; } }
        private bool IsGobSelectorVisible { get { return _gobSelector != null && _gobSelector.IsLoaded; } }
        private IEnumerable<EditorViewport> EditorViewports
        {
            get { return _game.DataEngine.Viewports.OfType<EditorViewport>(); }
        }

        public ArenaEditorWindow(string[] args)
        {
            InitializeComponent();
            SetWaitContent();
            AW2.Game.GobUtils.GobPropertyDescriptor.GetPropertyAttributes = GetPropertyAttributes;
            Loaded += (sender, eventArgs) =>
            {
                // GraphicsDeviceService needs a window handle which is only available after the window is visible
                InitializeGraphicsDeviceService();
                InitializeGame(args, AssaultWingCore.GetArgumentText());
                InitializeArenaView();
                AW2.Game.Spectator.CreateStatsData = () => new MockStats();
            };
            Closed += (sender, eventArgs) => _runner.Exit();
        }

        public void Dispose()
        {
            if (_game != null)
            {
                _game.Dispose();
                _game = null;
            }
        }

        public void LoadArena(string arenaFilename)
        {
            var oldCursor = Cursor;
            try
            {
                Cursor = Cursors.Wait;
                var arena = Arena.FromFile(_game, arenaFilename);
                _game.LoadArenaContent(arena);
                arena.Reset();
                _game.DataEngine.Arena = arena;
                _game.StartArena();
                UpdateControlsFromArena();
                ApplyViewSettingsToAllViewports();
            }
            finally
            {
                Cursor = oldCursor;
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            _runner.Exit();
            DoEvents(); // finish processing BeginInvoke()d Update() and Draw() calls
            base.OnClosing(e);
        }

        #region Control event handlers

        private void LoadArena_Click(object sender, RoutedEventArgs e)
        {
            var fileDialog = new OpenFileDialog
            {
                CheckFileExists = true,
                DefaultExt = ".xml",
                Filter = "Assault Wing Arenas (*.xml)|*.xml|All Files (*.*)|*.*",
                InitialDirectory = _game.Settings.System.ArenaEditorDefaultDirectory,
                Title = "Open an Existing Arena for Editing",
            };
            bool? success = fileDialog.ShowDialog();
            if (success.HasValue && !success.Value) return;
            _game.Settings.System.ArenaEditorDefaultDirectory = System.IO.Path.GetDirectoryName(fileDialog.FileName);
            _game.Settings.ToFile();
            LoadArena(fileDialog.FileName);
        }

        private void SaveArena_Click(object sender, RoutedEventArgs e)
        {
            var arena = _game.DataEngine.Arena;
            if (arena == null) return;
            arena.Info.Name = (CanonicalString)ArenaName.Text;
            var fileDialog = new SaveFileDialog
            {
                DefaultExt = ".xml",
                Filter = "Assault Wing Arenas (*.xml)|*.xml|All Files (*.*)|*.*",
                InitialDirectory = _game.Settings.System.ArenaEditorDefaultDirectory,
                Title = "Save the Arena to File",
                FileName = arena.Info.Name + ".xml",
            };
            bool? success = fileDialog.ShowDialog();
            if (success.HasValue && !success.Value) return;
            _game.Settings.System.ArenaEditorDefaultDirectory = System.IO.Path.GetDirectoryName(fileDialog.FileName);
            _game.Settings.ToFile();
            var oldCursor = Cursor;
            try
            {
                Cursor = Cursors.Wait;
                UpdateArenaBin(arena);
                var arenaFilename = fileDialog.FileName;
                var binFilename = System.IO.Path.ChangeExtension(arenaFilename, ".bin");
                var arenaPath = System.IO.Path.Combine(Paths.ARENAS, arenaFilename);
                var binPath = System.IO.Path.Combine(Paths.ARENAS, binFilename);
                arena.BinFilename = System.IO.Path.GetFileName(binPath);
                arena.Bin.Save(binPath);
                TypeLoader.SaveTemplate(arena, arenaPath, typeof(Arena), typeof(TypeParameterAttribute));
            }
            finally
            {
                Cursor = oldCursor;
            }
        }

        private void ArenaView_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            _mouseButtons &= ~e.Button;
            if (_mouseButtons == System.Windows.Forms.MouseButtons.None)
                _isDragging = false;
        }

        private void ArenaView_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            _mouseButtons |= e.Button;
            _dragStartLocation = e.Location;
        }

        private void ArenaView_MouseEnterOrLeave(object sender, EventArgs e)
        {
            _mouseButtons = System.Windows.Forms.MouseButtons.None;
        }

        private void ArenaView_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            try
            {
                // Left mouse button click selects gobs.
                if (!_isDragging && e.Button == System.Windows.Forms.MouseButtons.Left)
                {
                    if (_game.DataEngine.Arena == null) return;
                    var pointInViewport = new Vector2(e.Location.X, e.Location.Y);
                    var viewport = GetViewport(e.Location);
                    if (viewport != null) ClickViewport(viewport, pointInViewport);
                }
            }
            catch (Exception ex)
            {
                Log.Write("NOTE. Exception during mouse click: " + ex.ToString());
            }
        }

        private void ArenaView_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            try
            {
                e.Handled = true;
                if (e.Delta > 0) ZoomSlider.Value -= ZoomSlider.TickFrequency;
                if (e.Delta < 0) ZoomSlider.Value += ZoomSlider.TickFrequency;
            }
            catch (Exception ex)
            {
                Log.Write("NOTE. Exception during mouse wheeling: " + ex.ToString());
            }
        }

        private void ArenaView_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (_mouseButtons != System.Windows.Forms.MouseButtons.None)
                _isDragging = true;
            try
            {
                var cursorCoordinateText = "X:??? Y:???";
                var viewport = GetViewport(_dragStartLocation);
                if (viewport != null)
                {
                    var mouseCoordinates = viewport.ToPos(e.Location.ToVector2(), 0);
                    cursorCoordinateText = string.Format("X:{0:N1} Y:{1:N1}", mouseCoordinates.X, mouseCoordinates.Y);
                    DragViewport(viewport, e.Location);
                }
                CursorCoordinates.Text = cursorCoordinateText;
            }
            catch (Exception ex)
            {
                Log.Write("NOTE. Exception during mouse move: " + ex.ToString());
            }
        }

        private void Zoom_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            double screenScale = ZoomRatio > 1 ? ZoomRatio : 1;
            double originalScale = ZoomRatio > 1 ? 1 : 1 / ZoomRatio;
            ZoomValue.Content = string.Format("{0:N0}:{1:N0}", screenScale, originalScale);
            ApplyViewSettingsToAllViewports();
        }

        private void DuplicateGob_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedGob == null) return;
            var duplicate = (Gob)SelectedGob.CloneWithRuntimeState();
            duplicate.Game = _game;
            duplicate.Layer = SelectedGob.Layer;
            duplicate.Pos += new Vector2(50, 50);
            SelectedGob.Arena.Gobs.Add(duplicate);
        }

        private void DeleteGob_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedGob == null) return;
            SelectedGob.Arena.Gobs.Remove(SelectedGob);
            SelectedGob = null;
        }

        #endregion Control event handlers

        #region Helpers

        private static IEnumerable<Attribute> GetPropertyAttributes(System.Reflection.FieldInfo field)
        {
            if (field.Name.ToLower().Contains("rotation")) yield return new EditorAttribute(typeof(RotationEditor), typeof(PropertyValueEditor));
            if (field.FieldType == typeof(Vector2)) yield return new EditorAttribute(typeof(Vector2Editor), typeof(PropertyValueEditor));
        }

        private void InitializeGraphicsDeviceService()
        {
            var windowHandle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            _graphicsDeviceService = new GraphicsDeviceService(windowHandle);
        }

        private void InitializeGame(string[] args, string argumentText)
        {
            _game = new AssaultWingCore(_graphicsDeviceService, new CommandLineOptions(args, MiscHelper.QueryParams, argumentText));
            _game.Window = new Core.Window(new Core.Window.WindowImpl
            {
                GetTitle = () => Title,
                SetTitle = text => Dispatcher.Invoke((Action)(() => Title = text)),
                GetClientBounds = () => new Rectangle(ArenaView.Bounds.X, ArenaView.Bounds.Y, ArenaView.Bounds.Width, ArenaView.Bounds.Height),
                GetFullScreen = () => false,
                SetWindowed = () => { },
                SetFullScreen = (width, height) => { },
                IsVerticalSynced = () => false,
                EnableVerticalSync = () => { },
                DisableVerticalSync = () => { },
                EnsureCursorHidden = () => { },
                EnsureCursorShown = () => { },
            });
            AssaultWingCore.Instance = _game; // HACK: support oldschool singleton usage
            _game.SoundEngine.Enabled = false;
            _game.AllowDialogs = false;

            // Spectators/players can be initialized not until RunBegan because their AWViewports try to LoadContent.
            _game.RunBegan += () =>
            {
                _game.DataEngine.Spectators.Clear();
                var spectator = new EditorSpectator(_game);
                spectator.ViewportCreated += viewport =>
                    viewport.LayerDrawing += layer =>
                        LayerNames.Items.Cast<LayerReference>().First(lay => lay.Value == layer).Visible;
                _game.DataEngine.Spectators.Add(spectator);
                _game.DataEngine.Enabled = true;
                _game.GraphicsEngine.Enabled = true;
                _game.GraphicsEngine.Visible = true;
            };
        }

        private void InitializeArenaView()
        {
            ArenaView.ClientSizeChanged += (sender, eventArgs) => _game.DataEngine.RearrangeViewports();
            ArenaView.Draw += _game.Draw;
            ArenaView.GraphicsDeviceService = _graphicsDeviceService;
            ArenaView.ClientSize = ArenaView.ClientSize; // trigger ArenaView.ClientSizeChanged to react to the initial ArenaView size
            _runner = new AWGameRunner(_game,
                invoker: action => Dispatcher.Invoke(action),
                exceptionHandler: e => Dispatcher.BeginInvoke((Action)(() => { throw new ApplicationException("An exception occurred in a background thread", e); })),
                draw: () => Dispatcher.BeginInvoke((Action)ArenaView.Invalidate),
                update: gameTime => Dispatcher.BeginInvoke((Action<AWGameTime>)_game.Update, gameTime));
            _runner.Initialized += () => Dispatcher.BeginInvoke((Action)GameInitializedHandler);
            _runner.Run();
        }

        private void GameInitializedHandler()
        {
            RestoreProperContent();
            if (_game.CommandLineOptions.ArenaFilename != null)
                LoadArena(_game.CommandLineOptions.ArenaFilename);
        }

        private void SetWaitContent()
        {
            _properContent = Content;
            Content = new Label
            {
                Content = "Please wait while initializing...",
                FontSize = 22,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
            };
        }

        private void RestoreProperContent()
        {
            Content = _properContent;
            _properContent = null;
        }

        private static void UpdateArenaBin(Arena arena)
        {
            arena.Bin.Clear();
            foreach (var gob in arena.Gobs.GameplayLayer.Gobs)
                if (gob is Wall)
                {
                    gob.StaticID = gob.ID;
                    arena.Bin.Add(gob.StaticID, ((Wall)gob).CreateIndexMap());
                }
        }

        private void ApplyViewSettings(object sender, RoutedEventArgs e)
        {
            ApplyViewSettingsToAllViewports();
        }

        private void DoEvents()
        {
            // Invoke() returns after all pending events are processed.
            Dispatcher.Invoke((Action)(() => { }));
        }

        private void ClickViewport(AWViewport viewport, Vector2 pointInViewport)
        {
            var potentialGobs = LayerNames.Items
                .Cast<LayerReference>()
                .Where(layref => layref.Visible)
                .Reverse()
                .SelectMany(lay => FindGobs(viewport, pointInViewport, lay.Value));
            if (potentialGobs.Count() > 1) EnsureGobSelectorVisible();
            if (!IsGobSelectorVisible)
                SelectedGob = potentialGobs.FirstOrDefault();
            else
                _gobSelector.SetGobs(potentialGobs);
        }

        private IEnumerable<Gob> FindGobs(AWViewport viewport, Vector2 pointInViewport, ArenaLayer layer)
        {
            var ray = viewport.ToRay(pointInViewport, layer.Z);
            return
                from gob in layer.Gobs
                let distance = Vector2.Distance(gob.Pos, viewport.ToPos(pointInViewport, layer.Z))
                let t = gob.DrawBounds.Intersects(ray)
                where distance < 20 || t.HasValue
                orderby distance ascending
                select gob;
        }

        private void DragViewport(AWViewport viewport, Point newMouseLocation)
        {
            // Left mouse button drag moves selected gob, except when also right mouse drag is active.
            if ((_mouseButtons & System.Windows.Forms.MouseButtons.Left) != 0 &&
                (_mouseButtons & System.Windows.Forms.MouseButtons.Right) == 0)
            {
                if (SelectedGob != null)
                {
                    var move = viewport.MouseMoveToWorldCoordinates(_lastMouseLocation, newMouseLocation, SelectedGob.Layer.Z);
                    SelectedGob.Pos += move;
                }
            }

            // Right mouse button drag moves view.
            if ((_mouseButtons & System.Windows.Forms.MouseButtons.Right) != 0)
            {
                var move = viewport.MouseMoveToWorldCoordinates(_lastMouseLocation, newMouseLocation, 0);
                Spectator.LookAtPos -= move;
            }

            _lastMouseLocation = newMouseLocation;
        }

        /// <summary>
        /// Returns the viewport that contains a point on the arena view.
        /// </summary>
        /// The coordinates are in arena view coordinates, origin at the top left corner,
        /// positive X pointing right and positive Y pointing down.
        private AWViewport GetViewport(Point location)
        {
            var viewports = _game.DataEngine.Viewports;
            return viewports.FirstOrDefault(vp => vp.OnScreen.Contains(location.X, location.Y));
        }

        private void UpdateControlsFromArena()
        {
            var arena = _game.DataEngine.Arena;
            ArenaName.Text = arena.Info.Name;

            // Put arena layers on display.
            LayerNames.Items.Clear();
            foreach (var layer in arena.Layers)
                LayerNames.Items.Add(new LayerReference(layer));
        }

        private void ApplyViewSettingsToAllViewports()
        {
            if (_game == null) return;
            var arena = _game.DataEngine.Arena;
            foreach (var viewport in EditorViewports) ApplyViewSettings(viewport);
        }

        private void ApplyViewSettings(EditorViewport viewport)
        {
            viewport.ZoomRatio = (float)ZoomRatio;
            if (CircleGobs.IsChecked.HasValue)
                viewport.IsCirclingSmallAndInvisibleGobs = CircleGobs.IsChecked.Value;
        }

        private void EnsureGobSelectorVisible()
        {
            if (IsGobSelectorVisible) return;
            _gobSelector = new GobSelectionPopup { Owner = this };
            _gobSelector.GobList.SelectionChanged += (sender, args) => SelectedGob = args.AddedItems.Cast<Gob>().FirstOrDefault();
            _gobSelector.Show();
        }

        #endregion Helpers
    }
}
