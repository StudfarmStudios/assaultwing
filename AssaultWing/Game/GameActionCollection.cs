using System;
using System.Collections.Generic;
using System.Linq;
using AW2.Net;
using AW2.Helpers;

namespace AW2.Game
{
    public class GameActionCollection : IEnumerable<GameAction>, INetworkSerializable
    {
        List<GameAction> _items;
        List<GameAction> _toRemove;

        public GameActionCollection()
        {
            _items = new List<GameAction>();
            _toRemove = new List<GameAction>();
        }

        public void AddOrReplace(GameAction item)
        {
            int index = _items.FindIndex(a => a.GetType() == item.GetType());
            if (index >= 0)
                _items[index] = item;
            else
                _items.Add(item);
        }

        public void RemoveLater(GameAction item)
        {
            item.RemoveAction();
            _toRemove.Add(item);
        }

        public void CommitRemoves()
        {
            foreach (var item in _toRemove) _items.Remove(item);
            _toRemove.Clear();
        }

        public void Clear()
        {
            foreach (var item in _items) item.RemoveAction();
            _items.Clear();
            _toRemove.Clear();
        }

        public IEnumerator<GameAction> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        public void Serialize(NetworkBinaryWriter writer, SerializationModeFlags mode)
        {
            if ((mode & SerializationModeFlags.VaryingData) != 0)
            {
                writer.Write((short)_items.Count);
                foreach (var item in _items) item.Serialize(writer, mode);
            }
        }

        public void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode, TimeSpan messageAge)
        {
            if ((mode & SerializationModeFlags.VaryingData) != 0)
            {
                int count = reader.ReadInt16();
                _items.Clear();
                for (int i = 0; i < count; ++i)
                {
                    var itemTypeName = (CanonicalString)reader.ReadInt32();
                    var item = new GameAction(); // !!! must find the correct subclass
                    item.Deserialize(reader, mode, messageAge);
                }
            }
        }
    }
}
