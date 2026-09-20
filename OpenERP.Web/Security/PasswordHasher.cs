/*
 * File: OpenERP.Web/Security/PasswordHasher.cs
 * Description: PBKDF2 密码哈希工具（不可逆加密，用于员工登录密码安全存储）。
 */

using System.Security.Cryptography;
using System.Text;

namespace OpenERP.Web.Security;

/// <summary>
/// 密码哈希工具（使用 PBKDF2-HMAC-SHA256 算法，盐值随机生成，不可逆）。
/// </summary>
public static class PasswordHasher
{
    /// <summary>
    /// 盐值长度（字节）。
    /// </summary>
    private const int SaltSize = 16;

    /// <summary>
    /// 哈希输出长度（字节）。
    /// </summary>
    private const int HashSize = 32;

    /// <summary>
    /// PBKDF2 迭代次数（符合 OWASP 2023 建议的最低安全标准）。
    /// </summary>
    private const int Iterations = 600000;

    /// <summary>
    /// 哈希分隔符。
    /// </summary>
    private const char Separator = ':';

    /// <summary>
    /// 算法版本标记。
    /// </summary>
    private const string Version = "PBKDF2";

    /// <summary>
    /// 对明文密码进行哈希，返回格式为 "PBKDF2:迭代次数:Base64盐值:Base64哈希" 的字符串。
    /// </summary>
    public static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize);

        return $"{Version}{Separator}{Iterations}{Separator}{Convert.ToBase64String(salt)}{Separator}{Convert.ToBase64String(hash)}";
    }

    /// <summary>
    /// 验证明文密码是否与已存储的哈希值匹配。
    /// 支持 PBKDF2 格式和旧版明文密码（兼容迁移）。
    /// </summary>
    public static bool VerifyPassword(string password, string storedHash)
    {
        if (string.IsNullOrEmpty(storedHash))
        {
            return false;
        }

        // 兼容旧版明文密码（迁移过渡期使用，验证通过后会自动升级为哈希存储）
        if (!storedHash.StartsWith(Version + Separator))
        {
            return string.Equals(password, storedHash, StringComparison.Ordinal);
        }

        var parts = storedHash.Split(Separator);
        if (parts.Length != 4 || parts[0] != Version)
        {
            return false;
        }

        if (!int.TryParse(parts[1], out var iterations))
        {
            return false;
        }

        byte[] salt;
        byte[] storedHashBytes;
        try
        {
            salt = Convert.FromBase64String(parts[2]);
            storedHashBytes = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        var computedHash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            storedHashBytes.Length);

        return CryptographicOperations.FixedTimeEquals(computedHash, storedHashBytes);
    }

    /// <summary>
    /// 判断存储的哈希值是否为 PBKDF2 格式（用于识别是否需要密码升级）。
    /// </summary>
    public static bool IsHashed(string storedHash)
    {
        return !string.IsNullOrEmpty(storedHash) && storedHash.StartsWith(Version + Separator);
    }
}
