using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using AW2.Helpers;

namespace AW2.Net
{
    /// <summary>
    /// A thread that receives data from a remote host until the socket
    /// is closed or there is some other error condition.
    /// </summary>
    public class MessageReadThread : SuspendableStepwiseThread
    {
        public delegate void MessageHandler(byte[] messageHeaderBuffer, byte[] messageBodyBuffer);

        private MessageHandler _messageHandler;
        private Socket _socket;
        private byte[] _headerReceiveBuffer;
        private byte[] _bodyReceiveBuffer;

        public MessageReadThread(Socket socket, Action<Exception> exceptionHandler, MessageHandler messageHandler)
            : base("Message Read Thread", exceptionHandler)
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
        private IEnumerable<object> Receive(byte[] buffer, int byteCount)
        {
            if (buffer == null) throw new ArgumentNullException("Cannot receive to null buffer");
            if (byteCount < 0) throw new ArgumentException("Cannot receive negative number of bytes");
            int totalReadBytes = 0;
            while (totalReadBytes < byteCount)
            {
                if (_socket.Available == 0)
                {
                    // See if the socket is still connected. If Poll() shows that there
                    // is data to read but Available is still zero, the socket must have
                    // been closed at the remote host.
                    if (_socket.Poll(100, SelectMode.SelectRead) && _socket.Available == 0)
                        throw new SocketException((int)SocketError.NotConnected);

                    // We are still connected but there's no data.
                    // Let other threads do their stuff while we wait.
                    Thread.Sleep(0);
                }
                else
                {
                    int readBytes = _socket.Receive(buffer, totalReadBytes, byteCount - totalReadBytes, SocketFlags.None);
                    totalReadBytes += readBytes;
                }
                yield return null;
            }
            if (totalReadBytes > byteCount)
                AW2.Helpers.Log.Write("WARNING: Read " + totalReadBytes + " bytes when only " + byteCount + " was requested");
        }
    }
}
