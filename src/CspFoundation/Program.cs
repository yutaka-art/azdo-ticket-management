using Azure.Identity;
using CspFoundation.Commons;
using CspFoundation.Services;
using Dapr.Client;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Options;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Newtonsoft.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddApplicationInsights(); // Application Insightsログを追加

builder.Services.AddApplicationInsightsTelemetry();

// appsettings の読み込み
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}
else
{
    // ☆KeyVaultを作成していない場合はコメントアウト
    // ※1.KeyVaultを作成しMSIを有効化、アクセスポリシーにAzure FunctionsのアプリケーションIDを追加
    // ※2.環境変数にKeyVaultのURLを設定
    builder.Configuration.AddAzureKeyVault(
        new Uri(SettingsProvider.KEY_VAULT_URL),
        new DefaultAzureCredential());
}

// 依存関係の登録
builder.Services.AddControllers();
builder.Services.AddSingleton<DaprClient>(_ => new DaprClientBuilder().Build());
builder.Services.AddTransient<IStorageProvider, StorageProvider>();

// Azure クライアント登録
builder.Services.AddAzureClients(builder =>
{
    // Blobサービスクライアントの設定
    builder.AddBlobServiceClient(SettingsProvider.STORAGE_CONNECT_STRING)
    .ConfigureOptions(options => {
        options.Retry.Mode = Azure.Core.RetryMode.Exponential;
        options.Retry.MaxRetries = 5;
        options.Retry.MaxDelay = TimeSpan.FromSeconds(120);
    });

    // ファイルサービスクライアントの設定
    builder.AddFileServiceClient(SettingsProvider.STORAGE_CONNECT_STRING)
    .ConfigureOptions(options =>
    {
        options.Retry.Mode = Azure.Core.RetryMode.Exponential;
        options.Retry.MaxRetries = 5;
        options.Retry.MaxDelay = TimeSpan.FromSeconds(120);
    });
});

builder.Services.AddHttpClient();
builder.Services.AddTransient<IAzureDevOpsClient, AzureDevOpsClient>();

builder.Services.AddSingleton<IMainService, MainService>();

// Newtonsoft.JsonSetting の既定設定を 1 箇所で共有
builder.Services.Configure<JsonSerializerSettings>(opts =>
{
    opts.NullValueHandling = NullValueHandling.Ignore;
    opts.DateFormatHandling = DateFormatHandling.IsoDateFormat;
    opts.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
    // 追加設定があればここで
});

builder.Services
    .AddHttpClient<IWebhookClient, WebhookClient>("logicapps")
    .ConfigureHttpClient(c =>
    {
        c.Timeout = TimeSpan.FromSeconds(20);
        // UserAgent や他ヘッダーは WebhookClient の ctor 内でも追加しているが
        // ここで上書きしたい場合は記載
    });

builder.Services.Configure<AppSettingsModel>(
    builder.Configuration.GetSection("CspFoundation")
);
builder.Services.Configure<SecretSettingsModel>(
    builder.Configuration.GetSection("CspFoundation")
);

builder.Services.AddSingleton<WorkItemTrackingHttpClient>(sp =>
{
    var secret = sp.GetRequiredService<IOptions<SecretSettingsModel>>().Value;

    var creds = new VssBasicCredential(string.Empty, secret.PersonalAccessToken);
    var orgUri = new Uri($"{secret.AzdoBaseUrl}/{secret.OrganizatonName}");
    var conn = new VssConnection(orgUri, creds);

    return conn.GetClient<WorkItemTrackingHttpClient>();
});

// AzureDevOpsClient ラッパー
builder.Services.AddTransient<IAzureDevOpsClient, AzureDevOpsClient>();

// アプリケーションのビルドと起動
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// ルーティング設定
app.UseRouting();
app.UseAuthorization();
app.MapControllers();

app.Run();
