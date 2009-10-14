using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using AW2.Game.Gobs;
using AW2.Helpers;
using AW2.UI;

namespace AW2.Game
{
    public class ShipLocationPredicter
    {
        ShipLocations shipLocations;

        Ship ship;

        public ShipLocationPredicter(Ship ship)
        {
            this.ship = ship;
            shipLocations = new ShipLocations(ship);
        }

        /// <summary>
        /// Takes note of the control states of the owner of the ship after they have occurred.
        /// </summary>
        public void StoreControlStates(IList<ControlState> state, TimeSpan gameTime)
        {
            var halfFrameTime = AssaultWing.Instance.TargetElapsedTime.Divide(2);
            int entryIndex = shipLocations.FindLastIndex(entry => entry.GameTime - halfFrameTime < gameTime);
            if (entryIndex == -1) return; // too old controls

            // Apply controls to the ship location entry at the time of the controls
            // and propagate the change to newer ship location entries.
            if (entryIndex == 0) throw new ApplicationException("Cannot apply remote controls to the current frame");
            var frameDurationSeconds = (float)(shipLocations[entryIndex + 1].GameTime - shipLocations[entryIndex].GameTime).TotalSeconds;
            float rotationChange = ship.TurnSpeed * frameDurationSeconds *
                (state[(int)PlayerControlType.Left].force - state[(int)PlayerControlType.Right].force);
            var moveChange = AWMathHelper.GetUnitVector2(ship.Rotation) * ship.ThrustForce / ship.Mass * frameDurationSeconds;
            for (int i = entryIndex + 1; i < shipLocations.Count; ++i)
                shipLocations[i].ApplyChange(rotationChange, moveChange, shipLocations[entryIndex].GameTime);

            shipLocations.RemoveRange(0, entryIndex + 1);
        }

        public void ForgetOldShipLocations()
        {
            shipLocations.Clear();
        }

        public void StoreOldShipLocation(ShipLocationEntry entry)
        {
            shipLocations.Add(entry);
        }
    }

    public struct ShipLocationEntry
    {
        public TimeSpan GameTime { get; set; }
        public Vector2 Pos { get; set; }
        public Vector2 Move { get; set; }
        public float Rotation { get; set; }

        public void ApplyChange(float rotationChange, Vector2 moveChange, TimeSpan changeTime)
        {
            Rotation += rotationChange;
            Move += moveChange;
            Pos += moveChange * (float)(GameTime - changeTime).TotalSeconds;
        }
    }

    /// <summary>
    /// A list of ship location entries, including the ship's present location.
    /// </summary>
    public class ShipLocations : IList<ShipLocationEntry>
    {
        static readonly TimeSpan ENTRY_AGE_MAX = TimeSpan.FromMilliseconds(200);

        /// <summary>
        /// A short history of the player's ship datas, stored by increasing game time.
        /// </summary>
        List<ShipLocationEntry> oldShipLocations;

        Ship ship;

        ShipLocationEntry LatestEntry
        {
            get
            {
                return new ShipLocationEntry
                {
                    GameTime = AssaultWing.Instance.GameTime.TotalGameTime,
                    Pos = ship.Pos,
                    Move = ship.Move,
                    Rotation = ship.Rotation
                };
            }
        }

        public ShipLocations(Ship ship)
        {
            this.ship = ship;
            oldShipLocations = new List<ShipLocationEntry>();
        }

        public void CropOlderThan(TimeSpan gameTime)
        {
            int cropCount = oldShipLocations.FindLastIndex(entry => entry.GameTime < gameTime);
            oldShipLocations.RemoveRange(0, cropCount + 1);
        }

        #region List<ShipLocationEntry>'ish Members

        public int FindLastIndex(Predicate<ShipLocationEntry> match)
        {
            for (int index = Count - 1; index >= 0; --index)
                if (match(this[index])) return index;
            return -1;
        }

        public void RemoveRange(int index, int count)
        {
            if (index < 0 || index >= Count || count < 0) throw new ArgumentException("Invalid arguments to RemoveRange");
            if (index + count >= Count) throw new ArgumentException("Cannot remove the latest ship location");
            oldShipLocations.RemoveRange(index, count);
        }

        #endregion

        #region IList<ShipLocationEntry> Members

        public int IndexOf(ShipLocationEntry item)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, ShipLocationEntry item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        public ShipLocationEntry this[int index]
        {
            get
            {
                if (index == oldShipLocations.Count) return LatestEntry;
                return oldShipLocations[index];
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        #endregion

        #region ICollection<ShipLocationEntry> Members

        public void Add(ShipLocationEntry item)
        {
            if (oldShipLocations.Count > 0 && item.GameTime < oldShipLocations.Last().GameTime) throw new ArgumentException("Cannot add an old ship location entry");
            CropOlderThan(item.GameTime - ENTRY_AGE_MAX);
            oldShipLocations.Add(item);
        }

        public void Clear()
        {
            oldShipLocations.Clear();
        }

        public bool Contains(ShipLocationEntry item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(ShipLocationEntry[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get { return oldShipLocations.Count + 1; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(ShipLocationEntry item)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IEnumerable<ShipLocationEntry> Members

        public IEnumerator<ShipLocationEntry> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
