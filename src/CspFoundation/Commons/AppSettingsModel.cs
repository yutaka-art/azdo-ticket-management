namespace CspFoundation.Commons
{
    public class AppSettingsModel
    {
        public int MaxRetries { get; set; } = 5;
        public int DelayMilliseconds { get; set; } = 1000;
        public string? FileShareName { get; set; }
        public string? BlobContainerName { get; set; }
    }
}
