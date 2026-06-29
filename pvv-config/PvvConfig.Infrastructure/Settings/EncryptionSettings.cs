namespace PvvConfig.Infrastructure.Settings;

public class EncryptionSettings
{
    /// <summary>32-character (256-bit) AES key.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>16-character (128-bit) AES IV. Fixed so the hashed tokens are deterministic.</summary>
    public string Iv { get; set; } = string.Empty;
}
