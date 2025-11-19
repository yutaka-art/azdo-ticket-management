using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Net;
using System.Text;

namespace CspFoundation.Commons
{
    /// <summary>任意のペイロードを JSON で POST する汎用クライアント。</summary>
    public interface IWebhookClient
    {
        Task SendAsync<T>(Uri endpoint, T payload, CancellationToken ct = default);
    }
    public sealed class WebhookClient : IWebhookClient
    {
        private readonly HttpClient HttpClient;
        private readonly JsonSerializerSettings JsonSetting;
        private readonly ILogger<WebhookClient> Logger;

        public WebhookClient(
            HttpClient httpClient,
            IOptions<JsonSerializerSettings> jsonOptions,
            ILogger<WebhookClient> logger)
        {
            this.HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            this.JsonSetting = jsonOptions.Value ?? new JsonSerializerSettings();
            this.Logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // 共通ヘッダー（必要に応じてカスタマイズ）
            this.HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("CspFoundation/1.0");
            this.HttpClient.DefaultRequestHeaders.Accept.TryParseAdd("application/json");
        }

        /// <inheritdoc/>
        public async Task SendAsync<T>(
            Uri endpoint,
            T payload,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(endpoint);
            ArgumentNullException.ThrowIfNull(payload);

            var json = JsonConvert.SerializeObject(payload, JsonSetting);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            this.Logger.LogDebug("POST {Endpoint} : {JsonSetting}", endpoint, json);

            using var resp = await this.HttpClient.PostAsync(endpoint, content, ct);

            if (resp.IsSuccessStatusCode)
            {
                this.Logger.LogInformation("Webhook posted: {Status} {Reason}", (int)resp.StatusCode, resp.ReasonPhrase);
                return;
            }

            // 失敗: 内容を読み込んで詳細付き例外
            var body = await resp.Content.ReadAsStringAsync(ct);
            this.Logger.LogWarning("Webhook failed: {Status} {Body}", (int)resp.StatusCode, body);

            throw new WebhookException(
                $"POST {endpoint} failed with {(int)resp.StatusCode} {resp.ReasonPhrase}",
                resp.StatusCode,
                body);
        }
    }

    /// <summary>Webhook 送信失敗時にスローされる例外。</summary>
    public sealed class WebhookException : Exception
    {
        public HttpStatusCode StatusCode { get; }
        public string? RawBody { get; }

        public WebhookException(string message, HttpStatusCode status, string? rawBody)
            : base(message)
        {
            StatusCode = status;
            RawBody = rawBody;
        }
    }
}
