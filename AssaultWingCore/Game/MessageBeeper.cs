using System;
using System.Collections.Generic;
using System.Linq;
using AW2.Core;

namespace AW2.Game
{
    /// <summary>
    /// Makes a sound when new messages appear.
    /// </summary>
    public class MessageBeeper
    {
        private AssaultWingCore _game;
        private string _beepSoundName;
        private TimeSpan _lastMessageEntryRealTime;

        private Func<MessageContainer.Item> GetLatestMessage { get; set; }

        public MessageBeeper(AssaultWingCore game, string beepSoundName, Func<MessageContainer.Item> getLatestMessage)
        {
            _game = game;
            _beepSoundName = beepSoundName;
            GetLatestMessage = getLatestMessage;
        }

        public void BeepOnNewMessage()
        {
            var latestMessage = GetLatestMessage();
            if (latestMessage != null && latestMessage.EntryRealTime != _lastMessageEntryRealTime)
            {
                _lastMessageEntryRealTime = latestMessage.EntryRealTime;
                // Don't beep if the message appeared while we weren't watching.
                if (latestMessage.EntryRealTime + TimeSpan.FromSeconds(0.1) > _game.GameTime.TotalRealTime)
                    _game.SoundEngine.PlaySound(_beepSoundName);
            }
        }
    }
}
