namespace PayloadLogQuery.Abstractions;

public interface IDecryptionService
{
    Task<string> DecryptAsync(string encryptedMessage, CancellationToken ct = default);
}
