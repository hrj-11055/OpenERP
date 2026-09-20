using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace OpenERP.Web.Controllers;

/// <summary>
/// 认证接口控制器（用于 Vue 登录页的登录与登出）。
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthApiController : ControllerBase
{
    /// <summary>
    /// 员工资料表名（用于登录认证查询）。
    /// </summary>
    private const string EmployeeTableName = "HR_Employee";

    /// <summary>
    /// 数据库连接字符串（来自 DefaultConnection）。
    /// </summary>
    private readonly string _connectionString;

    /// <summary>
    /// 构造函数（注入配置）。
    /// </summary>
    public AuthApiController(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Missing connection string: DefaultConnection");
    }

    /// <summary>
    /// 登录接口（校验账号后签发 Cookie 会话）。
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> LoginAsync([FromBody] LoginRequest request)
    {
        var user = await TryValidateEmployeeAsync(request.Account, request.Password)
            ?? TryValidateDemoUser(request.Account, request.Password);

        if (user is null)
        {
            return Unauthorized(new LoginResponse
            {
                Success = false,
                Message = "账号或密码错误。"
            });
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.DisplayName),
            new("erp:account", user.Account),
            new("erp:company", request.Company),
            new("erp:fiscalYear", request.FiscalYear),
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            AllowRefresh = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);

        return Ok(new LoginResponse
        {
            Success = true,
            Message = "登录成功。",
            DisplayName = user.DisplayName,
            RedirectUrl = "/"
        });
    }

    /// <summary>
    /// 登出接口（清理当前用户 Cookie 会话）。
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> LogoutAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok(new LoginResponse
        {
            Success = true,
            Message = "已退出登录。",
            RedirectUrl = "/login"
        });
    }

    /// <summary>
    /// 按员工资料表中的登录字段校验账号密码。
    /// </summary>
    private async Task<AuthUser?> TryValidateEmployeeAsync(string account, string password)
    {
        var sql = $"""
            IF OBJECT_ID('dbo.{EmployeeTableName}', 'U') IS NULL
            BEGIN
                SELECT TOP (0)
                    CAST(0 AS INT) AS Id,
                    CAST('' AS NVARCHAR(100)) AS Account,
                    CAST('' AS NVARCHAR(50)) AS FirstName,
                    CAST('' AS NVARCHAR(50)) AS LastName;
                RETURN;
            END;

            IF COL_LENGTH('dbo.{EmployeeTableName}', 'LoginAccount') IS NULL
               OR COL_LENGTH('dbo.{EmployeeTableName}', 'LoginPassword') IS NULL
            BEGIN
                SELECT TOP (0)
                    CAST(0 AS INT) AS Id,
                    CAST('' AS NVARCHAR(100)) AS Account,
                    CAST('' AS NVARCHAR(50)) AS FirstName,
                    CAST('' AS NVARCHAR(50)) AS LastName;
                RETURN;
            END;

            SELECT TOP (1)
                e.Id,
                e.LoginAccount AS Account,
                ISNULL(e.FirstName, '') AS FirstName,
                ISNULL(e.LastName, '') AS LastName
            FROM dbo.{EmployeeTableName} e
            WHERE ISNULL(e.IsDeleted, 0) = 0
              AND e.LoginAccount = @Account
              AND e.LoginPassword = @Password;
            """;

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Account", account);
        cmd.Parameters.AddWithValue("@Password", password);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        var firstName = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
        var lastName = reader.IsDBNull(3) ? string.Empty : reader.GetString(3);
        var displayName = BuildAuthenticatedDisplayName(firstName, lastName, account);

        return new AuthUser
        {
            UserId = reader.GetInt32(0),
            Account = reader.IsDBNull(1) ? account : reader.GetString(1),
            DisplayName = displayName
        };
    }

    /// <summary>
    /// 开发环境演示账号（便于新系统联调登录流程）。
    /// </summary>
    /// <summary>
    /// 生成登录后显示姓名（中文姓名按姓氏在前，英文姓名保留空格）。
    /// </summary>
    private static string BuildAuthenticatedDisplayName(string? firstName, string? lastName, string fallbackAccount)
    {
        var normalizedFirstName = firstName?.Trim() ?? string.Empty;
        var normalizedLastName = lastName?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalizedFirstName) && string.IsNullOrWhiteSpace(normalizedLastName))
        {
            return fallbackAccount;
        }

        return ContainsLatinLetter(normalizedFirstName) || ContainsLatinLetter(normalizedLastName)
            ? string.Join(" ", new[] { normalizedFirstName, normalizedLastName }.Where(value => !string.IsNullOrWhiteSpace(value)))
            : $"{normalizedLastName}{normalizedFirstName}";
    }

    /// <summary>
    /// 判断字符串中是否包含英文字母（用于区分中英文姓名拼接方式）。
    /// </summary>
    private static bool ContainsLatinLetter(string value)
    {
        return value.Any(character => character is >= 'A' and <= 'Z' or >= 'a' and <= 'z');
    }

    private static AuthUser? TryValidateDemoUser(string account, string password)
    {
        if (string.Equals(account, "admin", StringComparison.OrdinalIgnoreCase)
            && string.Equals(password, "123456", StringComparison.Ordinal))
        {
            return new AuthUser
            {
                UserId = 0,
                Account = "admin",
                DisplayName = "系统管理员"
            };
        }

        return null;
    }

    /// <summary>
    /// 登录请求模型（来自登录页表单）。
    /// </summary>
    public sealed class LoginRequest
    {
        /// <summary>
        /// 财务年度（界面账套选择项）。
        /// </summary>
        [Required]
        public string FiscalYear { get; set; } = string.Empty;

        /// <summary>
        /// 登录公司（界面公司选择项）。
        /// </summary>
        [Required]
        public string Company { get; set; } = string.Empty;

        /// <summary>
        /// 登录账号（员工登录账号）。
        /// </summary>
        [Required]
        public string Account { get; set; } = string.Empty;

        /// <summary>
        /// 登录密码（员工登录密码明文输入）。
        /// </summary>
        [Required]
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// 登录响应模型（返回给前端）。
    /// </summary>
    public sealed class LoginResponse
    {
        /// <summary>
        /// 是否登录成功。
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 结果提示消息。
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 展示用户名（用于页面提示）。
        /// </summary>
        public string? DisplayName { get; set; }

        /// <summary>
        /// 登录后跳转地址。
        /// </summary>
        public string? RedirectUrl { get; set; }
    }

    /// <summary>
    /// 登录校验结果（内部用户信息载体）。
    /// </summary>
    private sealed class AuthUser
    {
        /// <summary>
        /// 用户主键 ID。
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// 账号标识。
        /// </summary>
        public string Account { get; set; } = string.Empty;

        /// <summary>
        /// 展示名称。
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;
    }
}
