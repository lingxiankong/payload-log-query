using PayloadLogQuery.Abstractions;

namespace PayloadLogQuery.Services;

public class LocalDecryptionService : IDecryptionService
{
    public Task<string> DecryptAsync(string version, string cipherText, CancellationToken ct = default)
    {
        // For local dev, just return the content as-is (simulating that we successfully "read" it).
        // User requested no fake "Decrypted" prefix.
        return Task.FromResult(cipherText);
    }
}
