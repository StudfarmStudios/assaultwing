using System;
using Microsoft.Xna.Framework;
using AW2.Helpers;
using AW2.Helpers.Serialization;

namespace AW2.Game
{
    /// <summary>
    /// A text message sent to a player.
    /// <see cref="ChatBoxOverlay"/> draws PlayerMessages in a player viewport.
    /// <see cref="PlayerMessageMessage"/> transports PlayerMessages between different game instances.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("{PreText} {Text}")]
    public class PlayerMessage : INetworkSerializable
    {
        public string PreText { get; private set; }
        public string Text { get; private set; }
        public Color TextColor { get; private set; }

        /// <summary>
        /// Only for deserialisation.
        /// </summary>
        public PlayerMessage()
            : this("", "", Color.Red)
        {
        }

        public PlayerMessage(string text, Color textColor)
            : this("", text, textColor)
        {
        }

        public PlayerMessage(string preText, string text, Color textColor)
        {
            if (preText == null || text == null) throw new ArgumentNullException("Null message");
            text = text.Replace("\n", " ");
            text = text.Capitalize();
            PreText = preText;
            Text = text;
            TextColor = textColor;
        }

        public void Serialize(NetworkBinaryWriter writer, SerializationModeFlags mode)
        {
            writer.Write((string)PreText);
            writer.Write((string)Text);
            writer.Write((Color)TextColor);
        }

        public void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode, int framesAgo)
        {
            PreText = reader.ReadString();
            Text = reader.ReadString();
            TextColor = reader.ReadColor();
        }

        public override string ToString()
        {
            return PreText + Text;
        }
    }
}
