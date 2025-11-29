namespace PayloadLogQuery.Options;

public class AzureBlobOptions
{
    public string? ConnectionString { get; set; }
    public string ContainerName { get; set; } = string.Empty;
}

