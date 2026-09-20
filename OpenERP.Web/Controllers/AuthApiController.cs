/*
 * File: OpenERP.Web/Controllers/AuthApiController.cs
 * Description: 认证接口控制器（用于 Vue 登录页的登录与登出）。
 */

using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OpenERP.HR.Models.Entities;
using OpenERP.Web.Data.HR;
using OpenERP.Web.Security;

namespace OpenERP.Web.Controllers;

/// <summary>
/// 认证接口控制器（用于 Vue 登录页的登录与登出）。
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthApiController : ControllerBase
{
    /// <summary>
    /// 人资仓储（读取员工账号、角色权限与管辖公司数据）。
    /// </summary>
    private readonly IHrRepository _hrRepository;

    /// <summary>
    /// 登录节流器（按账号统计失败次数并临时锁定）。
    /// </summary>
    private readonly LoginThrottler _loginThrottler;

    /// <summary>
    /// 权限声明类型（写入当前登录用户的有效权限编码）。
    /// </summary>
    private const string PermissionClaimType = "erp:permission";

    /// <summary>
    /// 角色声明类型（写入当前登录用户的角色编码）。
    /// </summary>
    private const string RoleClaimType = "erp:role";

    /// <summary>
    /// 构造函数（注入人资仓储与登录节流器）。
    /// </summary>
    public AuthApiController(IHrRepository hrRepository, LoginThrottler loginThrottler)
    {
        _hrRepository = hrRepository;
        _loginThrottler = loginThrottler;
    }

    /// <summary>
    /// 登录接口（校验账号密码、冻结状态、有效日期和管辖公司后签发 Cookie 会话）。
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> LoginAsync([FromBody] LoginRequest request)
    {
        // 连续失败达到阈值的账号临时锁定，锁定期间不再执行密码校验。
        if (_loginThrottler.IsLockedOut(request.Account))
        {
            return Unauthorized(new LoginResponse
            {
                Success = false,
                Message = "登录失败次数过多，账号已临时锁定，请 15 分钟后再试。"
            });
        }

        // 先尝试员工账号校验（PBKDF2 哈希验证，兼容旧版明文并自动升级）。
        var (user, employee) = await TryValidateEmployeeAsync(request.Account, request.Password);

        // 员工账号校验失败时再尝试演示账号。
        if (user is null)
        {
            user = TryValidateDemoUser(request.Account, request.Password);
        }

        if (user is null)
        {
            _loginThrottler.RecordFailure(request.Account);
            return Unauthorized(new LoginResponse
            {
                Success = false,
                Message = "账号或密码错误。"
            });
        }

        // 密码已验证通过，清空该账号的失败计数。
        _loginThrottler.ResetOnSuccess(request.Account);

        // 员工账号冻结校验。
        if (employee is not null && employee.IsAccountFrozen)
        {
            return Unauthorized(new LoginResponse
            {
                Success = false,
                Message = "此账号已被冻结，无法登录系统。"
            });
        }

        // 员工账号有效期校验（有效日期当天仍允许登录，次日开始失效）。
        if (employee is not null
            && employee.AccountValidUntil.HasValue
            && employee.AccountValidUntil.Value.Date < DateTime.Today)
        {
            return Unauthorized(new LoginResponse
            {
                Success = false,
                Message = "此账号已超过有效日期，无法登录系统。"
            });
        }

        // 员工管辖公司校验（未分配公司则禁止登录；仅允许登录被授权的公司）。
        if (employee is not null)
        {
            var allowedCompanyCodes = await _hrRepository.GetUserCompanyCodesAsync(employee.Id);
            if (allowedCompanyCodes.Count == 0)
            {
                return Unauthorized(new LoginResponse
                {
                    Success = false,
                    Message = "此账号尚未配置管辖公司，无法登录系统。"
                });
            }

            if (!allowedCompanyCodes.Contains(request.Company, StringComparer.OrdinalIgnoreCase))
            {
                return Unauthorized(new LoginResponse
                {
                    Success = false,
                    Message = "您没有权限登录所选的公司组织。"
                });
            }
        }

        var claims = await BuildAuthenticatedClaimsAsync(user, employee, request.Company, request.FiscalYear);

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
    /// 构建登录后的用户声明集合（包含账号、公司、财年、角色及有效权限，与页面登录保持一致）。
    /// </summary>
    private async Task<List<Claim>> BuildAuthenticatedClaimsAsync(
        AuthUser user,
        Employee? employee,
        string company,
        string fiscalYear)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.DisplayName),
            new("erp:account", user.Account),
            new("erp:company", company),
            new("erp:fiscalYear", fiscalYear)
        };

        if (employee is null)
        {
            return claims;
        }

        if (employee.RoleId.HasValue)
        {
            var role = (await _hrRepository.GetRolesAsync())
                .FirstOrDefault(item => item.Id == employee.RoleId.Value && !item.IsDeleted);

            if (role is not null && !string.IsNullOrWhiteSpace(role.RoleCode))
            {
                claims.Add(new Claim(ClaimTypes.Role, role.RoleCode));
                claims.Add(new Claim(RoleClaimType, role.RoleCode));
            }
        }

        var permissionCodes = await _hrRepository.GetEffectivePermissionCodesAsync(employee.Id);
        foreach (var permissionCode in permissionCodes.Where(code => !string.IsNullOrWhiteSpace(code)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            claims.Add(new Claim(PermissionClaimType, permissionCode));
        }

        return claims;
    }

    /// <summary>
    /// 按员工资料表校验账号密码（PBKDF2 哈希验证，兼容旧版明文密码并自动升级为哈希存储）。
    /// </summary>
    private async Task<(AuthUser? User, Employee? Employee)> TryValidateEmployeeAsync(string account, string password)
    {
        var normalizedAccount = account?.Trim() ?? string.Empty;
        var employee = await _hrRepository.GetEmployeeByLoginAccountAsync(normalizedAccount);
        if (employee == null || employee.IsDeleted)
        {
            return (null, null);
        }

        if (string.IsNullOrEmpty(employee.LoginPassword))
        {
            return (null, null);
        }

        if (!PasswordHasher.VerifyPassword(password, employee.LoginPassword))
        {
            return (null, null);
        }

        // 自动升级旧版明文密码为 PBKDF2 哈希。
        if (!PasswordHasher.IsHashed(employee.LoginPassword))
        {
            var hashedPassword = PasswordHasher.HashPassword(password);
            await _hrRepository.UpdateEmployeePasswordAsync(employee.Id, hashedPassword);
        }

        var displayName = BuildAuthenticatedDisplayName(employee.FirstName, employee.LastName, normalizedAccount);
        var user = new AuthUser
        {
            UserId = employee.Id,
            Account = employee.LoginAccount ?? normalizedAccount,
            DisplayName = displayName
        };

        return (user, employee);
    }

    /// <summary>
    /// 开发演示账号（便于新系统联调登录流程，可登录全部公司组织）。
    /// </summary>
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
        [StringLength(50)]
        public string Account { get; set; } = string.Empty;

        /// <summary>
        /// 登录密码（员工登录密码明文输入）。
        /// </summary>
        [Required]
        [StringLength(100)]
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
