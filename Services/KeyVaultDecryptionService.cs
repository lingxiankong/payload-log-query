using PayloadLogQuery.Abstractions;
using PayloadLogQuery.Options;

namespace PayloadLogQuery.Services;

public class KeyVaultDecryptionService : IDecryptionService
{
    // In this specific internal implementation, we don't actually need the KeyVaultClient
    // because the internal library wraps the complexity.
    // But we keep the class name as it implies the strategy used (or perhaps rename if 'KeyVault' is misleading,
    // but user didn't ask to rename). We will use the InternalLogCrypto as requested.

    public KeyVaultDecryptionService(AzureBlobOptions options)
    {
        // Options might be used if the internal library needs initialization,
        // but for the static method per requirement, we just call it.
    }

    public async Task<string> DecryptAsync(string encryptedMessage, CancellationToken ct = default)
    {
        // The requirement is to use the internal library: Task<string> DecryptLogMessage(string encryptedMessage)
        return await Utils.InternalLogCrypto.DecryptLogMessage(encryptedMessage);
    }
}
