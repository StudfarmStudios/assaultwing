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
        public static readonly Color PRETEXT_COLOR = new Color(1f, 1f, 1f);
        public static readonly Color DEFAULT_COLOR = new Color(0.9f, 0.9f, 0.9f);
        public static readonly Color BONUS_COLOR = new Color(0.3f, 0.7f, 1f);
        public static readonly Color DEATH_COLOR = new Color(1f, 0.2f, 0.2f);
        public static readonly Color SUICIDE_COLOR = new Color(1f, 0.5f, 0.5f);
        public static readonly Color KILL_COLOR = new Color(0.2f, 1f, 0.2f);
        public static readonly Color SPECIAL_KILL_COLOR = new Color(255, 228, 0);
        public static readonly Color PLAYER_STATUS_COLOR = new Color(1f, 0.52f, 0.13f);

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
            PreText = preText;
            Text = text;
            TextColor = textColor;
        }

        public void Serialize(NetworkBinaryWriter writer, SerializationModeFlags mode)
        {
#if NETWORK_PROFILING
            using (new NetworkProfilingScope(this))
#endif
            {
                writer.Write((string)PreText);
                writer.Write((string)Text);
                writer.Write((Color)TextColor);
            }
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
