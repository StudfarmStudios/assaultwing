﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using AW2.Helpers;

namespace AW2.Net.ConnectionUtils
{
    /// <summary>
    /// A thread that receives data from a remote host until the socket
    /// is closed or there is some other error condition.
    /// </summary>
    public abstract class MessageReadThread : SuspendableStepwiseThread
    {
        public delegate void MessageHandler(NetBuffer messageHeaderAndBody);

        protected Socket _socket;
        private MessageHandler _messageHandler;

        public MessageReadThread(string name, Socket socket, Action<Exception> exceptionHandler, MessageHandler messageHandler)
            : base(name, exceptionHandler)
        {
            _socket = socket;
            _messageHandler = messageHandler;
            SetAction(new StepwiseAction(KeepReadingMessages()));
        }

        // Stepwise method. Enumerated objects are undefined.
        private IEnumerable<object> KeepReadingMessages()
        {
            while (true)
            {
                var headerAndBodyReceiveBuffer = new NetBuffer(new byte[Message.MAXIMUM_LENGTH], 0, new IPEndPoint(IPAddress.Any, 0));
                foreach (var dummy in ReceiveHeaderAndBody(headerAndBodyReceiveBuffer)) yield return null;
                _messageHandler(headerAndBodyReceiveBuffer);
                yield return null;
            }
        }

        /// <summary>
        /// Receives the header and the body of a message into a buffer
        /// and writes down the end point where the data came.
        /// Blocks until the header and the body have been received.
        /// </summary>
        // Stepwise method. Enumerated objects are undefined.
        protected abstract IEnumerable<object> ReceiveHeaderAndBody(NetBuffer headerAndBodyBuffer);
    }
}