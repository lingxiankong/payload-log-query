namespace PayloadLogQuery.Utils;

public static class InternalLogCrypto
{
    private static readonly string _fakeKey = "internal-secret-key";

    public static Task<string> DecryptLogMessage(string encryptedMessage)
    {
        // Simulation: in a real scenario, this uses an internal key to decrypt.
        // We will just return the message prefixed with [Decrypted] to prove the flow works.
        // If the message is "v1:ciphertext", we pretend to decrypt "ciphertext".

        // Mock logic:
        return Task.FromResult($"[Decrypted Content]: {encryptedMessage}");
    }
}
