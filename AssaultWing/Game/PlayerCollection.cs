using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AW2.Game
{
    /// <summary>
    /// A collection of players.
    /// </summary>
    public class PlayerCollection : IList<Player>
    {
        List<Player> players = new List<Player>();

        #region Events

        /// <summary>
        /// Called when an item has been removed from the collection.
        /// The argument is the removed item.
        /// </summary>
        public event Action<Player> Removed;

        #endregion

        /// <summary>
        /// Removes items that satisfy a condition.
        /// </summary>
        /// <param name="condition">The condition by which to remove items.</param>
        public void Remove(Predicate<Player> condition)
        {
            for (int index = players.Count - 1; index >= 0; --index)
                if (condition(players[index]))
                    RemoveAt(index);
        }

        #region IList<Player> Members

        /// <summary>
        /// Determines the index of a specific item in the list.
        /// </summary>
        public int IndexOf(Player item)
        {
            return players.IndexOf(item);
        }

        /// <summary>
        /// Inserts an item to the list at the specified index.
        /// </summary>
        public void Insert(int index, Player item)
        {
            players.Insert(index, item);
        }

        /// <summary>
        /// Removes the list item at the specified index.
        /// </summary>
        public void RemoveAt(int index)
        {
            Player removed = players[index];
            players.RemoveAt(index);
            if (Removed != null) Removed(removed);
        }

        /// <summary>
        /// The list item at the specified index.
        /// </summary>
        public Player this[int index] { get { return players[index]; } set { players[index] = value; } }

        #endregion

        #region ICollection<Player> Members

        /// <summary>
        /// Adds an item to the collection.
        /// </summary>
        public void Add(Player item)
        {
            players.Add(item);
        }

        /// <summary>
        /// Removes all items from the collection.
        /// </summary>
        public void Clear()
        {
            if (Removed != null)
            {
                var removedPlayers = players.ToArray();
                players.Clear();
                foreach (var player in removedPlayers)
                    Removed(player);
            }
            else
                players.Clear();
        }

        /// <summary>
        /// Determines whether the collection contains a specific value.
        /// </summary>
        public bool Contains(Player item)
        {
            return players.Contains(item);
        }

        /// <summary>
        /// Copies the elements of the collection to an array, starting at a particular array index.
        /// </summary>
        public void CopyTo(Player[] array, int arrayIndex)
        {
            players.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// The number of elements contained in the collection.
        /// </summary>
        public int Count { get { return players.Count; } }

        /// <summary>
        /// Is the collection read-only.
        /// </summary>
        public bool IsReadOnly { get { return false; } }

        /// <summary>
        /// Removes the first occurrence of a specific element from the collection.
        /// </summary>
        public bool Remove(Player item)
        {
            int index = players.IndexOf(item);
            if (index >= 0)
            {
                var removed = players[index];
                players.RemoveAt(index);
                if (Removed != null) Removed(removed);
                return true;
            }
            else 
                return false;
        }

        #endregion

        #region IEnumerable<Player> Members

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        public IEnumerator<Player> GetEnumerator()
        {
            return players.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return ((System.Collections.IEnumerable)players).GetEnumerator();
        }

        #endregion
    }
}
