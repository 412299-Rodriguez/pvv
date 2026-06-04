namespace PvvConfig.Infrastructure.Settings;

public class EncryptionSettings
{
    /// <summary>32-character (256-bit) AES key.</summary>
    public string Key { get; set; } = string.Empty;
}
