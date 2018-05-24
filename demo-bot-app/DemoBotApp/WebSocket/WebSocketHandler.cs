namespace DemoBotApp.WebSocket
{
    using System;
    using System.Collections.Generic;
    using System.Net.WebSockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web.WebSockets;

    public class WebSocketHandler
    {
        private WebSocket webSocket;

        public const int FrameBytesCount = 10 * 1024;

        public event EventHandler OnOpened;

        //public event EventHandler<string> OnTextMessageReceived;
        public event Func<object, string, Task> OnTextMessageReceived;

        //public event EventHandler<byte[]> OnBinaryMessageReceived;
        public event Func<object, byte[], Task> OnBinaryMessageReceived;

        public event EventHandler OnClosed;

        public virtual async Task ProcessRequest(AspNetWebSocketContext context)
        {
            webSocket = context.WebSocket;
            RaiseOpenEvent();

            while (webSocket.State == WebSocketState.Open)
            {
                List<byte> receivedBytes = new List<byte>();
                ArraySegment<byte> buffer = WebSocket.CreateServerBuffer(FrameBytesCount);
                WebSocketMessageType messageType = WebSocketMessageType.Text;

                WebSocketReceiveResult receiveResult = await webSocket.ReceiveAsync(buffer, CancellationToken.None);
                messageType = receiveResult.MessageType;

                // Close websocket if accept the close request from client
                if (receiveResult.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                    RaiseOnClosed();
                    break;
                }

                MergeFrameContent(receivedBytes, buffer.Array, receiveResult.Count);

                // Receive message from client continuously
                while (!receiveResult.EndOfMessage)
                {
                    buffer = WebSocket.CreateServerBuffer(FrameBytesCount);
                    receiveResult = await webSocket.ReceiveAsync(buffer, CancellationToken.None);
                    MergeFrameContent(receivedBytes, buffer.Array, receiveResult.Count);
                }

                RaiseMessageArrive(receivedBytes.ToArray(), messageType, receivedBytes.Count);
            }
        }

        public virtual async Task SendMessage(string message)
        {
            if (webSocket == null || webSocket.State != WebSocketState.Open)
            {
                throw new InvalidOperationException("the web socket is not open.");
            }

            var bytes = Encoding.UTF8.GetBytes(message);
            int sentBytes = 0;
            while (sentBytes < bytes.Length)
            {
                int remainingBytes = bytes.Length - sentBytes;
                bool isEndOfMessage = remainingBytes > FrameBytesCount ? false : true;

                await webSocket.SendAsync(
                    new ArraySegment<byte>(bytes, sentBytes, remainingBytes > FrameBytesCount ? FrameBytesCount : remainingBytes),
                    WebSocketMessageType.Text,
                    isEndOfMessage,
                    CancellationToken.None);

                sentBytes += FrameBytesCount;
            }
        }

        public virtual async Task SendBinary(byte[] bytes)
        {
            if (webSocket == null || webSocket.State != WebSocketState.Open)
            {
                throw new InvalidOperationException("the web socket is not open.");
            }

            int sentBytes = 0;
            while (sentBytes < bytes.Length)
            {
                int remainingBytes = bytes.Length - sentBytes;
                bool isEndOfMessage = remainingBytes > FrameBytesCount ? false : true;

                await webSocket.SendAsync(
                    new ArraySegment<byte>(bytes, sentBytes, remainingBytes > FrameBytesCount ? FrameBytesCount : remainingBytes),
                    WebSocketMessageType.Binary,
                    isEndOfMessage,
                    CancellationToken.None);

                sentBytes += remainingBytes > FrameBytesCount ? FrameBytesCount : remainingBytes;

                Thread.Sleep(50);
            }
        }

        public virtual async Task Close()
        {
            if (webSocket == null || webSocket.State == WebSocketState.Closed || webSocket.State == WebSocketState.Aborted)
            {
                return;
            }

            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server Close", CancellationToken.None);
        }

        protected void MergeFrameContent(List<Byte> destBuffer, byte[] buffer, long count)
        {
            count = count < buffer.Length ? count : buffer.Length;

            if (count == buffer.Length)
            {
                destBuffer.AddRange(buffer);
            }
            else
            {
                var frameBuffer = new byte[count];
                Array.Copy(buffer, frameBuffer, count);

                destBuffer.AddRange(frameBuffer);
            }
        }

        protected void RaiseOpenEvent()
        {
            OnOpened?.Invoke(this, EventArgs.Empty);
        }

        protected void RaiseMessageArrive(byte[] buffer, WebSocketMessageType type, long count)
        {
            if (OnTextMessageReceived != null)
            {
                switch (type)
                {
                    case WebSocketMessageType.Text:
                        OnTextMessageReceived(this, Encoding.UTF8.GetString(buffer));
                        break;
                    case WebSocketMessageType.Binary:
                        OnBinaryMessageReceived(this, buffer);
                        break;
                }
            }
        }

        protected void RaiseOnClosed()
        {
            OnClosed?.Invoke(this, EventArgs.Empty);
        }
    }
}