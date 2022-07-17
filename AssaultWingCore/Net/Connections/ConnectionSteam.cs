using AW2.Core;
using Steamworks;
using AW2.Helpers;
using AW2.Helpers.Serialization;
using System.Runtime.InteropServices;

namespace AW2.Net.Connections
{
    /// <summary>
    /// A connection to a remote host over Steam network. Communication between 
    /// the local and remote host is done by messages. 
    /// </summary>
    /// <remarks>
    /// Connection operates asynchronously. Both creation of connections and 
    /// sending messages via connections are done asynchronously. Therefore 
    /// their result is not known by the time the corresponding method call returns. 
    /// When results of such actions eventually arrive (as either success or 
    /// failure), they are added to corresponding queues.
    /// It is up to the client program to read the results from the queues.
    /// This can be done handily in the client program main loop. If such 
    /// a loop is not available, or for other reasons, the client program 
    /// can hook up events that notify of finished asynchronous operations.
    /// Such queues exist for connection attempts (static), received messages 
    /// (for each connection) and general error conditions (for each connection).
    /// 
    /// This class is thread safe.
    /// </remarks>
    public abstract class ConnectionSteam : ConnectionBase, IDisposable
    {
        private const int BUFFER_LENGTH = 65536;

        private readonly byte[] Buffer = new byte[BUFFER_LENGTH];
        private GCHandle PinnedBuffer;

        public HSteamNetConnection Handle { get; init; }
        public SteamNetConnectionInfo_t Info { get; set; }

        protected ConnectionSteam(AssaultWingCore game, HSteamNetConnection handle, SteamNetConnectionInfo_t info)
            : base(game)
        {
            Handle = handle;
            Info = info;
            PinnedBuffer = GCHandle.Alloc(Buffer, GCHandleType.Pinned);
        }

        public override void QueueError(string message)
        {
            throw new NotImplementedException();
        }

        public override void Send(Message message)
        {
            if (IsDisposed) return;

            // TODO: Consider implementing a stream to write directly to memory allocated by Steam to avoid copying
            // Some background on how this could work maybe 
            // https://github.com/rlabrecque/Steamworks.NET/issues/388
            // https://github.com/rlabrecque/Steamworks.NET/issues/411
            
            var steamMessageIntPtr = SteamNetworkingUtils.AllocateMessage(0);
            //SteamNetworkingMessage_t steamMessage = SteamNetworkingMessage_t.FromIntPtr(steamMessageIntPtr);
            //steamMessage.m_pData = (IntPtr)PinnedBuffer.AddrOfPinnedObject();
            

            var writer = NetworkBinaryWriter.Create(new MemoryStream(Buffer));
            message.Serialize(writer);
            writer.GetBaseStream().Flush();
            //steamMessage.m_cbSize = (uint)writer.GetBaseStream().Position;
            //steamMessage.m_conn = Handle;

            int flags;
            switch (message.SendType)
            {
                case MessageSendType.TCP:
                    flags = Constants.k_nSteamNetworkingSend_Reliable;
                    break;
                case MessageSendType.UDP:
                    flags = Constants.k_nSteamNetworkingSend_UnreliableNoDelay | Constants.k_nSteamNetworkingSend_UnreliableNoNagle;
                    break;
                default: throw new MessageException("Unknown send type " + message.SendType);
            }
            
            var size = writer.GetBaseStream().Position;
            long messageNumber;
            var result = SteamNetworkingSockets.SendMessageToConnection(Handle, (IntPtr)PinnedBuffer.AddrOfPinnedObject(), (uint)size, flags, out messageNumber);

            if (result != EResult.k_EResultOK) {
                Log.Write($"Error {result} sending message {message.Type} flags:{flags} num:{messageNumber} size:{size}");
            } else {
                // Log.Write($"Sending message {message.Type} flags:{flags} num:{messageNumber} size:{size}");
            }
        }

        /// <summary>
        /// Performs the actual disposing.
        /// </summary>
        /// <param name="error">If <c>true</c> then an internal error has occurred.</param>
        override protected void DisposeImpl(bool error)
        {
            SteamNetworkingSockets.CloseConnection(Handle, 0, "Disposed", true);
            DisposeId();
            PinnedBuffer.Free();
        }
    }
}