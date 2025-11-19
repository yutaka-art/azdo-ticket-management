using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Microsoft.Extensions.Logging;

namespace CspFoundation.Commons
{
    #region IStorageProvider
    public interface IStorageProvider
    {
        Task<bool> IsDirectoryShareAsync(string shareName, string filePath);

        Task<bool> IsFileShareAsync(string shareName, string filePath);

        Task<ShareFileProperties> GetFileAttributeShareAsync(string shareName, string filePath);

        Task<byte[]> GetFileFromShareAsync(string shareName, string filePath);

        Task<Stream> GetFileFromShareToStreamAsync(string shareName, string filePath);

        Task UploadFileToShareAsync(string shareName, string filePath, Stream stream);

        Task DeleteFileInShareAsync(string shareName, string filePath);

        Task<bool> IsFileBlobAsync(string containerName, string filePath);

        Task<BlobItem> GetFileAttributeBlobAsync(string containerName, string filePath);

        Task<byte[]> GetFileFromBlobAsync(string containerName, string filePath);

        Task<Stream> GetFileFromBlobToStreamAsync(string containerName, string filePath);

        Task<string> UploadFileToBlobAsync(string containerName, string filePath, byte[] fileByte);

        Task DeleteFileInBlobAsync(string containerName, string filePath);

        Task<List<string>> GetFilePathBlobAsync(string containerName, string filePath);

        Task DeleteOldBackupsAsync(string containerName, int daysThreshold);
    }
    #endregion

    #region StorageProvider
    public class StorageProvider : IStorageProvider
    {
        #region Private
        private readonly ILogger<StorageProvider> Logger;
        private ShareServiceClient ShareServiceClient { get; set; }
        private BlobServiceClient BlobServiceClient { get; set; }
        #endregion

        #region Constructor
        public StorageProvider(BlobServiceClient blobServiceClient,
            ShareServiceClient shareServiceClient,
            ILogger<StorageProvider> logger
            ) : base()
        {

            if (blobServiceClient == null)
                throw new ArgumentNullException(nameof(blobServiceClient));
            if (shareServiceClient == null)
                throw new ArgumentNullException(nameof(shareServiceClient));
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            BlobServiceClient = blobServiceClient;
            ShareServiceClient = shareServiceClient;
            Logger = logger;
        }
        #endregion

        #region FileStorage
        public async Task<bool> IsDirectoryShareAsync(string shareName, string filePath)
        {
            var result = false;

            var shareClient = ShareServiceClient.GetShareClient(shareName);
            var directoryClient = shareClient.GetDirectoryClient(filePath);

            result = await directoryClient.ExistsAsync();

            return result;
        }

        public async Task<bool> IsFileShareAsync(string shareName, string filePath)
        {
            var result = false;

            var wkDirectoryPath = Path.GetDirectoryName(filePath);
            var wkFileName = Path.GetFileName(filePath);

            var shareClient = ShareServiceClient.GetShareClient(shareName);
            var directoryClient = shareClient.GetDirectoryClient(wkDirectoryPath);

            var fileClient = directoryClient.GetFileClient(wkFileName);
            result = await fileClient.ExistsAsync();

            return result;
        }

        public async Task<ShareFileProperties> GetFileAttributeShareAsync(string shareName, string filePath)
        {
            ShareFileProperties result = null;

            var wkDirectoryPath = Path.GetDirectoryName(filePath);
            var wkFileName = Path.GetFileName(filePath);

            var shareClient = ShareServiceClient.GetShareClient(shareName);
            var directoryClient = shareClient.GetDirectoryClient(wkDirectoryPath);

            var fileClient = directoryClient.GetFileClient(wkFileName);
            result = await fileClient.GetPropertiesAsync();

            return result;
        }

        public async Task<byte[]> GetFileFromShareAsync(string shareName, string filePath)
        {
            var wkDirectoryPath = Path.GetDirectoryName(filePath);
            var wkFileName = Path.GetFileName(filePath);

            var shareClient = ShareServiceClient.GetShareClient(shareName);
            var directoryClient = shareClient.GetDirectoryClient(wkDirectoryPath);

            var fileClient = directoryClient.GetFileClient(wkFileName);

            ShareFileDownloadInfo download = await fileClient.DownloadAsync();
            using (var stream = new MemoryStream())
            {
                await download.Content.CopyToAsync(stream);
                return stream.ToArray();
            }
        }

        public async Task<Stream> GetFileFromShareToStreamAsync(string shareName, string filePath)
        {
            var wkDirectoryPath = Path.GetDirectoryName(filePath);
            var wkFileName = Path.GetFileName(filePath);

            var shareClient = ShareServiceClient.GetShareClient(shareName);
            var directoryClient = shareClient.GetDirectoryClient(wkDirectoryPath);

            var fileClient = directoryClient.GetFileClient(wkFileName);

            ShareFileDownloadInfo download = await fileClient.DownloadAsync();
            var stream = new MemoryStream();

            await download.Content.CopyToAsync(stream);
            stream.Position = 0;

            return stream;
        }

        public async Task UploadFileToShareAsync(string shareName, string filePath, Stream stream)
        {
            var wkDirectoryPath = Path.GetDirectoryName(filePath);
            var wkFileName = Path.GetFileName(filePath);

            var shareClient = ShareServiceClient.GetShareClient(shareName);

            var directoryClient = shareClient.GetDirectoryClient(wkDirectoryPath);
            await directoryClient.CreateIfNotExistsAsync();

            var fileClient = directoryClient.GetFileClient(wkFileName);

            await fileClient.DeleteIfExistsAsync();
            await fileClient.CreateAsync(stream.Length);

            await fileClient.UploadRangeAsync(new Azure.HttpRange(0, stream.Length), stream);
        }

        public async Task DeleteFileInShareAsync(string shareName, string filePath)
        {
            var wkDirectoryPath = Path.GetDirectoryName(filePath);
            var wkFileName = Path.GetFileName(filePath);

            var shareClient = ShareServiceClient.GetShareClient(shareName);
            var directoryClient = shareClient.GetDirectoryClient(wkDirectoryPath);

            var fileClient = directoryClient.GetFileClient(wkFileName);
            await fileClient.DeleteIfExistsAsync();
        }
        #endregion

        #region BlobStorage
        public async Task<bool> IsFileBlobAsync(string containerName, string filePath)
        {
            bool result = false;

            var blobContainerClient = BlobServiceClient.GetBlobContainerClient(containerName);
            if (!await blobContainerClient.ExistsAsync())
            {
                throw new DirectoryNotFoundException($"Container not found. container:{containerName}; file path:{filePath}");
            }

            var blobs = blobContainerClient.GetBlobsAsync(prefix: filePath);

            await foreach (var blobItem in blobs)
            {
                result = true;
            }

            return result;
        }

        public async Task<BlobItem> GetFileAttributeBlobAsync(string containerName, string filePath)
        {
            BlobItem result = null;

            var blobContainerClient = BlobServiceClient.GetBlobContainerClient(containerName);
            if (!await blobContainerClient.ExistsAsync())
            {
                throw new DirectoryNotFoundException($"Container not found. container:{containerName}; file path:{filePath}");
            }

            var blobs = blobContainerClient.GetBlobsAsync(prefix: filePath);

            await foreach (var blobItem in blobs)
            {
                result = blobItem;
            }

            return result;
        }

        public async Task<byte[]> GetFileFromBlobAsync(string containerName, string filePath)
        {
            var blobContainerClient = BlobServiceClient.GetBlobContainerClient(containerName);
            if (!await blobContainerClient.ExistsAsync())
            {
                throw new DirectoryNotFoundException($"Container not found. container:{containerName}; file path:{filePath}");
            }

            var blobClient = blobContainerClient.GetBlobClient(filePath);
            if (!await blobClient.ExistsAsync())
            {
                throw new FileNotFoundException($"File not found. container:{containerName}; file path:{filePath}");
            }

            using (var stream = new MemoryStream())
            {
                await blobClient.DownloadToAsync(stream);
                return stream.ToArray();
            }
        }

        public async Task<Stream> GetFileFromBlobToStreamAsync(string containerName, string filePath)
        {
            var blobContainerClient = BlobServiceClient.GetBlobContainerClient(containerName);
            if (!await blobContainerClient.ExistsAsync())
            {
                throw new DirectoryNotFoundException($"Container not found. container:{containerName}; file path:{filePath}");
            }

            var blobClient = blobContainerClient.GetBlobClient(filePath);
            if (!await blobClient.ExistsAsync())
            {
                throw new FileNotFoundException($"File not found. container:{containerName}; file path:{filePath}");
            }

            var stream = new MemoryStream();

            await blobClient.DownloadToAsync(stream);
            stream.Position = 0;

            return stream;
        }

        public async Task<string> UploadFileToBlobAsync(string containerName, string filePath, byte[] fileByte)
        {
            var blobContainerClient = BlobServiceClient.GetBlobContainerClient(containerName);
            if (!await blobContainerClient.ExistsAsync())
            {
                throw new DirectoryNotFoundException($"Container not found. container:{containerName}; file path:{filePath}");
            }

            var blobClient = blobContainerClient.GetBlobClient(filePath);
            await blobClient.DeleteIfExistsAsync();

            var stream = new MemoryStream(fileByte);
            await blobClient.UploadAsync(stream);

            return blobClient.Uri.ToString();
        }

        public async Task DeleteFileInBlobAsync(string containerName, string filePath)
        {
            var blobContainerClient = BlobServiceClient.GetBlobContainerClient(containerName);
            if (!await blobContainerClient.ExistsAsync())
            {
                throw new DirectoryNotFoundException($"Container not found. container:{containerName}; file path:{filePath}");
            }

            var blobClient = blobContainerClient.GetBlobClient(filePath);
            await blobClient.DeleteIfExistsAsync();
        }

        public async Task<List<string>> GetFilePathBlobAsync(string containerName, string filePath)
        {
            var wkResult = new List<string>();
            var blobContainerClient = BlobServiceClient.GetBlobContainerClient(containerName);
            if (!await blobContainerClient.ExistsAsync())
            {
                throw new DirectoryNotFoundException($"Container not found. container:{containerName}; file path:{filePath}");
            }

            var blobs = blobContainerClient.GetBlobsAsync(prefix: filePath);
            await foreach (var blobItem in blobs)
            {
                wkResult.Add(blobItem.Name);
            }

            return wkResult;
        }

        public async Task DeleteOldBackupsAsync(string containerName, int daysThreshold)
        {
            var cutoffDate = DateTime.UtcNow.Date.AddDays(-daysThreshold);
            this.Logger.LogInformation("Deleting backups older than {CutoffDate}", cutoffDate.ToString("yyyy-MM-dd"));

            var allBlobs = await GetFilePathBlobAsync(containerName, "");
            var groupedByPrefix = allBlobs
                .Where(name => name.Contains("/")) // フォルダ構造が前提
                .GroupBy(name => name.Split('/')[0]) // yyyy-MM-dd 部分でグループ化
                .ToList();

            foreach (var group in groupedByPrefix)
            {
                if (DateTime.TryParse(group.Key, out DateTime folderDate))
                {
                    if (folderDate < cutoffDate)
                    {
                        this.Logger.LogInformation("Deleting folder: {Folder}", group.Key);
                        foreach (var blobName in group)
                        {
                            try
                            {
                                await DeleteFileInBlobAsync(containerName, blobName);
                                this.Logger.LogInformation("Deleted: {BlobName}", blobName);
                            }
                            catch (Exception ex)
                            {
                                this.Logger.LogWarning(ex, $"Failed to delete blob: {blobName}");
                            }
                        }
                    }
                }
            }
        }
        #endregion
    }
    #endregion
}
