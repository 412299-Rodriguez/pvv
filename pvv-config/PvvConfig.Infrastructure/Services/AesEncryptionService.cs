using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using PvvConfig.Application.Interfaces;
using PvvConfig.Infrastructure.Settings;

namespace PvvConfig.Infrastructure.Services;

/// <summary>
/// AES-256-CBC encryption. A random IV is generated per call and prepended to the
/// ciphertext; the result is URL-safe base64.
/// </summary>
public class AesEncryptionService : IEncryptionService
{
    private readonly byte[] _key;

    public AesEncryptionService(IOptions<EncryptionSettings> options)
    {
        _key = Encoding.UTF8.GetBytes(options.Value.Key);
        if (_key.Length != 32)
        {
            throw new InvalidOperationException(
                "EncryptionSettings:Key must be exactly 32 bytes (256 bits).");
        }
    }

    public string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        var result = new byte[aes.IV.Length + cipherBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);

        return ToBase64Url(result);
    }

    public string Decrypt(string cipherText)
    {
        var bytes = FromBase64Url(cipherText);

        using var aes = Aes.Create();
        aes.Key = _key;

        var iv = new byte[16];
        Buffer.BlockCopy(bytes, 0, iv, 0, iv.Length);
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        var plainBytes = decryptor.TransformFinalBlock(bytes, iv.Length, bytes.Length - iv.Length);
        return Encoding.UTF8.GetString(plainBytes);
    }

    private static string ToBase64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] FromBase64Url(string value)
    {
        var base64 = value.Replace('-', '+').Replace('_', '/');
        base64 = (base64.Length % 4) switch
        {
            2 => base64 + "==",
            3 => base64 + "=",
            _ => base64
        };
        return Convert.FromBase64String(base64);
    }
}
