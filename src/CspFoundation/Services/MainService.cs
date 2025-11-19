using CspFoundation.Commons;
using Dapr.Client;
using HtmlAgilityPack;
using Microsoft.Extensions.Options;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace CspFoundation.Services
{
    #region IMainService
    public interface IMainService
    {
        Task ExecuteAsync(string? receiveData);
        Task ExecuteOnCronAsync();
    }
    #endregion

    #region MainService
    public class MainService : IMainService
    {
        #region Variable・Const
        private readonly DaprClient DaprClient;
        private readonly ILogger<MainService> Logger;
        private readonly IStorageProvider StorageProvider;
        private readonly AppSettingsModel AppSettings;
        private readonly SecretSettingsModel SecretSettings;
        private readonly IWebhookClient WebhookClient;
        private readonly IAzureDevOpsClient AzureDevOpsClient;
        private readonly IWebHostEnvironment Env;
        #endregion

        #region Constructor
        public MainService(
            DaprClient daprClient,
            ILogger<MainService> logger,
            IStorageProvider storageProvider,
            IOptions<AppSettingsModel> appSettings,
            IOptions<SecretSettingsModel> secretSettings,
            IWebhookClient webhookClient,
            IAzureDevOpsClient azureDevOpsClient,
            IWebHostEnvironment env)
        {
            this.Logger = logger;
            this.AppSettings = appSettings.Value;
            this.SecretSettings = secretSettings.Value;
            this.DaprClient = daprClient;
            this.StorageProvider = storageProvider;
            this.WebhookClient = webhookClient;
            this.AzureDevOpsClient = azureDevOpsClient;
            this.Env = env;
        }
        #endregion

        #region Method
        #region ExecuteAsync
        public async Task ExecuteAsync(string? receiveData)
        {
            this.Logger.LogInformation("Method:{Method};Status:Start;", MethodHelper.GetCurrentMethod());

            var jsonText = ExtractJsonObject(receiveData);
            if (jsonText is null)
            {
                this.Logger.LogWarning("Invalid JSON structure.");
                return;
            }

            var src = JObject.Parse(jsonText);
            var eventType = src["eventType"]?.ToString();

            string? cardJson;
            switch (eventType)
            {
                case "workitem.created":
                    cardJson = CreateWorkitemCreatedCard(src);
                    break;
                case "workitem.updated":
                    cardJson = TryCreateWorkitemUpdatedCard(src);
                    break;
                case "workitem.deleted":
                    cardJson = null;
                    break;
                case "workitem.restored":
                    cardJson = null;
                    break;
                default:
                    cardJson = null;
                    break;
            }

            if (string.IsNullOrEmpty(cardJson))
            {
                this.Logger.LogInformation("No card generated for eventType={EventType}", eventType);
                return;
            }

            await this.WebhookClient.SendAsync(
                new Uri(this.SecretSettings.LogicAppsEndPointUrl), cardJson);

            this.Logger.LogInformation("Method:{Method};Status:End;", MethodHelper.GetCurrentMethod());

        }

        #region Card Generators
        private string CreateWorkitemCreatedCard(JObject src)
        {
            var (display, email) = ParseUser(
                src["resource"]?["fields"]?["System.AssignedTo"]?.ToString());

            var payload = new
            {
                id = src["resource"]?["id"]?.ToString(),
                assignedToDisplay = display,
                assignedToEmail = email,
                inquiryTopic = src["resource"]?["fields"]?["Custom.InquiryTopic"]?.ToString() ?? "(no topic)",
                companyName = src["resource"]?["fields"]?["Custom.CompanyName"]?.ToString() ?? "(no company)",
                organizationName = src["resource"]?["fields"]?["Custom.OrganizationName"]?.ToString() ?? "(no org)",
                contactName = src["resource"]?["fields"]?["Custom.ContactName"]?.ToString() ?? "(no contact)",
                description = HtmlToPlain(
                                       src["resource"]?["fields"]?["System.Description"]?.ToString() ?? string.Empty)
            };

            var templateJson = LoadTemplate("workitem-created-card.json");
            var template = new AdaptiveCards.Templating.AdaptiveCardTemplate(templateJson);
            return template.Expand(payload);
        }

        private string? TryCreateWorkitemUpdatedCard(JObject src)
        {
            var assignedToken = src["resource"]?["fields"]?["System.AssignedTo"];
            if (assignedToken is null) return null;

            var oldRaw = assignedToken["oldValue"]?.ToString();
            var newRaw = assignedToken["newValue"]?.ToString();
            if (string.Equals(oldRaw, newRaw, StringComparison.OrdinalIgnoreCase))
                return null; // 担当者変更なし

            var (oldDisplay, _) = ParseUser(oldRaw);
            var (newDisplay, newEmail) = ParseUser(newRaw);

            var payload = new
            {
                AzdoBaseUrl = this.SecretSettings.AzdoBaseUrl,
                AzdoOrganizationName = this.SecretSettings.OrganizatonName,
                AzdoProjectName = this.SecretSettings.ProjectName,
                id = src["resource"]?["workItemId"]?.ToString()
                                   ?? src["resource"]?["id"]?.ToString(),
                oldDisplay,
                newDisplay,
                newEmail,
                companyName = src["resource"]?["revision"]?["fields"]?["Custom.CompanyName"]?.ToString()
                                   ?? "(no company)",
                organizationName = src["resource"]?["revision"]?["fields"]?["Custom.OrganizationName"]?.ToString()
                                   ?? "(no org)",
                contactName = src["resource"]?["revision"]?["fields"]?["Custom.ContactName"]?.ToString()
                                   ?? "(no contact)",
                changedBy = src["resource"]?["revisedBy"]?["displayName"]?.ToString()
                                   ?? "(unknown)",
                description = HtmlToPlain(
                                       src["resource"]?["revision"]?["fields"]?["System.Description"]?.ToString() ?? string.Empty)

            };

            var templateJson = LoadTemplate("workitem-updated-card.json");
            var template = new AdaptiveCards.Templating.AdaptiveCardTemplate(templateJson);
            return template.Expand(payload);
        }
        #endregion

        private (string display, string email) ParseUser(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return ("(unassigned)", "(unassigned)");

            var m = Regex.Match(raw, @"^(.*?)\s*<([^>]+)>$");
            return m.Success
                ? (m.Groups[1].Value, m.Groups[2].Value)
                : (raw, raw);
        }

        private string HtmlToPlain(string html)
        {
            if (string.IsNullOrEmpty(html)) return string.Empty;

            // <br> → \n
            string tmp = Regex.Replace(html, @"<br\s*/?>", "\n",
                                       RegexOptions.IgnoreCase);
            var doc = new HtmlDocument();
            doc.LoadHtml(tmp);
            return WebUtility.HtmlDecode(doc.DocumentNode.InnerText).Trim();
        }

        /// 受信ボディから最初の JSON オブジェクト部分だけ抜き出す
        private string? ExtractJsonObject(string? input)
        {
            if (string.IsNullOrEmpty(input)) return null;
            int s = input.IndexOf('{'); if (s == -1) return null;
            int depth = 0;
            for (int i = s; i < input.Length; i++)
            {
                if (input[i] == '{') depth++;
                else if (input[i] == '}') depth--;
                if (depth == 0) return input.Substring(s, i - s + 1);
            }
            return null;
        }
        #endregion

        #region ExecuteOnCronAsync
        public async Task ExecuteOnCronAsync()
        {
            this.Logger.LogInformation("Method:{Method};Message:-;Status:Start;", MethodHelper.GetCurrentMethod());

            var queryResult = await this.AzureDevOpsClient.ExecuteSavedQueryAsync(new Guid(this.SecretSettings.WiqlId));
            var idList = queryResult.Select(x => x.Id).Where(id => id.HasValue).Select(id => id.Value).ToArray();

            var workItems = await this.AzureDevOpsClient.GetWorkItemsDetailsAsync(idList);
            var payload = CreateStaleItemsCard(workItems);

            await this.WebhookClient.SendAsync(new Uri(this.SecretSettings.LogicAppsEndPointUrl), payload);

            this.Logger.LogInformation("Method:{Method};Message:-;Status:End;", MethodHelper.GetCurrentMethod());
        }

        private string CreateStaleItemsCard(IReadOnlyList<WorkItem> items)
        {
            //var templateJson = System.IO.File.ReadAllText("Templates/stale-items-card.json");
            var templateJson = LoadTemplate("stale-items-card.json");

            var template = new AdaptiveCards.Templating.AdaptiveCardTemplate(templateJson);

            var payload = new
            {
                AzdoBaseUrl = this.SecretSettings.AzdoBaseUrl,
                AzdoOrganizationName = this.SecretSettings.OrganizatonName,
                AzdoProjectName = this.SecretSettings.ProjectName,
                itemCount = items.Count,
                items = items.Select(i =>
                {
                    i.Fields.TryGetValue("System.Id", out var idObj);
                    var id = idObj ?? "(no id)";

                    i.Fields.TryGetValue("System.Title", out var titleObj);
                    var title = titleObj ?? "(no title)";

                    // ------- AssignedTo (IdentityRef) -------
                    string assignedTo = "(unassigned)";
                    if (i.Fields.TryGetValue("System.AssignedTo", out var assigneeObj) &&
                        assigneeObj is IdentityRef identity)
                    {
                        // DisplayName が無ければ UniqueName を fallback
                        assignedTo = identity.DisplayName ?? identity.UniqueName ?? assignedTo;
                    }

                    i.Fields.TryGetValue("System.State", out var stateObj);
                    var state = stateObj ?? "(no state)";

                    string dueDate = "(no due date)";
                    if (i.Fields.TryGetValue("Microsoft.VSTS.Scheduling.DueDate", out var dueObj))
                    {
                        switch (dueObj)
                        {
                            case DateTime dt:
                                dueDate = dt.ToString("yyyy-MM-dd");
                                break;
                            case DateTimeOffset dto:
                                dueDate = dto.ToString("yyyy-MM-dd");
                                break;
                            case string s when DateTime.TryParse(s, out var parsed):
                                dueDate = parsed.ToString("yyyy-MM-dd");
                                break;
                        }
                    }

                    return new
                    {
                        id,
                        title,
                        assignedTo,
                        state,
                        dueDate
                    };
                })
                .OrderBy(i => i.dueDate)
                .ToList()
            };
            return template.Expand(payload);
        }
        #endregion
        #endregion

        #region Helpers
        private string LoadTemplate(string fileName)
            => File.ReadAllText(
                Path.Combine(this.Env.ContentRootPath, "_Templates", fileName));
        #endregion
    }
    #endregion
}
