using System.Security.Cryptography;
using System.Text;

namespace Youtube_Downloader;

/// <summary>
/// Provides encryption and decryption for sensitive cookie data using Windows DPAPI.
/// Data is encrypted per-user, per-machine - only the same Windows user on the same
/// machine can decrypt the data.
/// </summary>
public static class CookieEncryption
{
    /// <summary>
    /// Encrypts a string using Windows DPAPI (Data Protection API).
    /// </summary>
    /// <param name="plainText">The text to encrypt</param>
    /// <returns>Base64-encoded encrypted data, or empty string if input is empty</returns>
    public static string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return "";

        try
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] encryptedBytes = ProtectedData.Protect(
                plainBytes,
                null, // Optional entropy (additional secret)
                DataProtectionScope.CurrentUser // Only current Windows user can decrypt
            );
            return Convert.ToBase64String(encryptedBytes);
        }
        catch
        {
            return "";
        }
    }

    /// <summary>
    /// Decrypts a Base64-encoded string that was encrypted with Encrypt().
    /// </summary>
    /// <param name="encryptedText">Base64-encoded encrypted data</param>
    /// <returns>The original plain text, or empty string if decryption fails</returns>
    public static string Decrypt(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
            return "";

        try
        {
            byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
            byte[] plainBytes = ProtectedData.Unprotect(
                encryptedBytes,
                null, // Must match entropy used during encryption
                DataProtectionScope.CurrentUser
            );
            return Encoding.UTF8.GetString(plainBytes);
        }
        catch
        {
            // Decryption failed - likely different user or corrupted data
            return "";
        }
    }
}
