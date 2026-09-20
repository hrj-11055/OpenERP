/*
 * File: OpenERP.Web/Security/PasswordPolicyValidator.cs
 * Description: 员工账号密码复杂度校验工具（统一约束账号密码安全规则）。
 */

namespace OpenERP.Web.Security;

/// <summary>
/// 密码策略校验工具（用于统一校验员工登录密码是否符合系统安全要求）。
/// </summary>
public static class PasswordPolicyValidator
{
    /// <summary>
    /// 密码最小长度（满足常规业务系统密码安全要求）。
    /// </summary>
    private const int MinimumLength = 8;

    /// <summary>
    /// 密码最大长度（避免异常超长输入影响表单与数据库存储）。
    /// </summary>
    private const int MaximumLength = 100;

    /// <summary>
    /// 校验密码是否符合复杂度要求（至少包含字母、数字、特殊字符，且不允许空白字符）。
    /// </summary>
    public static bool TryValidate(string? password, out string errorMessage)
    {
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(password))
        {
            errorMessage = "请输入登录密码。";
            return false;
        }

        if (password.Length < MinimumLength || password.Length > MaximumLength)
        {
            errorMessage = $"密码长度需为 {MinimumLength}-{MaximumLength} 个字符。";
            return false;
        }

        if (password.Any(char.IsWhiteSpace))
        {
            errorMessage = "密码不能包含空格或其他空白字符。";
            return false;
        }

        var hasLetter = password.Any(char.IsLetter);
        var hasDigit = password.Any(char.IsDigit);
        var hasSpecialCharacter = password.Any(character => !char.IsLetterOrDigit(character));

        if (!hasLetter || !hasDigit || !hasSpecialCharacter)
        {
            errorMessage = "密码需同时包含字母、数字和特殊字符。";
            return false;
        }

        return true;
    }
}
