namespace PayloadLogQuery.Options;

public class AzureBlobOptions
{
    public string ContainerName { get; set; } = string.Empty;
    public string? StorageAccountName { get; set; }
    public string? KeyVaultName { get; set; }
}

