using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using AW2.Game;
using AW2.Helpers;
using AW2.UI;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;
using System.Windows.Controls;
using AW2.Graphics;

namespace AW2
{
    /// <summary>
    /// Assault Wing Arena Editor main window.
    /// </summary>
    public partial class ArenaEditor : Window
    {
        class GobReference
        {
            public Gob Value { get; set; }
            public int LayerIndex { get; set; }
            public override string ToString()
            {
                return string.Format("#{0} {1}", LayerIndex, Value.TypeName);
            }
        }

        TypeLoader arenaSaver;
        EditorSpectator spectator;
        System.Windows.Forms.MouseButtons mouseButtons;
        Point lastMouseLocation, dragStartLocation;
        bool isDragging;

        private double ZoomRatio { get { return Math.Pow(0.5, ZoomSlider.Value); } }
        private Gob SelectedGob { get { return (Gob)gobNames.SelectedValue; } }

        public ArenaEditor()
        {
            InitializeComponent();
        }

        void Initialize()
        {
            AssaultWing.Instance.DataEngine.Spectators.Clear();
            var spectatorControls = new PlayerControls
            {
                thrust = new KeyboardKey(Keys.Up),
                left = new KeyboardKey(Keys.Left),
                right = new KeyboardKey(Keys.Right),
                down = new KeyboardKey(Keys.Down),
                fire1 = new KeyboardKey(Keys.RightControl),
                fire2 = new KeyboardKey(Keys.RightShift),
                extra = new KeyboardKey(Keys.Enter)
            };
            spectator = new EditorSpectator(spectatorControls);
            spectator.ViewportCreated += ApplyViewSettings;
            AssaultWing.Instance.DataEngine.Spectators.Add(spectator);
            arenaSaver = new TypeLoader(typeof(Arena), Paths.Arenas);
        }

        #region Control event handlers

        private void LoadArena_Click(object sender, RoutedEventArgs e)
        {
            Initialize(); // TODO: Move Initialize() to be called once after AssaultWing has started running

            // Load the selected arena.
            var arenaToLoad = arenaName.Text;
            if (arenaToLoad == "") return;
            var data = AssaultWing.Instance.DataEngine;
            data.ProgressBar.Task = () => data.InitializeFromArena(arenaToLoad, false);
            data.ProgressBar.StartTask();
            while (!data.ProgressBar.TaskCompleted) System.Threading.Thread.Sleep(100);
            data.ProgressBar.FinishTask();
            AssaultWing.Instance.StartArena();

            // Put arena layers on display.
            layerNames.Items.Clear();
            var layerNameList = data.Arena.Layers.Select((layer, index) =>
                string.Format("#{0} z={1:f0} {2}{3}", index, layer.Z, layer.IsGameplayLayer ? "(G) " : "", layer.ParallaxName));
            foreach (var layerName in layerNameList)
                layerNames.Items.Add(layerName);
        }

        private void SaveArena_Click(object sender, RoutedEventArgs e)
        {
            var arena = AssaultWing.Instance.DataEngine.Arena;
            if (!arena.Name.EndsWith("_edited")) arena.Name += "_edited";
            arenaSaver.SaveObject(arena, typeof(TypeParameterAttribute), arena.Name);
        }

        private void ArenaView_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            mouseButtons &= ~e.Button;
            if (mouseButtons == System.Windows.Forms.MouseButtons.None)
                isDragging = false;
        }

        private void ArenaView_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            mouseButtons = e.Button;
            dragStartLocation = e.Location;
        }

        private void ArenaView_MouseEnterOrLeave(object sender, EventArgs e)
        {
            mouseButtons = System.Windows.Forms.MouseButtons.None;
        }

        private void ArenaView_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            try
            {
                // Left mouse button click selects gobs.
                if (!isDragging && e.Button == System.Windows.Forms.MouseButtons.Left)
                {
                    var data = AssaultWing.Instance.DataEngine;
                    if (data.Arena == null) return;
                    gobNames.Items.Clear();
                    var pointInViewport = new Vector2(e.Location.X, e.Location.Y);
                    var viewport = GetViewport(e.Location);
                    int layerIndex = 0;
                    foreach (var layer in data.Arena.Layers)
                    {
                        var ray = viewport.ToRay(pointInViewport, layer.Z);
                        foreach (var gob in layer.Gobs)
                        {
                            float distance = Vector2.Distance(gob.Pos, viewport.ToPos(pointInViewport, layer.Z));
                            float? t = gob.DrawBounds.Intersects(ray);
                            if (distance < 20 || t.HasValue)
                                gobNames.Items.Add(new GobReference { Value = gob, LayerIndex = layerIndex });
                        }
                        ++layerIndex;
                    }
                    if (gobNames.Items.Count == 1) gobNames.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                Log.Write("NOTE. Exception during mouse click: " + ex.ToString());
            }

        }

        private void ArenaView_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (mouseButtons != System.Windows.Forms.MouseButtons.None)
                isDragging = true;
            try
            {
                var viewport = GetViewport(dragStartLocation);
                var mouseCoordinates = viewport.ToPos(e.Location.ToVector2(), 0);
                CursorCoordinates.Text = string.Format("X:{0} Y:{1}", mouseCoordinates.X, mouseCoordinates.Y);

                // Left mouse button drag moves selected gob.
                if ((mouseButtons & System.Windows.Forms.MouseButtons.Left) != 0)
                {
                    if (SelectedGob != null)
                    {
                        var move = viewport.MouseMoveToWorldCoordinates(lastMouseLocation, e.Location, SelectedGob.Layer.Z);
                        SelectedGob.Pos += move;
                    }
                }

                // Right mouse button drag moves view.
                if ((mouseButtons & System.Windows.Forms.MouseButtons.Right) != 0)
                {
                    var move = viewport.MouseMoveToWorldCoordinates(lastMouseLocation, e.Location, 0);
                    spectator.LookAt.Position -= move;
                }
                lastMouseLocation = e.Location;
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

        private void GobNames_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (SelectedGob == null) return;
            float newValue = SelectedGob.Rotation % MathHelper.TwoPi;
            if (newValue < 0) newValue += MathHelper.TwoPi;
            gobRotation.Value = newValue;
        }

        private void DuplicateGob_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedGob == null) return;
            var duplicate = (Gob)SelectedGob.CloneWithRuntimeState();
            duplicate.Layer = SelectedGob.Layer;
            duplicate.Pos += new Vector2( 50, 50 );
            SelectedGob.Arena.Gobs.Add(duplicate);
        }

        private void DeleteGob_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedGob == null) return;
            SelectedGob.Arena.Gobs.Remove(SelectedGob);
        }

        private void CircleGobs_Click(object sender, RoutedEventArgs e)
        {
            ApplyViewSettingsToAllViewports();
        }

        #endregion Control event handlers

        #region Helpers

        /// <summary>
        /// Returns the viewport that contains a point on the arena view.
        /// </summary>
        /// The coordinates are in arena view coordinates, origin at the top left corner,
        /// positive X pointing right and positive Y pointing down.
        private AWViewport GetViewport(Point location)
        {
            AWViewport result = null;
            AssaultWing.Instance.DataEngine.ForEachViewport(viewport =>
            {
                if (viewport.OnScreen.Contains(location.X, location.Y))
                    result = viewport;
            });
            if (result == null)
                throw new ArgumentException("There is no viewport at render target surface coordinates " + location.ToString());
            return result;
        }

        private void ForEachEditorViewport(Action<EditorViewport> action)
        {
            AssaultWing.Instance.DataEngine.ForEachViewport(viewport =>
            {
                if (viewport is EditorViewport)
                    action((EditorViewport)viewport);
            });
        }

        private void ApplyViewSettingsToAllViewports()
        {
            AssaultWing.Instance.DataEngine.ForEachViewport(viewport =>
            {
                if (viewport is EditorViewport) ApplyViewSettings((EditorViewport)viewport);
            });
        }

        private void ApplyViewSettings(EditorViewport viewport)
        {
            if (!CircleGobs.IsChecked.HasValue)
                viewport.IsCirclingSmallAndInvisibleGobs = CircleGobs.IsChecked.Value;
            viewport.ZoomRatio = (float)ZoomRatio;
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
