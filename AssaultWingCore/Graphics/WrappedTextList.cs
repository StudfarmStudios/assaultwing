using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Xna.Framework;
using AW2.Core;
using AW2.Game;
using AW2.Game.Players;

namespace AW2.Graphics
{
    /// <summary>
    /// Collection of lines from <see cref="PlayerMessage"/> objects. Caches the
    /// wrapped lines for all queried text widths.
    /// </summary>
    public class WrappedTextList
    {
        [System.Diagnostics.DebuggerDisplay("{Text}")]
        public class Line
        {
            public string Text { get; private set; }
            public Color Color { get; private set; }
            public bool ContainsPretext { get; private set; }

            public Line(string text, Color color, bool containsPretext)
            {
                Text = text;
                Color = color;
                ContainsPretext = containsPretext;
            }
        }

        private const int LINE_KEEP_COUNT = 200;

        private Dictionary<float, List<Line>> _items;
        private Player _previousChatPlayer;
        private AssaultWingCore _game;

        public ReadOnlyCollection<Line> this[float textWidth]
        {
            get
            {
                CheckIfChatPlayerChanged();
                if (!_items.ContainsKey(textWidth)) AddCachedTextWidth(textWidth);
                return _items[textWidth].AsReadOnly();
            }
        }

        private Player ChatPlayer { get { return _game.DataEngine.LocalPlayer; } }

        public WrappedTextList(AssaultWingCore game)
        {
            _game = game;
            _items = new Dictionary<float, List<Line>>();
        }

        private void AddCachedTextWidth(float textWidth)
        {
            if (_items.Keys.Count > 100) throw new NotImplementedException("WrappedTextList purging outdated text widths");
            _items[textWidth] = new List<Line>();
            foreach (var mess in ChatPlayer.Messages.ChatItems) AddMessage(mess.Message, textWidth);
        }

        private void AddMessage(PlayerMessage message)
        {
            foreach (var textWidth in _items.Keys) AddMessage(message, textWidth);
        }

        private void AddMessage(PlayerMessage message, float textWidth)
        {
            var lines = _items[textWidth];
            lines.AddRange(GetMessageLines(message, textWidth));
            if (lines.Count >= LINE_KEEP_COUNT * 2) lines.RemoveRange(0, lines.Count - LINE_KEEP_COUNT);
        }

        private IEnumerable<Line> GetMessageLines(PlayerMessage message, float textWidth)
        {
            var fullText = message.PreText.Length > 0 ? message.PreText + " " + message.Text : message.Text;
            var lines = new WrappedText(fullText, GetMessageLineWidth).WrapToWidth(textWidth);
            if (lines.Length == 0) yield break;
            var lineContainsPretext = message.PreText.Length > 0;
            foreach (var line in lines)
            {
                yield return new Line(line, message.TextColor, lineContainsPretext);
                lineContainsPretext = false;
            }
        }

        private float GetMessageLineWidth(string line)
        {
            return _game.GraphicsEngine.GameContent.ChatFont.MeasureString(line).X;
        }

        private void CheckIfChatPlayerChanged()
        {
            if (ChatPlayer == _previousChatPlayer) return;
            if (_previousChatPlayer != null) _previousChatPlayer.Messages.NewChatMessage -= AddMessage;
            _items.Clear();
            ChatPlayer.Messages.NewChatMessage += AddMessage;
            _previousChatPlayer = ChatPlayer;
        }
    }
}
