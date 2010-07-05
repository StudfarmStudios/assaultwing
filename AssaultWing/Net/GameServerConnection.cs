using System.Net.Sockets;

namespace AW2.Net
{
    /// <summary>
    /// A network connection to a game server.
    /// </summary>
    public class GameServerConnection : Connection
    {
        /// <summary>
        /// Creates a new connection to a game server.
        /// </summary>
        /// <param name="socket">An opened socket to the remote host. The
        /// created connection owns the socket and will dispose of it.</param>
        public GameServerConnection(Socket socket)
            : base(socket)
        {
            Name = "Game Server Connection " + ID;
        }

        /// <summary>
        /// Performs the actual diposing.
        /// </summary>
        /// <param name="error">If <c>true</c> then an internal error
        /// has occurred.</param>
        protected override void DisposeImpl(bool error)
        {
            if (error)
            {
                AssaultWing.Instance.StopClient();
                var dialogData = new AW2.Graphics.OverlayComponents.CustomOverlayDialogData(
                    "Connection to server lost!\nPress Enter to return to Main Menu",
                    new AW2.UI.TriggeredCallback(AW2.UI.TriggeredCallback.GetProceedControl(),
                        AssaultWing.Instance.ShowMenu));
                AssaultWing.Instance.ShowDialog(dialogData);
            }
            base.DisposeImpl(error);
        }
    }
}
