using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;
using Microsoft.Xna.Framework;

namespace AW2.Events
{
    /// <summary>
    /// Describes an event that can be sent over internet (meaning that the message
    /// can be serialized/deserialized.
    /// </summary>
    /// Serialization methods (constructor and GetObjectData) must be extended by all subclasses
    [Serializable()]    //Set this attribute to all the classes that want to serialize
    class NetworkableEvent : Event, ISerializable
    {
        /// <summary>Deserialization constructor.</summary>
        /// Get the values from info and assign them to the appropriate properties
        public NetworkableEvent(SerializationInfo info, StreamingContext ctxt)
        {
            eventTime = (TimeSpan)info.GetValue("eventTime", typeof(TimeSpan));
        }
        
        /// <summary>Serialization function.</summary>
        /// You can use any custom name for your name-value pair. But make sure you
        /// read the values with the same name. For ex:- If you write EmpId as "EmployeeId"
        /// then you should read the same with "EmployeeId"
        public void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            info.AddValue("eventTime", eventTime);
        }
    }
}
