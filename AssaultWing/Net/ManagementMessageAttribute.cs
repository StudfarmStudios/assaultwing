using System;

namespace AW2.Net
{
    /// <summary>
    /// An attribute providing information about a ManagementMessage subclass.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ManagementMessageAttribute : MessageTypeAttribute
    {
        public string Operation { get; private set; }

        public ManagementMessageAttribute(string operation)
        {
            Operation = operation;
        }
    }
}
