using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using AW2.Game.Gobs;
using AW2.Helpers;
using AW2.UI;

namespace AW2.Game.GobUtils
{
    public class ShipLocationPredicter
    {
        public struct ShipData
        {
            public ShipLocationEntry ShipLocationEntry;
            public float Mass;
            public float TurnSpeed;
            public float ThrustForce;
            public TimeSpan TargetElapsedTime;
        }

        private ShipLocations _shipLocations;
        private Func<ShipData> _getShipData;

        public int ShipLocationCount { get { return _shipLocations.Count; } }

        public ShipLocationPredicter(Func<ShipData> getShipData, Action<ShipLocationEntry> setShipLocation)
        {
            _getShipData = getShipData;
            _shipLocations = new ShipLocations(getShipData, setShipLocation);
        }

        private static ShipData DefaultShipDataProvider(Ship ship)
        {
            return new ShipData
            {
                ShipLocationEntry = new ShipLocationEntry
                {
                    Pos = ship.Pos,
                    Move = ship.Move,
                    Rotation = ship.Rotation,
                    GameTime = ship.Game.DataEngine.ArenaTotalTime,
                    ControlStates = ship.GetControlStates(),
                },
                TurnSpeed = ship.TurnSpeed,
                ThrustForce = ship.Thruster.MaxForce,
                Mass = ship.Mass,
                TargetElapsedTime = AW2.Core.AssaultWingCore.TargetElapsedTime,
            };
        }

        private static void DefaultShipLocationSetter(ShipLocationEntry entry, Ship ship)
        {
            ship.Pos = entry.Pos;
            ship.Move = entry.Move;
            ship.Rotation = entry.Rotation;
        }

        public ShipLocationPredicter(Ship ship)
            : this(() => DefaultShipDataProvider(ship), entry => DefaultShipLocationSetter(entry, ship))
        {
        }

        /// <summary>
        /// Takes note of the control states of the owner of the ship after they have occurred.
        /// </summary>
        public void StoreControlStates(IList<ControlState> state, TimeSpan gameTime)
        {
            var shipData = _getShipData();
            var halfFrameTime = shipData.TargetElapsedTime.Divide(2);
            int entryIndex = _shipLocations.FindLastIndex(entry => entry.GameTime - halfFrameTime < gameTime);
            if (entryIndex == -1) return; // too old controls are useless
            if (entryIndex == _shipLocations.Count - 1) return; // controls appear in the future, cannot apply them :(

            // Apply controls to the ship location entry at the time of the controls
            // and propagate the change to newer ship location entries.
            var frameDurationSeconds = (float)(_shipLocations[entryIndex + 1].GameTime - _shipLocations[entryIndex].GameTime).TotalSeconds;
            float rotationChange = shipData.TurnSpeed * frameDurationSeconds *
                (state[(int)PlayerControlType.Left].Force - state[(int)PlayerControlType.Right].Force);
            var moveChange = AWMathHelper.GetUnitVector2(shipData.ShipLocationEntry.Rotation) * shipData.ThrustForce / shipData.Mass * frameDurationSeconds;
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

        /// <summary>
        /// Called on a game client to update an old ship location based on data received from the game server.
        /// Old ControlStates are not to be updated and should be null in the given ShipLocationEntry.
        /// Setting an old ShipLocationEntry affects all newer ShipLocationEntries.
        /// </summary>
        public void UpdateOldShipLocation(ShipLocationEntry entry)
        {
            if (entry.ControlStates != null) throw new ArgumentException("Expected null ControlStates");
            var shipData = _getShipData();
            var halfFrameTime = shipData.TargetElapsedTime.Divide(2);
            int entryIndex = _shipLocations.FindLastIndex(e => e.GameTime - halfFrameTime < entry.GameTime);
            for (int i = entryIndex; 0 <= i && i < _shipLocations.Count; ++i)
            {
                var oldEntry = _shipLocations[i];
                var newEntry = oldEntry;
                if (i == entryIndex)
                    newEntry.Rotation = entry.Rotation;
                else
                {
                    var prevEntry = _shipLocations[i - 1];
                    var elapsedSeconds = (float)(oldEntry.GameTime - prevEntry.GameTime).TotalSeconds;
                    var rotationChange = shipData.TurnSpeed * elapsedSeconds *
                        (oldEntry.ControlStates[(int)PlayerControlType.Left].Force - oldEntry.ControlStates[(int)PlayerControlType.Right].Force);
                    newEntry.Rotation = prevEntry.Rotation + rotationChange;
                }
                _shipLocations[i] = newEntry;
            }
        }

        public ShipLocationEntry GetShipLocation(TimeSpan gameTime)
        {
            int prevEntryIndex = _shipLocations.FindLastIndex(entry => entry.GameTime <= gameTime);
            if (prevEntryIndex == _shipLocations.Count - 1)
            {
                var shipData = _getShipData();
                var prevEntry = _shipLocations[prevEntryIndex];
                var seconds = (float)(gameTime - prevEntry.GameTime).TotalSeconds;
                var posDelta = prevEntry.Move * seconds;
                var rotationDelta = seconds * shipData.TurnSpeed *
                    (prevEntry.ControlStates[(int)PlayerControlType.Left].Force -
                    prevEntry.ControlStates[(int)PlayerControlType.Right].Force);
                var entry = new ShipLocationEntry
                {
                    GameTime = gameTime,
                    Pos = prevEntry.Pos + posDelta,
                    Move = prevEntry.Move,
                    Rotation = prevEntry.Rotation + rotationDelta,
                    ControlStates = prevEntry.ControlStates,
                };
                return entry;
            }
            else
            {
                var prevEntry = _shipLocations[prevEntryIndex];
                var nextEntry = _shipLocations[prevEntryIndex + 1];
                // Interpolate between prevEntry and nextEntry
                float nextWeight = (gameTime - prevEntry.GameTime).Ticks / (float)(nextEntry.GameTime - prevEntry.GameTime).Ticks;
                return new ShipLocationEntry
                {
                    GameTime = gameTime,
                    Pos = Vector2.Lerp(prevEntry.Pos, nextEntry.Pos, nextWeight),
                    Move = Vector2.Lerp(prevEntry.Move, nextEntry.Move, nextWeight),
                    Rotation = MathHelper.Lerp(prevEntry.Rotation, nextEntry.Rotation, nextWeight),
                    ControlStates = prevEntry.ControlStates,
                };
            }
        }
    }

    public struct ShipLocationEntry
    {
        public TimeSpan GameTime { get; set; }
        public Vector2 Pos { get; set; }
        public Vector2 Move { get; set; }
        public float Rotation { get; set; }
        public ControlState[] ControlStates { get; set; }

        public void ApplyChange(float rotationChange, Vector2 moveChange, TimeSpan changeTime)
        {
            Rotation += rotationChange;
            Move += moveChange;
            Pos += moveChange * (float)(GameTime - changeTime).TotalSeconds;
        }

        public override string ToString()
        {
            return string.Format("GameTime = {0}, Pos = {1}, Move = {2}, Rotation = {3}, " +
                "ControlStates = th:{4}, lf:{5}, rg:{6}, w1:{7}, w2:{8}, xt:{9}",
                GameTime, Pos, Move, Rotation,
                ControlStates[(int)PlayerControlType.Thrust],
                ControlStates[(int)PlayerControlType.Left],
                ControlStates[(int)PlayerControlType.Right],
                ControlStates[(int)PlayerControlType.Fire1],
                ControlStates[(int)PlayerControlType.Fire2],
                ControlStates[(int)PlayerControlType.Extra]);
        }
    }

    /// <summary>
    /// A list of ship location entries, including the ship's present location.
    /// Entries are ordered by increasing game time, i.e. index 0 contains the oldest entry.
    /// </summary>
    public class ShipLocations : IList<ShipLocationEntry>
    {
        private static readonly TimeSpan ENTRY_AGE_MAX = TimeSpan.FromMilliseconds(500);

        /// <summary>
        /// A short history of the player's ship datas, ordered by increasing game time
        /// (index 0 contains the oldest entry).
        /// </summary>
        private List<ShipLocationEntry> _oldShipLocations;

        private Func<ShipLocationPredicter.ShipData> _getShipData;
        private Action<ShipLocationEntry> _setShipLocation;

        private ShipLocationEntry LatestEntry { get { return _getShipData().ShipLocationEntry; } }

        public ShipLocations(Func<ShipLocationPredicter.ShipData> getShipData, Action<ShipLocationEntry> setShipLocation)
        {
            _getShipData = getShipData;
            _setShipLocation = setShipLocation;
            _oldShipLocations = new List<ShipLocationEntry>();
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
                if (index == _oldShipLocations.Count)
                    _setShipLocation(value);
                else
                    _oldShipLocations[index] = value;
            }
        }

        #endregion

        #region ICollection<ShipLocationEntry> Members

        public void Add(ShipLocationEntry item)
        {
            if (_oldShipLocations.Count > 0 && item.GameTime < _oldShipLocations.Last().GameTime)
                throw new ArgumentException("Cannot add an old ship location entry");
            if (item.GameTime > _getShipData().ShipLocationEntry.GameTime)
                throw new ArgumentException("Cannot add a future ship location entry");
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
