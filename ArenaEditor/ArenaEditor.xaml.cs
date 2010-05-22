﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using AW2.Game;
using AW2.Game.Gobs;
using AW2.Graphics;
using AW2.Helpers;
using AW2.UI;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;
using System.IO;
using System.IO.Compression;

namespace AW2
{
    /// <summary>
    /// Assault Wing Arena Editor main window.
    /// </summary>
    public partial class ArenaEditor : Window
    {
        private class GobReference
        {
            public Gob Value { get; set; }
            public int LayerIndex { get; set; }
            public override string ToString()
            {
                return string.Format("#{0} {1}", LayerIndex, Value.TypeName);
            }
        }

        private System.Windows.Forms.MouseButtons _mouseButtons;
        private Point _lastMouseLocation, _dragStartLocation;
        private bool _isDragging;

        private EditorSpectator Spectator { get { return (EditorSpectator)AssaultWing.Instance.DataEngine.Spectators.First(); } }
        private double ZoomRatio { get { return Math.Pow(0.5, ZoomSlider.Value); } }
        private Gob SelectedGob { get { return (Gob)GobNames.SelectedValue; } }

        public ArenaEditor()
        {
            InitializeComponent();
        }

        #region Control event handlers

        private void LoadArena_Click(object sender, RoutedEventArgs e)
        {
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

            ApplyViewSettingsToAllViewports();
        }

        private void SaveArena_Click(object sender, RoutedEventArgs e)
        {
            var arena = AssaultWing.Instance.DataEngine.Arena;
            if (arena == null) return;
            if (!arena.Name.EndsWith("_edited")) arena.Name += "_edited";
            var filename = TypeLoader.GetFilename(arena, arena.Name);
            var path = System.IO.Path.Combine(Paths.Arenas, filename);
            var binPath = System.IO.Path.ChangeExtension(path, ".bin");
            SaveArenaBinaryData(arena, binPath);
            TypeLoader.SaveTemplate(arena, path, typeof(Arena), typeof(TypeParameterAttribute));
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
                    if (AssaultWing.Instance.DataEngine.Arena == null) return;
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
            duplicate.Layer = SelectedGob.Layer;
            duplicate.Pos += new Vector2( 50, 50 );
            SelectedGob.Arena.Gobs.Add(duplicate);
        }

        private void DeleteGob_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedGob == null) return;
            SelectedGob.Arena.Gobs.Remove(SelectedGob);
        }

        private void ApplyViewSettings(object sender, RoutedEventArgs e)
        {
            ApplyViewSettingsToAllViewports();
        }

        #endregion Control event handlers

        #region Helpers

        private void ClickViewport(AWViewport viewport, Vector2 pointInViewport)
        {
            int layerIndex = 0;
            foreach (var layer in AssaultWing.Instance.DataEngine.Arena.Layers)
            {
                var ray = viewport.ToRay(pointInViewport, layer.Z);
                foreach (var gob in layer.Gobs)
                {
                    float distance = Vector2.Distance(gob.Pos, viewport.ToPos(pointInViewport, layer.Z));
                    float? t = gob.DrawBounds.Intersects(ray);
                    if (distance < 20 || t.HasValue)
                        GobNames.Items.Add(new GobReference { Value = gob, LayerIndex = layerIndex });
                }
                ++layerIndex;
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
                Spectator.LookAt.Position -= move;
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
            var viewports = AssaultWing.Instance.DataEngine.Viewports;
            return viewports.FirstOrDefault(vp => vp.OnScreen.Contains(location.X, location.Y));
        }

        private void ForEachEditorViewport(Action<EditorViewport> action)
        {
            foreach (var viewport in AssaultWing.Instance.DataEngine.Viewports)
                if (viewport is EditorViewport)
                    action((EditorViewport)viewport);
        }

        private void ApplyViewSettingsToAllViewports()
        {
            var arena = AssaultWing.Instance.DataEngine.Arena;
            if (arena != null && EnableFog.IsChecked.HasValue)
                arena.IsFogOverrideDisabled = !EnableFog.IsChecked.Value;
            ForEachEditorViewport(viewport => ApplyViewSettings(viewport));
        }

        private void ApplyViewSettings(EditorViewport viewport)
        {
            viewport.ZoomRatio = (float)ZoomRatio;
            if (CircleGobs.IsChecked.HasValue)
                viewport.IsCirclingSmallAndInvisibleGobs = CircleGobs.IsChecked.Value;
        }

        private static void SaveArenaBinaryData(Arena arena, string path)
        {
            // Create file and write header without compression
            var file = File.Open(path, FileMode.Create);
            var rawWriter = new BinaryWriter(file);
            rawWriter.Write(System.Text.Encoding.ASCII.GetBytes("AWBIN100"));
            rawWriter.Close();

            // Append contents to the file with compression
            file = File.Open(path, FileMode.Append);
            var packWriter = new BinaryWriter(new DeflateStream(file, CompressionMode.Compress));
            foreach (var gob in arena.Gobs.GameplayLayer.Gobs)
            {
                if (!(gob is Wall)) continue;
                checked
                {
                    gob.StaticID = gob.Id;
                    packWriter.Write((int)gob.StaticID);
                    var buffer = new MemoryStream();
                    var bufferWriter = new BinaryWriter(buffer);
                    ((Wall)gob).CreateIndexMap().Serialize(bufferWriter);
                    int bufferLength = (int)buffer.Length;
                    bufferWriter.Close();
                    packWriter.Write(bufferLength);
                    packWriter.Write(buffer.GetBuffer(), 0, bufferLength);
                }
            }
            packWriter.Close();
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
