using AccsaberLeaderboard.Utils;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AccsaberLeaderboard.API
{
    internal static class AccsaberLiveScores
    {
        public static event Action<AccsaberAPI.ScoreInfoToken> OnScoreUpdated;
        public static event Action<AccsaberAPI.ScoreInfoToken> OnPlayerScoreUpdated;

        internal static CancellationTokenSource WebsocketCanceller { get; private set; }
        internal const int RecieveBufferSize = 1024;
        internal const int SendBufferSize = 16;

        private static ClientWebSocket webSocket;
        private static readonly AsyncLock listenerLock = new();

        static AccsaberLiveScores()
        {
            WebsocketCanceller = new();
            OnScoreUpdated += token =>
            {
                string id = AccsaberAPI.GetPlayerId(token);
                if (id is null || !id.Equals(Plugin.Instance.PlayerID)) return;
                OnPlayerScoreUpdated?.Invoke(token);
            };
        }

        public static async Task StartWebsocket(CancellationToken ct = default)
        {
            while (!ct.IsCancellationRequested)
            {
                webSocket = new();
                await ListenForScores(ct);
                if (!ct.IsCancellationRequested)
                {
                    await Task.Delay(2000);
                    Plugin.Log.Notice("Attempting to restart the websocket");
                }
            }
        }
        private static async Task ListenForScores(CancellationToken ct)
        {
            AsyncLock.Releaser? theLock = await listenerLock.TryLockAsync();
            if (theLock is null)
                return;
            using (theLock.Value)
            {
                await webSocket.ConnectAsync(new(HelpfulPaths.APAPI_WEBSOCKET), ct);
                try
                {
                    using MemoryStream ms = new();
                    WebSocketReceiveResult result;
                    while (webSocket.State == WebSocketState.Open)
                    {
                        do
                        {
                            ArraySegment<byte> clientBuffer = WebSocket.CreateClientBuffer(RecieveBufferSize, SendBufferSize);
                            result = await webSocket.ReceiveAsync(clientBuffer, ct);
                            if (result.MessageType == WebSocketMessageType.Close)
                            {
                                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", ct);
                                return;
                            }
                            ms.Write(clientBuffer.Array, clientBuffer.Offset, result.Count);
                        } 
                        while (!result.EndOfMessage);

                        if (result.MessageType == WebSocketMessageType.Text)
                            OnScoreUpdated?.Invoke(new(JObject.Parse(Encoding.UTF8.GetString(ms.ToArray()))));

                        ms.SetLength(0);
                        ms.Seek(0, SeekOrigin.Begin);
                        ms.Position = 0;
                    }
                }
                catch (WebSocketException)
                {
                    Plugin.Log.Info("The remote party has very rudely left us hanging (closed connect without handshake).");
                }
                catch (OperationCanceledException)
                {
                    Plugin.Log.Info("The websocket has been TERMINATED (cancel token invoked).");
                }
                catch (Exception e)
                {
                    Plugin.Log.Error("There was an error with the websocket!\n" + e);
                }
            }
        }
    }
}
