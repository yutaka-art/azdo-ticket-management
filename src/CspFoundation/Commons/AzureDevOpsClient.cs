using Microsoft.Extensions.Options;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using System.Collections.Generic;

namespace CspFoundation.Commons
{
    #region IAzureDevOpsClient
    /// <summary>
    /// AzureDevOpsClient インタフェース
    /// </summary>
    public interface IAzureDevOpsClient
    {
        Task<IReadOnlyList<WorkItem>> ExecuteSavedQueryAsync(Guid queryId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<WorkItem>> GetWorkItemsDetailsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default);
    }
    #endregion

    #region AzureDevOpsClient
    /// <summary>
    /// AzureDevOpsClient
    /// </summary>
    public class AzureDevOpsClient : IAzureDevOpsClient, IAsyncDisposable
    {
        #region Const
        private static readonly string[] FieldList =
        {
            "System.Id",
            "System.WorkItemType",
            "System.Title",
            "System.AssignedTo",
            "System.State",
            "System.Tags",
            "System.CreatedDate",
            "Microsoft.VSTS.Scheduling.DueDate"
        };
        #endregion

        #region Fields
        private readonly WorkItemTrackingHttpClient WitClient;
        private readonly ILogger<AzureDevOpsClient> Logger;
        private readonly SecretSettingsModel SecretSettings;
        private readonly AppSettingsModel AppSettings;
        #endregion

        #region Constructor
        public AzureDevOpsClient(
            WorkItemTrackingHttpClient witClient,
            IOptions<AppSettingsModel> optionsSettingsAccessor,
            IOptions<SecretSettingsModel> optionsKeyVaultAccessor,
            ILogger<AzureDevOpsClient> logger)
        {
            this.WitClient = witClient;
            this.AppSettings = optionsSettingsAccessor.Value;
            this.SecretSettings = optionsKeyVaultAccessor.Value;
            this.Logger = logger;
        }
        #endregion

        #region Public Methods
        public async Task<IReadOnlyList<WorkItem>> ExecuteSavedQueryAsync(Guid queryId, CancellationToken cancellationToken = default)
        {
            var result = await this.WitClient.QueryByIdAsync(queryId, cancellationToken: cancellationToken);

            var ids = result.WorkItems.Select(w => w.Id).ToArray();
            if (ids.Length == 0) return Array.Empty<WorkItem>();

            return await this.WitClient.GetWorkItemsAsync(
                ids,
                fields: null,
                asOf: null,
                expand: WorkItemExpand.None,
                cancellationToken: cancellationToken);
        }

        public async Task<IReadOnlyList<WorkItem>> GetWorkItemsDetailsAsync(
            IEnumerable<int> ids,
            CancellationToken cancellationToken = default)
        {
            var result = new List<WorkItem>();

            var array = ids?.ToArray() ?? Array.Empty<int>();
            if (array.Length == 0) { return result; }

            result = await WitClient.GetWorkItemsAsync(
                array,
                fields: FieldList,
                asOf: null,
                expand: WorkItemExpand.None,
                errorPolicy: null,
                cancellationToken: cancellationToken);

            return result;
        }
        #endregion

        #region Private Methods
        #endregion

        #region Dispose
        // WorkItemTrackingHttpClient は IDisposable のみ実装
        public ValueTask DisposeAsync()
        {
            this.WitClient.Dispose();
            return ValueTask.CompletedTask;
        }
        #endregion
    }
    #endregion
}
