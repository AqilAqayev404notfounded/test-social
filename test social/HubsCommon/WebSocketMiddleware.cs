using System.Net.WebSockets;

namespace test_social.HubsCommon
{
    public class WebSocketMiddleware
    {
        private static readonly Dictionary<string, WebSocket> _connections = new();

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                var connectionId = Guid.NewGuid().ToString();

                _connections[connectionId] = webSocket;
                await HandleConnectionAsync(connectionId, webSocket);
            }
            else
            {
                await context.Response.WriteAsync("WebSocket endpoint");
            }
        }

        private async Task HandleConnectionAsync(string connectionId, WebSocket webSocket)
        {
            var buffer = new byte[1024 * 4];

            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    _connections.Remove(connectionId);
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", CancellationToken.None);
                }
                else
                {
                    // İstemciden gelen mesajları diğer istemcilere gönder
                    foreach (var pair in _connections)
                    {
                        if (pair.Key != connectionId)
                        {
                            await pair.Value.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), result.MessageType, result.EndOfMessage, CancellationToken.None);
                        }
                    }
                }
            }
        }
    }

}


