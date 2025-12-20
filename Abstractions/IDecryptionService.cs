namespace PayloadLogQuery.Abstractions;

public interface IDecryptionService
{
    Task<string> DecryptAsync(string version, string cipherText, CancellationToken ct = default);
}
