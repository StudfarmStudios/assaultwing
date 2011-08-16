using System;
using System.Collections.Generic;
using AW2.Core;

namespace AW2.Game
{
    public class MessageContainer
    {
        public class Item
        {
            public bool IsChatMessage { get { return Message.PreText != ""; } }
            public PlayerMessage Message { get; private set; }
            public TimeSpan EntryRealTime { get; private set; }
            public Item(PlayerMessage message, TimeSpan entryRealTime)
            {
                Message = message;
                EntryRealTime = entryRealTime;
            }
        }

        private const int MESSAGE_KEEP_COUNT = 500;

        public event Action<PlayerMessage> NewMessage;

        public List<Item> ChatItems { get; private set; }
        public List<Item> CombatLogItems { get; private set; }

        public MessageContainer()
        {
            ChatItems = new List<Item>();
            CombatLogItems = new List<Item>();
        }

        public void Add(PlayerMessage message)
        {
            var item = new Item(message, AssaultWingCore.Instance.GameTime.TotalRealTime);
            var log = item.IsChatMessage ? ChatItems : CombatLogItems;
            log.Add(item);
            if (log.Count >= 2 * MESSAGE_KEEP_COUNT) log.RemoveRange(0, log.Count - MESSAGE_KEEP_COUNT);
            if (NewMessage != null) NewMessage(message);
        }

        public void Clear()
        {
            ChatItems.Clear();
            CombatLogItems.Clear();
        }

        public IEnumerable<Item> ReversedChat()
        {
            for (int i = ChatItems.Count - 1; i >= 0; i--) yield return ChatItems[i];
        }

        public IEnumerable<Item> ReversedCombatLog()
        {
            for (int i = CombatLogItems.Count - 1; i >= 0; i--) yield return CombatLogItems[i];
        }
    }
}
