using PayloadLogQuery.Abstractions;

namespace PayloadLogQuery.Services;

public class LocalDecryptionService : IDecryptionService
{
    public Task<string> DecryptAsync(string encryptedMessage, CancellationToken ct = default)
    {
        // For local dev, return as is.
        return Task.FromResult(encryptedMessage);
    }
}
