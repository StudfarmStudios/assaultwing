﻿using System;
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

namespace AW2
{
    /// <summary>
    /// Assault Wing Arena Editor main window.
    /// </summary>
    public partial class ArenaEditor : Window
    {
        Spectator spectator;
        System.Windows.Forms.MouseButtons mouseButtons;
        Point lastMouseLocation, dragStartLocation;
        bool isDragging;

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
            spectator = new Spectator(spectatorControls);
            AssaultWing.Instance.DataEngine.Spectators.Add(spectator);
        }

        private void LoadArenaClick(object sender, RoutedEventArgs e)
        {
            Initialize(); // TODO: Move Initialize() to be called once after AssaultWing has started running

            // Load the selected arena.
            var arenaToLoad = arenaName.Text;
            if (arenaToLoad == "") return;
            var data = AssaultWing.Instance.DataEngine;
            data.ProgressBar.Task = () => data.InitializeFromArena(arenaToLoad, true);
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
                    foreach (var layer in data.Arena.Layers)
                    {
                        var ray = viewport.ToRay(pointInViewport, layer.Z);
                        foreach (var gob in layer.Gobs)
                        {
                            float distance = Vector2.Distance(gob.Pos, viewport.ToPos(pointInViewport, layer.Z));
                            float? t = gob.DrawBounds.Intersects(ray);
                            if (distance < 20 || t.HasValue)
                                gobNames.Items.Add(string.Format("z={0} {1}", layer.Z, gob.TypeName));
                        }
                    }
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
                // Right button drag moves view
                if ((mouseButtons & System.Windows.Forms.MouseButtons.Right) != 0)
                {
                    var viewport = GetViewport(dragStartLocation);
                    var oldPos = viewport.ToPos(lastMouseLocation.ToVector2(), 0);
                    var nowPos = viewport.ToPos(e.Location.ToVector2(), 0);
                    spectator.LookAt.Position -= nowPos - oldPos;
                }
                lastMouseLocation = e.Location;
            }
            catch (Exception ex)
            {
                Log.Write("NOTE. Exception during mouse move: " + ex.ToString());
            }
        }

        /// <summary>
        /// Returns the viewport that contains a point on the arena view.
        /// </summary>
        /// The coordinates are in arena view coordinates, origin at the top left corner,
        /// positive X pointing right and positive Y pointing down.
        AW2.Graphics.AWViewport GetViewport(Point location)
        {
            var data = AssaultWing.Instance.DataEngine;
            AW2.Graphics.AWViewport result = null;
            foreach (var layer in data.Arena.Layers)
                data.ForEachViewport(viewport =>
                {
                    if (viewport.OnScreen.Contains(location.X, location.Y))
                        result = viewport;
                });
            if (result == null)
                throw new ArgumentException("There is no viewport at render target surface coordinates " + location.ToString());
            return result;
        }
    }

    static class ArenaEditorHelpers
    {
        public static Vector2 ToVector2(this Point point)
        {
            return new Vector2(point.X, point.Y);
        }
    }
}
