using System;
using System.Collections.Generic;
using System.Net.Sockets;
using AW2.Helpers;

namespace AW2.Net.ConnectionUtils
{
    /// <summary>
    /// A thread that sends data to the remote host until the socket
    /// is closed or there is some other error condition. 
    /// </summary>
    public abstract class MessageSendThread : SuspendableStepwiseThread
    {
        protected static int MaximumTransmissionUnit = 1500;

        protected Socket _socket;
        protected ThreadSafeWrapper<Queue<NetBuffer>> _sendBuffers;

        public MessageSendThread(string name, Socket socket, ThreadSafeWrapper<Queue<NetBuffer>> sendBuffers, Action<Exception> exceptionHandler)
            : base(name, exceptionHandler)
        {
            _socket = socket;
            _sendBuffers = sendBuffers;
            SetAction(new StepwiseAction(KeepSendingMessages()));
        }

        // Stepwise method. Enumerated objects are undefined.
        protected abstract IEnumerable<object> KeepSendingMessages();
    }
}
