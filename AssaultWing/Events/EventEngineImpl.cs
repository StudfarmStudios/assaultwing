// marked in as DEBUG because we don't want NUnit framework to release builds
#if DEBUG
using NUnit.Framework;
#endif

using System;
using System.Collections.Generic;
using System.Text;

namespace AW2.Events
{
    /// <summary>
    /// Basic Event Engine implementation.
    /// </summary>
    class EventEngineImpl : EventEngine
    {
        Dictionary<Type, Queue<Event>> eventDictionary;

        public EventEngineImpl()
        {
            eventDictionary = new Dictionary<Type, Queue<Event>>();
        }

        #region EventEngine Members

        /// <summary>
        /// Pushes an event to the event queue.
        /// </summary>
        /// <param name="eventToSend">The event to push.</param>
        public void SendEvent(Event eventToSend)
        {
            if (eventDictionary.ContainsKey(eventToSend.GetType())) {
                eventDictionary[eventToSend.GetType()].Enqueue(eventToSend);
            } else {
                Queue<Event> lista = new Queue<Event>();
                lista.Enqueue(eventToSend);
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
                if (eventDictionary[type].Count > 0)
                {
                    Event returnedEvent = eventDictionary[type].Dequeue();
                    return returnedEvent;
                }
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
