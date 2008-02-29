// marked in as DEBUG because we don't want NUnit framework to release builds
#if DEBUG
using NUnit.Framework;
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using AW2.Helpers;

namespace AW2.Events
{
    /// <summary>
    /// Comparer of events. Comparisons are based on scheduled event processing times.
    /// </summary>
    public class EventComparer : IComparer
    {
        #region IComparer Members

        /// <summary>
        /// Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
        /// </summary>
        /// <param name="x">The first object to compare.</param>
        /// <param name="y">The second object to compare.</param>
        /// <returns>Less than zero, if x is less than y. 
        /// Zero, if x equals y. 
        /// Greater than zero, if x is greater than y.</returns>
        public int Compare(object x, object y)
        {
            if (!(x is Event) || !(y is Event))
                throw new ArgumentException("EventComparer can compare only Event instances");
            return ((Event)x).EventTime.CompareTo(((Event)y).EventTime);
        }

        #endregion
    }
    
    /// <summary>
    /// Basic Event Engine implementation.
    /// </summary>
    class EventEngineImpl : EventEngine
    {
        Dictionary<Type, BinaryPriorityQueue> eventDictionary;

        public EventEngineImpl()
        {
            eventDictionary = new Dictionary<Type, BinaryPriorityQueue>();
        }

        #region EventEngine Members

        /// <summary>
        /// Pushes an event to the event queue.
        /// </summary>
        /// <param name="eventToSend">The event to push.</param>
        public void SendEvent(Event eventToSend)
        {
            if (eventDictionary.ContainsKey(eventToSend.GetType())) {
                eventDictionary[eventToSend.GetType()].Push(eventToSend);
            } else {
                BinaryPriorityQueue lista = new BinaryPriorityQueue(new EventComparer());
                lista.Push(eventToSend);
                eventDictionary[eventToSend.GetType()] = lista;
            }
        }

        /// <summary>
        /// Returns the oldest event of the given type from the event queue,
        /// or null if there are no events of the given type.
        /// </summary>
        /// <param name="type">The type of event to get.</param>
        /// <returns>The oldest event of that type, or null if no such events are waiting.</returns>
        public Event GetEvent(Type type)
        {
            if (eventDictionary.ContainsKey(type))
            {
                Event eve = (Event)eventDictionary[type].Peek();
                if (eve != null && eve.EventTime <= AssaultWing.Instance.GameTime.TotalGameTime)
                    return (Event)eventDictionary[type].Pop();
            }
            return null;
        }

        /// <summary>
        /// Returns the oldest event of the given type from the event queue,
        /// or null if there are no events of the given type.
        /// </summary>
        /// <typeparam name="T">The type of event to get.</typeparam>
        /// <returns>The oldest event of that type, or null if no such events are waiting.</returns>
        public T GetEvent<T>() where T : Event
        {
            Type type = typeof(T);
            if (eventDictionary.ContainsKey(type))
            {
                T eve = (T)eventDictionary[type].Peek();
                if (eve != null && eve.EventTime <= AssaultWing.Instance.GameTime.TotalGameTime)
                    return (T)eventDictionary[type].Pop();
            }
            return null;
        }

        #endregion


        #region Unit tests
#if DEBUG

        [TestFixture]
        public class EventEngineTest
        {
            EventEngine engine;

            [SetUp]
            public void SetUp()
            {
                engine = new EventEngineImpl();
            }


            /// <summary>
            /// Sending an event to empty queue and requesting it should return the same event
            /// </summary>
            [Test]
            public void TestSendGet()
            {
                Event eventti = new Event();
                engine.SendEvent(eventti);
                Event reply = engine.GetEvent(typeof(Event));
                Assert.IsNotNull(reply);
                Assert.AreEqual(reply, eventti);
            }

            /// <summary>
            /// Requesting event from empty queue should return null
            /// </summary>
            [Test]
            public void TestGetNull()
            {
                Event returned = engine.GetEvent(typeof(Event));
                Assert.IsNull(returned);
            }

            [Test]
            public void TestGetTwoOfTheSame()
            {
                Event send1 = new SoundEffectEvent();
                Event send2 = new SoundEffectEvent();
                engine.SendEvent(send1);
                engine.SendEvent(send2);
                Event return1 = engine.GetEvent(typeof(SoundEffectEvent));
                Event return2 = engine.GetEvent(typeof(SoundEffectEvent));

                Assert.IsNotNull(return1);
                Assert.IsNotNull(return2);
                Assert.AreEqual(return1, send1);
                Assert.AreEqual(return2, send2);
            }
        }
#endif
        #endregion // Unit tests

    }
}
