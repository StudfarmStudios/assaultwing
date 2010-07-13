using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using AW2.Helpers;

namespace AW2.Net.ConnectionUtils
{
    /// <summary>
    /// A thread that receives data from a remote host until the socket
    /// is closed or there is some other error condition.
    /// </summary>
    public abstract class MessageReadThread : SuspendableStepwiseThread
    {
        public delegate void MessageHandler(byte[] messageHeaderBuffer, byte[] messageBodyBuffer);

        protected Socket _socket;
        private MessageHandler _messageHandler;
        private byte[] _headerReceiveBuffer;
        private byte[] _bodyReceiveBuffer;

        public MessageReadThread(string name, Socket socket, Action<Exception> exceptionHandler, MessageHandler messageHandler)
            : base(name, exceptionHandler)
        {
            _socket = socket;
            _messageHandler = messageHandler;
            _headerReceiveBuffer = new byte[Message.HeaderLength];
            SetAction(new StepwiseAction(KeepReadingMessages()));
        }

        // Stepwise method. Enumerated objects are undefined.
        private IEnumerable<object> KeepReadingMessages()
        {
            while (true)
            {
                foreach (var dummy in ReadHeader()) yield return null;
                foreach (var dummy in ReadBody()) yield return null;
                _messageHandler(_headerReceiveBuffer, _bodyReceiveBuffer);
                yield return null;
            }
        }

        // Stepwise method. Enumerated objects are undefined.
        private IEnumerable<object> ReadHeader()
        {
            foreach (var dummy in Receive(_headerReceiveBuffer, _headerReceiveBuffer.Length)) yield return null;
            if (!Message.IsValidHeader(_headerReceiveBuffer))
            {
                string txt = "Connection received an invalid message header [length:" +
                    _headerReceiveBuffer.Length + " data:" +
                    string.Join(",", Array.ConvertAll<byte, string>(_headerReceiveBuffer,
                    a => ((byte)a).ToString("X2"))) + "]";
                throw new InvalidDataException(txt);
            }
        }

        // Stepwise method. Enumerated objects are undefined.
        private IEnumerable<object> ReadBody()
        {
            int bodyLength = Message.GetBodyLength(_headerReceiveBuffer);
            if (_bodyReceiveBuffer == null || _bodyReceiveBuffer.Length < bodyLength)
                _bodyReceiveBuffer = new byte[bodyLength];
            foreach (var dummy in Receive(_bodyReceiveBuffer, bodyLength)) yield return null;
        }

        /// <summary>
        /// Receives a certain number of bytes to a buffer.
        /// This method blocks until the required number of bytes have been received,
        /// or until the socket is closed or there is some error condition.
        /// However, wrapping this method in <see cref="StepwiseAction"/> enables
        /// you to run this blocking method in small steps, effectively working around
        /// the blocking.
        /// </summary>
        /// <param name="buffer">The buffer to store the bytes in.</param>
        /// <param name="byteCount">The number of bytes to receive.</param>
        // Stepwise method. Enumerated objects are undefined.
        protected abstract IEnumerable<object> Receive(byte[] buffer, int byteCount);
    }
}
