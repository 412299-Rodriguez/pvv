namespace PvvConfig.Application.Interfaces;

public interface IEncryptionService
{
    /// <summary>AES-256-CBC encrypt; output is URL-safe base64.</summary>
    string Encrypt(string plainText);

    string Decrypt(string cipherText);
}
