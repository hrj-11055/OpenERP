/*
 * File: OpenERP.Web/Controllers/AccountController.cs
 * Description: 账户控制器（负责登录、登出、切换公司与修改密码）。
 */

using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using OpenERP.Web.Data.HR;
using OpenERP.Web.Localization;
using OpenERP.Web.Security;

namespace OpenERP.Web.Controllers;

/// <summary>
/// 账户控制器（处理登录、登出、切换公司与修改密码等认证相关请求）。
/// </summary>
public class AccountController : Controller
{
    /// <summary>
    /// 共享本地化资源（用于登录页、切换公司与提示消息文案）。
    /// </summary>
    private readonly IStringLocalizer<SharedResource> _localizer;

    /// <summary>
    /// 人资仓储（读取员工账号、公司组织与登录限制数据）。
    /// </summary>
    private readonly IHrRepository _hrRepository;

    /// <summary>
    /// 权限声明类型（写入当前登录用户的有效权限编码）。
    /// </summary>
    private const string PermissionClaimType = "erp:permission";

    /// <summary>
    /// 角色声明类型（写入当前登录用户的角色编码）。
    /// </summary>
    private const string RoleClaimType = "erp:role";

    public AccountController(IStringLocalizer<SharedResource> localizer, IHrRepository hrRepository)
    {
        _localizer = localizer;
        _hrRepository = hrRepository;
    }

    /// <summary>
    /// 登录页面（展示登录表单，并提供公司下拉默认数据）。
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Login(string? returnUrl = null)
    {
        var companyContext = await BuildCompanySelectionContextAsync();
        var viewModel = new LoginViewModel
        {
            ReturnUrl = returnUrl,
            FiscalYearOptions = GetFiscalYearOptions(),
            CompanyOptions = companyContext.Options,
            Company = companyContext.Options.FirstOrDefault(option => option.IsSelected)?.Value ?? string.Empty,
            CompanyScopeHint = companyContext.HintMessage
        };

        return View(viewModel);
    }

    /// <summary>
    /// 按登录账号动态返回可登录公司列表（用于登录页根据账号过滤公司下拉选项）。
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> CompanyOptions(string? account, string? selectedCompany = null)
    {
        var companyContext = await BuildCompanySelectionContextAsync(account, selectedCompany);
        return Json(new
        {
            companies = companyContext.Options.Select(option => new
            {
                value = option.Value,
                text = option.Text,
                isSelected = option.IsSelected
            }),
            hintMessage = companyContext.HintMessage,
            isRestricted = companyContext.IsRestricted
        });
    }

    /// <summary>
    /// 登录提交（校验账号密码、冻结状态、有效日期和管辖公司后签发 Cookie 会话）。
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        model.FiscalYearOptions = GetFiscalYearOptions();

        var companyContext = await BuildCompanySelectionContextAsync(model.Account, model.Company);
        model.CompanyOptions = companyContext.Options;
        model.CompanyScopeHint = companyContext.HintMessage;

        if (string.IsNullOrWhiteSpace(model.Company))
        {
            model.Company = model.CompanyOptions.FirstOrDefault(option => option.IsSelected)?.Value ?? string.Empty;
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // 先尝试员工账号校验。
        var (user, employee) = await TryValidateEmployeeWithAccountCheckAsync(model.Account, model.Password);

        // 员工账号校验失败时再尝试演示账号。
        if (user is null)
        {
            user = TryValidateDemoUser(model.Account, model.Password);
        }

        if (user is null)
        {
            ModelState.AddModelError(string.Empty, _localizer["Login_InvalidCredentials"]);
            return View(model);
        }

        // 员工账号冻结校验。
        if (employee is not null && employee.IsAccountFrozen)
        {
            ModelState.AddModelError(string.Empty, "此账号已被冻结，无法登录系统。");
            return View(model);
        }

        // 员工账号有效期校验（有效日期当天仍允许登录，次日开始失效）。
        if (employee is not null
            && employee.AccountValidUntil.HasValue
            && employee.AccountValidUntil.Value.Date < DateTime.Today)
        {
            ModelState.AddModelError(string.Empty, "此账号已超过有效日期，无法登录系统。");
            return View(model);
        }

        // 员工管辖公司校验（未分配公司则禁止登录；仅允许登录被授权的公司）。
        if (employee is not null)
        {
            var allowedCompanyCodes = await _hrRepository.GetUserCompanyCodesAsync(employee.Id);
            if (allowedCompanyCodes.Count == 0)
            {
                model.CompanyOptions = [];
                model.Company = string.Empty;
                model.CompanyScopeHint = "该账号未配置管辖公司，暂不可登录系统。";
                ModelState.AddModelError(string.Empty, "此账号尚未配置管辖公司，无法登录系统。");
                return View(model);
            }

            if (!allowedCompanyCodes.Contains(model.Company, StringComparer.OrdinalIgnoreCase))
            {
                var restrictedCompanyContext = await BuildCompanySelectionContextAsync(model.Account, model.Company);
                model.CompanyOptions = restrictedCompanyContext.Options;
                model.CompanyScopeHint = restrictedCompanyContext.HintMessage;
                model.Company = restrictedCompanyContext.Options.FirstOrDefault(option => option.IsSelected)?.Value ?? string.Empty;
                ModelState.AddModelError(string.Empty, "您没有权限登录所选的公司组织。");
                return View(model);
            }
        }

        var claims = await BuildAuthenticatedClaimsAsync(user, employee, model.Company, model.FiscalYear);

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = model.RememberMe,
            AllowRefresh = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return RedirectToAction("Index", "Home", new { area = "" });
    }

    /// <summary>
    /// 登出（清理当前用户 Cookie 会话）。
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    /// <summary>
    /// 切换当前业务公司（仅允许切换到当前账号已授权的公司组织）。
    /// </summary>
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SwitchCompany(string company, string? returnUrl = null)
    {
        var currentAccount = User.FindFirstValue("erp:account");
        var companyContext = await BuildCompanySelectionContextAsync(currentAccount, company, fallBackToAllWhenAccountUnknown: false);
        var selectedCompany = companyContext.Options
            .FirstOrDefault(option => string.Equals(option.Value, company, StringComparison.OrdinalIgnoreCase));

        if (selectedCompany is null)
        {
            return RedirectToSafeUrl(returnUrl);
        }

        var authenticateResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (!authenticateResult.Succeeded || authenticateResult.Principal is null)
        {
            return RedirectToAction(nameof(Login));
        }

        var refreshedClaims = authenticateResult.Principal.Claims
            .Where(claim => !string.Equals(claim.Type, "erp:company", StringComparison.Ordinal))
            .ToList();
        refreshedClaims.Add(new Claim("erp:company", selectedCompany.Value));

        var refreshedIdentity = new ClaimsIdentity(refreshedClaims, CookieAuthenticationDefaults.AuthenticationScheme);
        var refreshedPrincipal = new ClaimsPrincipal(refreshedIdentity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            refreshedPrincipal,
            authenticateResult.Properties ?? new AuthenticationProperties
            {
                AllowRefresh = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            });

        return RedirectToSafeUrl(returnUrl);
    }

    /// <summary>
    /// 修改密码页面（展示当前登录账号的密码修改表单）。
    /// </summary>
    [HttpGet]
    [Authorize]
    public IActionResult ChangePassword()
    {
        return View(new ChangePasswordViewModel());
    }

    /// <summary>
    /// 修改密码提交（校验当前密码与新密码策略后更新员工登录密码）。
    /// </summary>
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (string.Equals(model.CurrentPassword, model.NewPassword, StringComparison.Ordinal))
        {
            ModelState.AddModelError(nameof(ChangePasswordViewModel.NewPassword), "新密码不能与当前密码相同。");
            return View(model);
        }

        if (!PasswordPolicyValidator.TryValidate(model.NewPassword, out var passwordErrorMessage))
        {
            ModelState.AddModelError(nameof(ChangePasswordViewModel.NewPassword), passwordErrorMessage);
            return View(model);
        }

        var currentAccount = User.FindFirstValue("erp:account");
        if (string.IsNullOrWhiteSpace(currentAccount))
        {
            ModelState.AddModelError(string.Empty, "未获取到当前登录账号，请重新登录后再试。");
            return View(model);
        }

        if (string.Equals(currentAccount, "admin", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(string.Empty, "演示账号暂不支持修改密码。");
            return View(model);
        }

        var isPasswordChanged = await TryChangeEmployeePasswordAsync(currentAccount, model.CurrentPassword, model.NewPassword);
        if (!isPasswordChanged)
        {
            ModelState.AddModelError(nameof(ChangePasswordViewModel.CurrentPassword), "当前密码不正确。");
            return View(model);
        }

        TempData["ChangePasswordSuccessMessage"] = "密码修改成功。";
        return RedirectToAction(nameof(ChangePassword));
    }

    /// <summary>
    /// 获取财务年度选项列表。
    /// </summary>
    private List<FiscalYearOption> GetFiscalYearOptions()
    {
        var currentYear = DateTime.Now.Year;
        var options = new List<FiscalYearOption>();

        for (var i = -2; i <= 2; i++)
        {
            var year = currentYear + i;
            options.Add(new FiscalYearOption
            {
                Value = year.ToString(),
                Text = _localizer["Login_FiscalYearOption", year],
                IsSelected = i == 0
            });
        }

        return options;
    }

    /// <summary>
    /// 按登录账号构建可选公司列表（支持登录页动态过滤与已登录后的切换公司校验）。
    /// </summary>
    private async Task<CompanySelectionContext> BuildCompanySelectionContextAsync(
        string? loginAccount = null,
        string? selectedCompany = null,
        bool fallBackToAllWhenAccountUnknown = true)
    {
        var allCompanyOptions = await GetAllCompanyOptionsAsync(selectedCompany);

        if (string.IsNullOrWhiteSpace(loginAccount))
        {
            return new CompanySelectionContext
            {
                Options = allCompanyOptions,
                HintMessage = "输入登录账号后，系统会自动过滤可登录的公司组织。"
            };
        }

        var normalizedLoginAccount = loginAccount.Trim();
        if (string.Equals(normalizedLoginAccount, "admin", StringComparison.OrdinalIgnoreCase))
        {
            return new CompanySelectionContext
            {
                Options = allCompanyOptions,
                HintMessage = "演示账号可登录全部公司组织。"
            };
        }

        var employee = await _hrRepository.GetEmployeeByLoginAccountAsync(normalizedLoginAccount);
        if (employee is null)
        {
            return fallBackToAllWhenAccountUnknown
                ? new CompanySelectionContext
                {
                    Options = allCompanyOptions,
                    HintMessage = "未识别到登录账号时，先显示全部公司组织。"
                }
                : new CompanySelectionContext
                {
                    Options = [],
                    HintMessage = "未找到该登录账号对应的员工档案。",
                    IsRestricted = true
                };
        }

        var allowedCompanyCodes = await _hrRepository.GetUserCompanyCodesAsync(employee.Id);
        if (allowedCompanyCodes.Count == 0)
        {
            return new CompanySelectionContext
            {
                Options = [],
                HintMessage = "该账号未配置管辖公司，暂不可登录系统。",
                IsRestricted = true
            };
        }

        var filteredOptions = allCompanyOptions
            .Where(option => allowedCompanyCodes.Contains(option.Value, StringComparer.OrdinalIgnoreCase))
            .ToList();

        MarkSelectedCompany(filteredOptions, selectedCompany);

        return new CompanySelectionContext
        {
            Options = filteredOptions,
            HintMessage = $"当前账号可登录 {filteredOptions.Count} 个公司组织。",
            IsRestricted = true
        };
    }

    /// <summary>
    /// 获取全部公司组织选项（来自系统公司组织资料，组织为空时使用兜底选项）。
    /// </summary>
    private async Task<List<CompanyOption>> GetAllCompanyOptionsAsync(string? selectedCompany = null)
    {
        var organizations = await _hrRepository.GetCompanyOrganizationsAsync();
        var companyOptions = organizations
            .Where(organization => !string.IsNullOrWhiteSpace(organization.OrganizationCode))
            .OrderBy(organization => organization.OrganizationCode)
            .Select(organization => new CompanyOption
            {
                Value = organization.OrganizationCode,
                Text = organization.OrganizationName
            })
            .ToList();

        if (companyOptions.Count == 0)
        {
            companyOptions = GetFallbackCompanyOptions();
        }

        MarkSelectedCompany(companyOptions, selectedCompany);
        return companyOptions;
    }

    /// <summary>
    /// 公司组织为空时的兜底公司选项。
    /// </summary>
    private List<CompanyOption> GetFallbackCompanyOptions()
    {
        return
        [
            new() { Value = "HQ", Text = _localizer["Login_Company_HQ"] },
            new() { Value = "BJ", Text = _localizer["Login_Company_BJ"] },
            new() { Value = "SH", Text = _localizer["Login_Company_SH"] },
            new() { Value = "GZ", Text = _localizer["Login_Company_GZ"] },
            new() { Value = "SZ", Text = _localizer["Login_Company_SZ"] }
        ];
    }

    /// <summary>
    /// 标记当前选中的公司选项（未命中时默认选中第一项）。
    /// </summary>
    private static void MarkSelectedCompany(List<CompanyOption> companyOptions, string? selectedCompany)
    {
        foreach (var option in companyOptions)
        {
            option.IsSelected = false;
        }

        if (companyOptions.Count == 0)
        {
            return;
        }

        var selectedOption = !string.IsNullOrWhiteSpace(selectedCompany)
            ? companyOptions.FirstOrDefault(option =>
                string.Equals(option.Value, selectedCompany, StringComparison.OrdinalIgnoreCase))
            : null;

        (selectedOption ?? companyOptions[0]).IsSelected = true;
    }

    /// <summary>
    /// 安全跳转到站内页面（避免切换公司后跳到非法外部地址）。
    /// </summary>
    private IActionResult RedirectToSafeUrl(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home", new { area = "" });
    }

    /// <summary>
    /// 按当前登录账号修改员工登录密码（验证当前密码后使用 PBKDF2 哈希存储新密码）。
    /// </summary>
    /// <summary>
    /// 构建登录后的用户声明集合（包含账号、公司、财年、角色及有效权限，供菜单显示与后续授权扩展复用）。
    /// </summary>
    private async Task<List<Claim>> BuildAuthenticatedClaimsAsync(
        AuthUser user,
        OpenERP.HR.Models.Entities.Employee? employee,
        string company,
        string fiscalYear)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.DisplayName),
            new("erp:account", user.Account),
            new("erp:company", company),
            new("erp:fiscalYear", fiscalYear),
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
    /// 按当前登录账号修改员工登录密码（校验当前密码后使用 PBKDF2 哈希存储新密码）。
    /// </summary>
    private async Task<bool> TryChangeEmployeePasswordAsync(string account, string currentPassword, string newPassword)
    {
        var employee = await _hrRepository.GetEmployeeByLoginAccountAsync(account);
        if (employee == null || employee.IsDeleted)
        {
            return false;
        }

        if (!PasswordHasher.VerifyPassword(currentPassword, employee.LoginPassword ?? string.Empty))
        {
            return false;
        }

        var hashedNewPassword = PasswordHasher.HashPassword(newPassword);
        return await _hrRepository.UpdateEmployeePasswordAsync(employee.Id, hashedNewPassword);
    }

    /// <summary>
    /// 校验员工账号密码（使用 PBKDF2 验证，兼容旧版明文密码，并自动升级哈希存储）。
    /// </summary>
    private async Task<(AuthUser? User, OpenERP.HR.Models.Entities.Employee? Employee)> TryValidateEmployeeWithAccountCheckAsync(string account, string password)
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
    /// 开发环境演示账号（便于新系统联调登录流程）。
    /// </summary>
    private AuthUser? TryValidateDemoUser(string account, string password)
    {
        if (string.Equals(account, "admin", StringComparison.OrdinalIgnoreCase)
            && string.Equals(password, "123456", StringComparison.Ordinal))
        {
            return new AuthUser
            {
                UserId = 0,
                Account = "admin",
                DisplayName = _localizer["Login_DemoAdminDisplayName"]
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
    /// 登录校验结果（内部用户信息载体）。
    /// </summary>
    private sealed class AuthUser
    {
        /// <summary>
        /// 用户主键ID（对应员工主键或演示账号占位ID）。
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// 账号标识（写入登录票据的员工登录账号）。
        /// </summary>
        public string Account { get; set; } = string.Empty;

        /// <summary>
        /// 展示名称（登录后用于界面显示的姓名）。
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;
    }

    /// <summary>
    /// 登录公司选项上下文（用于登录页与切换公司弹窗共用公司过滤结果）。
    /// </summary>
    private sealed class CompanySelectionContext
    {
        /// <summary>
        /// 可选公司列表（登录页或切换公司页实际允许显示的公司选项）。
        /// </summary>
        public List<CompanyOption> Options { get; init; } = [];

        /// <summary>
        /// 账号范围提示（提示当前账号是否已按管辖公司做过滤）。
        /// </summary>
        public string HintMessage { get; init; } = string.Empty;

        /// <summary>
        /// 是否已按账号范围收窄公司列表（true 表示当前为受限列表）。
        /// </summary>
        public bool IsRestricted { get; init; }
    }
}

/// <summary>
/// 登录视图模型。
/// </summary>
public class LoginViewModel
{
    /// <summary>
    /// 财务年度（登录页面选择的业务年度）。
    /// </summary>
    [Required(ErrorMessage = "Login_FiscalYearRequired")]
    [Display(Name = "Login_FiscalYear")]
    public string FiscalYear { get; set; } = DateTime.Now.Year.ToString();

    /// <summary>
    /// 登录公司（员工本次登录选择的公司组织编码）。
    /// </summary>
    [Required(ErrorMessage = "Login_CompanyRequired")]
    [Display(Name = "Login_Company")]
    public string Company { get; set; } = string.Empty;

    /// <summary>
    /// 登录账号（员工系统登录账号）。
    /// </summary>
    [Required(ErrorMessage = "Login_AccountRequired")]
    [Display(Name = "Login_Account")]
    [StringLength(50, ErrorMessage = "Login_AccountMaxLength")]
    public string Account { get; set; } = string.Empty;

    /// <summary>
    /// 登录密码（员工输入的当前密码明文）。
    /// </summary>
    [Required(ErrorMessage = "Login_PasswordRequired")]
    [DataType(DataType.Password)]
    [Display(Name = "Login_Password")]
    [StringLength(100, ErrorMessage = "Login_PasswordMaxLength")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 记住我（勾选后保持持久登录会话）。
    /// </summary>
    [Display(Name = "Login_RememberMe")]
    public bool RememberMe { get; set; }

    /// <summary>
    /// 返回地址（登录成功后回跳到原访问地址）。
    /// </summary>
    public string? ReturnUrl { get; set; }

    /// <summary>
    /// 财务年度选项列表（登录页年度下拉数据）。
    /// </summary>
    public List<FiscalYearOption> FiscalYearOptions { get; set; } = [];

    /// <summary>
    /// 公司选项列表（登录页公司下拉数据）。
    /// </summary>
    public List<CompanyOption> CompanyOptions { get; set; } = [];

    /// <summary>
    /// 公司范围提示（提示账号可登录的公司范围或限制信息）。
    /// </summary>
    public string CompanyScopeHint { get; set; } = string.Empty;
}

/// <summary>
/// 修改密码视图模型。
/// </summary>
public class ChangePasswordViewModel
{
    /// <summary>
    /// 当前密码（用于校验当前登录用户身份）。
    /// </summary>
    [Required(ErrorMessage = "请输入当前密码。")]
    [DataType(DataType.Password)]
    [Display(Name = "当前密码")]
    [StringLength(100, ErrorMessage = "当前密码长度不能超过 100 个字符。")]
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>
    /// 新密码（修改后的登录密码）。
    /// </summary>
    [Required(ErrorMessage = "请输入新密码。")]
    [DataType(DataType.Password)]
    [Display(Name = "新密码")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "新密码长度需为 8-100 个字符。")]
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// 确认新密码（用于校验两次输入一致）。
    /// </summary>
    [Required(ErrorMessage = "请再次输入新密码。")]
    [DataType(DataType.Password)]
    [Display(Name = "确认新密码")]
    [Compare(nameof(NewPassword), ErrorMessage = "两次输入的新密码不一致。")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>
/// 财务年度选项。
/// </summary>
public class FiscalYearOption
{
    /// <summary>
    /// 年度值（写回登录表单的财务年度编码）。
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// 年度文本（登录页下拉显示文案）。
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// 是否默认选中（控制登录页年度默认值）。
    /// </summary>
    public bool IsSelected { get; set; }
}

/// <summary>
/// 公司选项。
/// </summary>
public class CompanyOption
{
    /// <summary>
    /// 公司编码（对应组织编码，登录后写入 erp:company 声明）。
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// 公司名称（登录页或切换公司弹窗显示名称）。
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// 是否默认选中（控制公司下拉当前选项）。
    /// </summary>
    public bool IsSelected { get; set; }
}
