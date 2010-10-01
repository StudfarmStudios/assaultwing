using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using AW2.Game.Gobs;
using AW2.Helpers;
using AW2.UI;

namespace AW2.Game.GobUtils
{
    // TODO: Remove this class or make it do something. Currently it just eats data but gives nothing out.
    public class ShipLocationPredicter
    {
        private ShipLocations _shipLocations;
        private Ship _ship;

        public ShipLocationPredicter(Ship ship)
        {
            _ship = ship;
            _shipLocations = new ShipLocations(ship);
        }

        /// <summary>
        /// Takes note of the control states of the owner of the ship after they have occurred.
        /// </summary>
        public void StoreControlStates(IList<ControlState> state, TimeSpan gameTime)
        {
            var halfFrameTime = _ship.Game.TargetElapsedTime.Divide(2);
            int entryIndex = _shipLocations.FindLastIndex(entry => entry.GameTime - halfFrameTime < gameTime);
            if (entryIndex == -1) return; // too old controls are useless
            if (entryIndex == _shipLocations.Count - 1) return; // controls appear in the future, cannot apply them :(

            // Apply controls to the ship location entry at the time of the controls
            // and propagate the change to newer ship location entries.
            var frameDurationSeconds = (float)(_shipLocations[entryIndex + 1].GameTime - _shipLocations[entryIndex].GameTime).TotalSeconds;
            float rotationChange = _ship.TurnSpeed * frameDurationSeconds *
                (state[(int)PlayerControlType.Left].Force - state[(int)PlayerControlType.Right].Force);
            var moveChange = AWMathHelper.GetUnitVector2(_ship.Rotation) * _ship.ThrustForce / _ship.Mass * frameDurationSeconds;
            for (int i = entryIndex + 1; i < _shipLocations.Count; ++i)
                _shipLocations[i].ApplyChange(rotationChange, moveChange, _shipLocations[entryIndex].GameTime);

            _shipLocations.RemoveRange(0, entryIndex + 1);
        }

        public void ForgetOldShipLocations()
        {
            _shipLocations.Clear();
        }

        public void StoreOldShipLocation(ShipLocationEntry entry)
        {
            _shipLocations.Add(entry);
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
        private static readonly TimeSpan ENTRY_AGE_MAX = TimeSpan.FromMilliseconds(500);

        /// <summary>
        /// A short history of the player's ship datas, stored by increasing game time.
        /// </summary>
        private List<ShipLocationEntry> _oldShipLocations;

        private Ship _ship;

        private ShipLocationEntry LatestEntry
        {
            get
            {
                return new ShipLocationEntry
                {
                    GameTime = _ship.Game.DataEngine.ArenaTotalTime,
                    Pos = _ship.Pos,
                    Move = _ship.Move,
                    Rotation = _ship.Rotation
                };
            }
        }

        public ShipLocations(Ship ship)
        {
            _ship = ship;
            _oldShipLocations = new List<ShipLocationEntry>();
        }

        public ShipLocationEntry Predict(TimeSpan gameTime)
        {
            throw new NotImplementedException("TODO");
        }

        public void CropOlderThan(TimeSpan gameTime)
        {
            int cropCount = _oldShipLocations.FindLastIndex(entry => entry.GameTime < gameTime);
            _oldShipLocations.RemoveRange(0, cropCount + 1);
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
            _oldShipLocations.RemoveRange(index, count);
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
                if (index == _oldShipLocations.Count) return LatestEntry;
                return _oldShipLocations[index];
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
            if (_oldShipLocations.Count > 0 && item.GameTime < _oldShipLocations.Last().GameTime) throw new ArgumentException("Cannot add an old ship location entry");
            CropOlderThan(item.GameTime - ENTRY_AGE_MAX);
            _oldShipLocations.Add(item);
        }

        public void Clear()
        {
            _oldShipLocations.Clear();
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
            get { return _oldShipLocations.Count + 1; }
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
