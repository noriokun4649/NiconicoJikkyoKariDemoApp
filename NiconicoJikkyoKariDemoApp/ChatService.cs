using Dwango.Nicolive.Chat.Service.Edge;
using Google.Protobuf;
using System.IO;
using System.Net.Http;
using System.Text.Json;

namespace NiconicoJikkyoKariDemoApp
{
    public class ChatServiceUrlGenerator
    {
        private static readonly Dictionary<string, string> channels = new ()
        {
            { "NHK総合", "kl11" },
            { "NHK Eテレ", "kl12" },
            { "NHK BS", "kl13" },
            { "日本テレビ", "kl14" },
            { "テレビ朝日", "kl15" },
            { "TBSテレビ", "kl16" },
            { "テレビ東京", "kl17" },
            { "フジテレビ", "kl18" },
            { "TOKYO MX", "kl19" },
            { "BS11", "kl20" }
        };
        private const string chatApiBaseUrl = "https://mpn.live.nicovideo.jp/m1/api/v1/chat";

        public static string[] GetChannelList()
        {
            return [.. channels.Keys];
        }

        private static string GetChannelUrl(string channelName)
        {
            if (channels.TryGetValue(channelName, out var roomId))
            {
                return roomId;
            }
            throw new ArgumentException($"チャンネル見つからんよ");
        }

        private static string GenerateUrl(string channelName)
        {
            if (string.IsNullOrWhiteSpace(channelName))
            {
                throw new ArgumentException("チャンネル空白ダメよ", nameof(channelName));
            }
            var roomId = GetChannelUrl(channelName);
            return $"{chatApiBaseUrl}/{roomId}/view";
        }

        public static async Task<string> GetChatDataAsync(string channelName)
        {
            string url = GenerateUrl(channelName);

            using HttpClient client = new();
            HttpResponseMessage response = await client.GetAsync(url);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            using JsonDocument doc = JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("view", out JsonElement viewElement))
            {
                var viewUri = viewElement.GetString();
                if (viewUri != null)
                    return viewUri;
            };
            throw new InvalidOperationException("viewパラメータみつからんから無理よ");
        }
    }

    public class ChatService(string uri) : IDisposable
    {
        private readonly HttpClient httpClient = new();
        private readonly string uri = uri;
        private bool isDisposed = false;
        public event Action<ChunkedMessage> MessageReceived = delegate { };

        public async Task Connect(long? fromUnixTime = null)
        {
            fromUnixTime ??= DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long? next = fromUnixTime;

            while (next.HasValue && !isDisposed)
            {
                string fetchUri = $"{uri}?at={next.Value}";
                next = null;

                await foreach (var entry in Retrieve(fetchUri, reader =>
                    ChunkedEntry.Parser.ParseDelimitedFrom(reader)))
                {
                    if (entry.Segment != null)
                    {
                        await RetrieveMessages(entry.Segment.Uri);
                    }
                    else if (entry.Next != null)
                    {
                        next = entry.Next.At;
                    }
                }
            }
        }

        private async Task RetrieveMessages(string uri)
        {
            await foreach (var msg in Retrieve(uri, reader =>
                ChunkedMessage.Parser.ParseDelimitedFrom(reader)))
            {
                if (isDisposed) return;

                MessageReceived?.Invoke(msg);
                if (msg.State != null)
                {
                    UpdateState(msg);
                }
            }
        }

        private async IAsyncEnumerable<T> Retrieve<T>(string uri, Func<Stream, T> decoder)
        {
            var unread = new List<byte>();
            using var response = await httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            using var responseStream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(responseStream);
            var buffer = new byte[4096];

            while (true)
            {
                int bytesRead = await responseStream.ReadAsync(buffer);
                if (bytesRead == 0) break;

                unread.AddRange(buffer[..bytesRead]);
                using var memoryStream = new MemoryStream([.. unread]);
                List<T> messages = [];

                try
                {
                    while (memoryStream.Position < memoryStream.Length)
                    {
                        var msg = decoder(memoryStream);
                        messages.Add(msg);
                    }
                    unread.Clear();
                }
                catch (InvalidProtocolBufferException)
                {
                    //protobufが途中でちぎれていた場合RangeErrorになるので未読分をunreadにつめる
                    unread = unread.Skip((int)memoryStream.Position).ToList(); // Save unread part
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(ex.Message);
                }

                foreach (var msg in messages)
                {
                    yield return msg;
                }
            }
        }

        private void UpdateState(ChunkedMessage msg)
        {
            // 現在は不要だが、将来状態更新するときここに書く
        }


        public void Dispose()
        {
            GC.SuppressFinalize(this);
            isDisposed = true;
        }
    }


}
