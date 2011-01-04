using System;
using System.Collections.Generic;
using AW2.Core;

namespace AW2.Game
{
    public class ChatContainer : IEnumerable<ChatContainer.Item>
    {
        public class Item
        {
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

        private List<Item> Items { get; set; }

        public ChatContainer()
        {
            Items = new List<Item>();
        }

        public void Add(PlayerMessage message)
        {
            Items.Add(new Item(message, AssaultWingCore.Instance.GameTime.TotalRealTime));
            if (Items.Count >= 2 * MESSAGE_KEEP_COUNT) Items.RemoveRange(0, Items.Count - MESSAGE_KEEP_COUNT);
            if (NewMessage != null) NewMessage(message);
        }

        public void Clear()
        {
            Items.Clear();
        }

        public IEnumerable<Item> Reversed()
        {
            for (int i = Items.Count - 1; i >= 0; i--) yield return Items[i];
        }

        public IEnumerator<ChatContainer.Item> GetEnumerator()
        {
            return Items.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return Items.GetEnumerator();
        }
    }
}
