using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using AW2.Helpers;

namespace AW2.Net.ConnectionUtils
{
    /// <summary>
    /// A thread that receives data from a remote host via TCP until the socket
    /// is closed or there is some other error condition.
    /// </summary>
    public class TCPMessageReadThread : MessageReadThread
    {
        public TCPMessageReadThread(Socket socket, Action<Exception> exceptionHandler, MessageHandler messageHandler)
            : base("TCP Message Read Thread", socket, exceptionHandler, messageHandler)
        {
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
        protected override IEnumerable<object> Receive(byte[] buffer, int byteCount)
        {
            if (buffer == null) throw new ArgumentNullException("buffer", "Cannot receive to null buffer");
            if (byteCount < 0) throw new ArgumentException("Cannot receive negative number of bytes", "byteCount");
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
