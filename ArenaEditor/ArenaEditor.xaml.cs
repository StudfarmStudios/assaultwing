using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using AW2.Core;
using AW2.Game;
using AW2.Game.Gobs;
using AW2.Graphics;
using AW2.Helpers;
using AW2.Helpers.Serialization;
using AW2.UI;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;
using AW2.Game.Arenas;

namespace AW2
{
    /// <summary>
    /// Assault Wing Arena Editor main window.
    /// </summary>
    public partial class ArenaEditor : System.Windows.Window
    {
        private class GobReference
        {
            public Gob Value { get; set; }
            public override string ToString()
            {
                return string.Format("{0}", Value.TypeName);
            }
        }

        private class LayerReference
        {
            public ArenaLayer Value { get; set; }
            public override string ToString()
            {
                return string.Format("z={0:f0} {1}{2}", Value.Z, Value.IsGameplayLayer ? "(G) " : "", Value.ParallaxName);
            }
        }

        private GraphicsDeviceService _graphicsDeviceService;
        private AssaultWingCore _game;
        private AWGameRunner _runner;
        private System.Windows.Forms.MouseButtons _mouseButtons;
        private Point _lastMouseLocation, _dragStartLocation;
        private bool _isDragging;
        private object _properContent;

        /// <summary>
        /// Must be set right after creation.
        /// </summary>
        private EditorSpectator Spectator { get { return (EditorSpectator)_game.DataEngine.Spectators.First(); } }
        private double ZoomRatio { get { return Math.Pow(0.5, ZoomSlider.Value); } }
        private ArenaLayer SelectedLayer { get { return (ArenaLayer)LayerNames.SelectedValue; } }
        private Gob SelectedGob { get { return (Gob)GobNames.SelectedValue; } }
        private IEnumerable<EditorViewport> EditorViewports
        {
            get { return _game.DataEngine.Viewports.Where(vp => vp is EditorViewport).Cast<EditorViewport>(); }
        }

        public ArenaEditor(string[] args)
        {
            InitializeComponent();
            SetWaitContent();
            Loaded += (sender, eventArgs) =>
            {
                // GraphicsDeviceService needs a window handle which is only available after the window is visible
                InitializeGraphicsDeviceService();
                InitializeGame(args);
                InitializeArenaView();
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
            var oldCursor = ArenaEditorWindow.Cursor;
            try
            {
                ArenaEditorWindow.Cursor = Cursors.Wait;
                var data = _game.DataEngine;
                data.ProgressBar.Task = () => data.InitializeFromArena(arenaFilename, false);
                data.ProgressBar.StartTask();
                while (!data.ProgressBar.TaskCompleted) System.Threading.Thread.Sleep(100);
                data.ProgressBar.FinishTask();
                _game.StartArena();
                UpdateControlsFromArena();
                ApplyViewSettingsToAllViewports();
            }
            finally
            {
                ArenaEditorWindow.Cursor = oldCursor;
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
                InitialDirectory = Environment.CurrentDirectory,
                Title = "Open an Existing Arena for Editing",
            };
            bool? success = fileDialog.ShowDialog();
            if (success.HasValue && !success.Value) return;
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
                InitialDirectory = Environment.CurrentDirectory,
                Title = "Save the Arena to File",
                FileName = arena.Info.Name + ".xml",
            };
            bool? success = fileDialog.ShowDialog();
            if (success.HasValue && !success.Value) return;
            var oldCursor = ArenaEditorWindow.Cursor;
            try
            {
                ArenaEditorWindow.Cursor = Cursors.Wait;
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
                ArenaEditorWindow.Cursor = oldCursor;
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
                    GobNames.Items.Clear();
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

        private void GobRotation_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SelectedGob == null) return;
            SelectedGob.Rotation = (float)e.NewValue;
        }

        private void Zoom_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            double screenScale = ZoomRatio > 1 ? ZoomRatio : 1;
            double originalScale = ZoomRatio > 1 ? 1 : 1 / ZoomRatio;
            ZoomValue.Content = string.Format("{0:N0}:{1:N0}", screenScale, originalScale);
            ApplyViewSettingsToAllViewports();
        }

        private void LayerNames_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            GobNames.Items.Clear();
        }

        private void GobNames_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            foreach (GobReference gob in e.RemovedItems) gob.Value.BleachValue = 0;
            foreach (GobReference gob in e.AddedItems) gob.Value.BleachValue = 0.35f;
            if (SelectedGob == null) return;
            float newValue = SelectedGob.Rotation % MathHelper.TwoPi;
            if (newValue < 0) newValue += MathHelper.TwoPi;
            gobRotation.Value = newValue;
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
        }

        #endregion Control event handlers

        #region Helpers

        private void InitializeGraphicsDeviceService()
        {
            var windowHandle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            _graphicsDeviceService = new GraphicsDeviceService(windowHandle);
        }

        private void InitializeGame(string[] args)
        {
            _game = new AssaultWingCore(_graphicsDeviceService);
            _game.Window = new Core.Window(
                () => Title,
                text => Dispatcher.Invoke((Action)(() => Title = text)),
                () => new Rectangle(ArenaView.Bounds.X, ArenaView.Bounds.Y, ArenaView.Bounds.Width, ArenaView.Bounds.Height));
            AssaultWingCore.Instance = _game; // HACK: support oldschool singleton usage
            _game.CommandLineOptions = new CommandLineOptions(args);
            _game.SoundEngine.Enabled = false;
            _game.AllowDialogs = false;
            _graphicsDeviceService.DeviceResetting += (sender2, eventArgs2) => _game.UnloadContent();
            _graphicsDeviceService.DeviceReset += (sender2, eventArgs2) => _game.LoadContent();

            // Spectators/players can be initialized not until RunBegan because their AWViewports try to LoadContent.
            _game.RunBegan += () =>
            {
                _game.DataEngine.Spectators.Clear();
                var spectatorControls = new PlayerControls
                {
                    Thrust = new KeyboardKey(Keys.Up),
                    Left = new KeyboardKey(Keys.Left),
                    Right = new KeyboardKey(Keys.Right),
                    Down = new KeyboardKey(Keys.Down),
                    Fire1 = new KeyboardKey(Keys.RightControl),
                    Fire2 = new KeyboardKey(Keys.RightShift),
                    Extra = new KeyboardKey(Keys.Enter)
                };
                var spectator = new EditorSpectator(_game, spectatorControls);
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
                () => Dispatcher.BeginInvoke((Action)ArenaView.Invalidate),
                gameTime => Dispatcher.BeginInvoke((Action)(() => _game.Update(gameTime))));
            _runner.Initialized += () => Dispatcher.BeginInvoke((Action)RestoreProperContent);
            _runner.Run();
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
            if (SelectedLayer == null) return;
            var ray = viewport.ToRay(pointInViewport, SelectedLayer.Z);
            foreach (var gob in SelectedLayer.Gobs)
            {
                float distance = Vector2.Distance(gob.Pos, viewport.ToPos(pointInViewport, SelectedLayer.Z));
                float? t = gob.DrawBounds.Intersects(ray);
                if (distance < 20 || t.HasValue)
                    GobNames.Items.Add(new GobReference { Value = gob });
            }
            if (GobNames.Items.Count == 1) GobNames.SelectedIndex = 0;
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
                LayerNames.Items.Add(new LayerReference { Value = layer });
        }

        private void ApplyViewSettingsToAllViewports()
        {
            if (_game == null) return;
            var arena = _game.DataEngine.Arena;
            if (arena != null && EnableFog.IsChecked.HasValue)
                arena.IsFogOverrideDisabled = !EnableFog.IsChecked.Value;
            foreach (var viewport in EditorViewports) ApplyViewSettings(viewport);
        }

        private void ApplyViewSettings(EditorViewport viewport)
        {
            viewport.ZoomRatio = (float)ZoomRatio;
            if (CircleGobs.IsChecked.HasValue)
                viewport.IsCirclingSmallAndInvisibleGobs = CircleGobs.IsChecked.Value;
        }

        #endregion Helpers
    }

    static class ArenaEditorHelpers
    {
        public static Vector2 ToVector2(this Point point)
        {
            return new Vector2(point.X, point.Y);
        }

        public static Vector2 MouseMoveToWorldCoordinates(this AWViewport viewport, Point oldMouseLocation, Point newMouseLocation, float z)
        {
            var oldPos = viewport.ToPos(oldMouseLocation.ToVector2(), z);
            var nowPos = viewport.ToPos(newMouseLocation.ToVector2(), z);
            return nowPos - oldPos;
        }
    }
}
