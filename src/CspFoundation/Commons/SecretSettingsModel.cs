namespace CspFoundation.Commons
{
    public class SecretSettingsModel
    {
        // **AzureDevOps >>>**
        public string? OrganizatonName { get; set; }
        public string? ProjectName { get; set; }
        public string? PersonalAccessToken { get; set; }
        public string? AzdoBaseUrl { get; set; }
        public string? WiqlId { get; set; }
        // **AzureDevOps <<<**

        // **Azure >>>**
        public string? LogicAppsEndPointUrl { get; set; }
        // **Azure <<<**
    }
}
