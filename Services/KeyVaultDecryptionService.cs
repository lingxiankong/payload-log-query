using PayloadLogQuery.Abstractions;
using PayloadLogQuery.Options;

namespace PayloadLogQuery.Services;

public class KeyVaultDecryptionService : IDecryptionService
{
    private readonly string _keyVaultName;

    public KeyVaultDecryptionService(AzureBlobOptions options)
    {
        _keyVaultName = options.KeyVaultName ?? "unknown-kv";
    }

    public Task<string> DecryptAsync(string version, string cipherText, CancellationToken ct = default)
    {
        // TODO: Implement actual KeyVault decryption here.
        // 1. Create SecretClient using _keyVaultName and DefaultAzureCredential
        // 2. Get secret using 'version'
        // 3. Use secret to decrypt 'cipherText'

        return Task.FromResult($"[Encrypted via {_keyVaultName}] {cipherText}");
    }
}
