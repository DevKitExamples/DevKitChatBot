namespace DemoBotApp.WebSocket
{
    using System.Web;

    public static class HttpContextExtension
    {
        public static void AcceptWebSocketRequest(this HttpContext context, WebSocketHandler handler)
        {
            context.AcceptWebSocketRequest(handler.ProcessRequest);
        }
    }
}