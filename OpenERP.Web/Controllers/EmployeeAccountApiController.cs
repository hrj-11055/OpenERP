/*
 * File: OpenERP.Web/Controllers/EmployeeAccountApiController.cs
 * Description: 员工账号及权限 API 控制器（提供 AJAX 加载和保存账号权限数据）。
 */

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenERP.Web.Data.HR;
using OpenERP.Web.Security;

namespace OpenERP.Web.Controllers;

/// <summary>
/// 员工账号及权限 API 控制器（处理账号设置页签的加载和保存请求）。
/// </summary>
[ApiController]
[Route("api/employeeaccount")]
[Route("api/[controller]")]
[Authorize]
public class EmployeeAccountApiController : ControllerBase
{
    /// <summary>
    /// 人资仓储（读取角色、权限、公司及员工数据）。
    /// </summary>
    private readonly IHrRepository _hrRepository;

    public EmployeeAccountApiController(IHrRepository hrRepository)
    {
        _hrRepository = hrRepository;
    }

    /// <summary>
    /// 获取员工账号及权限数据（用于账号及权限页签 AJAX 加载）。
    /// </summary>
    [HttpGet("{employeeId}")]
    public async Task<IActionResult> Get(int employeeId)
    {
        var employee = await _hrRepository.GetEmployeeByIdAsync(employeeId);
        if (employee == null)
        {
            return NotFound(new { message = "员工不存在。" });
        }

        var roles = await _hrRepository.GetRolesAsync();
        var permissions = await _hrRepository.GetPermissionsAsync();
        var userPermissionIds = await _hrRepository.GetUserPermissionIdsAsync(employeeId);
        var companies = await _hrRepository.GetCompanyOrganizationsAsync();
        var userCompanyIds = await _hrRepository.GetUserCompanyIdsAsync(employeeId);

        return Ok(new
        {
            loginAccount = employee.LoginAccount,
            hasPassword = !string.IsNullOrEmpty(employee.LoginPassword),
            forceViewRecordDays = employee.ForceViewRecordDays,
            roleId = employee.RoleId,
            accountValidUntil = employee.AccountValidUntil?.ToString("yyyy-MM-dd"),
            isAccountFrozen = employee.IsAccountFrozen,
            roles = roles.Select(role => new { role.Id, role.RoleCode, role.RoleName }),
            permissions = permissions.Select(permission => new
            {
                permission.Id,
                permission.PermissionCode,
                permission.PermissionName,
                permission.Category,
                permission.SortOrder
            }),
            userPermissionIds,
            companies = companies.Select(company => new
            {
                company.Id,
                company.OrganizationCode,
                company.OrganizationName
            }),
            userCompanyIds
        });
    }

    /// <summary>
    /// 保存员工账号及权限设置（用于账号及权限页签 AJAX 提交）。
    /// </summary>
    [HttpPost("{employeeId}")]
    public async Task<IActionResult> Save(int employeeId, [FromBody] SaveAccountRequest request)
    {
        if (employeeId <= 0)
        {
            return BadRequest(new { message = "无效的员工ID。" });
        }

        var employee = await _hrRepository.GetEmployeeByIdAsync(employeeId);
        if (employee == null)
        {
            return NotFound(new { message = "员工不存在。" });
        }

        // 登录账号（去除前后空格，空字符串按未设置处理）。
        var normalizedLoginAccount = string.IsNullOrWhiteSpace(request.LoginAccount)
            ? null
            : request.LoginAccount.Trim();

        if (!string.IsNullOrWhiteSpace(normalizedLoginAccount))
        {
            var existingAccountOwner = await _hrRepository.GetEmployeeByLoginAccountAsync(normalizedLoginAccount);
            if (existingAccountOwner is not null && existingAccountOwner.Id != employeeId)
            {
                return BadRequest(new { message = "登录账号已被其他员工使用，请更换后再保存。" });
            }
        }

        // 登录密码（如有新密码则按密码策略校验后做 PBKDF2 哈希存储）。
        string? hashedPassword = null;
        if (!string.IsNullOrWhiteSpace(request.NewPassword))
        {
            if (!PasswordPolicyValidator.TryValidate(request.NewPassword, out var passwordErrorMessage))
            {
                return BadRequest(new { message = passwordErrorMessage });
            }

            hashedPassword = PasswordHasher.HashPassword(request.NewPassword);
        }

        // 有效日期（为空表示永久有效）。
        DateTime? accountValidUntil = null;
        if (!string.IsNullOrWhiteSpace(request.AccountValidUntil)
            && DateTime.TryParse(request.AccountValidUntil, out var validDate))
        {
            accountValidUntil = validDate.Date;
        }

        // 强制查看记录日数（为空表示不限制）。
        int? forceViewRecordDays = null;
        if (request.ForceViewRecordDays.HasValue)
        {
            if (request.ForceViewRecordDays.Value < 0)
            {
                return BadRequest(new { message = "强制查看记录日数不能小于 0。" });
            }

            forceViewRecordDays = request.ForceViewRecordDays.Value;
        }

        // 功能权限ID列表（去重后保存）。
        var permissionIds = (request.PermissionIds ?? [])
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        // 管辖公司ID列表（去重后保存）。
        var companyIds = (request.CompanyIds ?? [])
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        var saved = await _hrRepository.SaveUserAccountAsync(
            employeeId,
            normalizedLoginAccount,
            hashedPassword,
            request.RoleId,
            accountValidUntil,
            request.IsAccountFrozen,
            forceViewRecordDays,
            permissionIds,
            companyIds);

        if (!saved)
        {
            return NotFound(new { message = "保存失败，员工记录可能已被删除。" });
        }

        return Ok(new { message = "保存成功。" });
    }
}

/// <summary>
/// 保存账号设置请求体。
/// </summary>
public class SaveAccountRequest
{
    /// <summary>
    /// 登录账号（员工系统登录账号）。
    /// </summary>
    public string? LoginAccount { get; set; }

    /// <summary>
    /// 新密码（为空表示不修改密码）。
    /// </summary>
    public string? NewPassword { get; set; }

    /// <summary>
    /// 角色ID（对应权限角色实体 HR_Role）。
    /// </summary>
    public int? RoleId { get; set; }

    /// <summary>
    /// 有效日期（格式 yyyy-MM-dd，留空表示永久有效）。
    /// </summary>
    public string? AccountValidUntil { get; set; }

    /// <summary>
    /// 冻结账号（勾选后账号不可登录系统）。
    /// </summary>
    public bool IsAccountFrozen { get; set; }

    /// <summary>
    /// 强制查看记录日数（账号登录后需要查看的历史记录天数）。
    /// </summary>
    public int? ForceViewRecordDays { get; set; }

    /// <summary>
    /// 选中的功能权限ID列表（对应 HR_Permission 主键）。
    /// </summary>
    public List<int>? PermissionIds { get; set; }

    /// <summary>
    /// 选中的管辖公司组织ID列表（对应 HR_CompanyOrganization 主键）。
    /// </summary>
    public List<int>? CompanyIds { get; set; }
}
