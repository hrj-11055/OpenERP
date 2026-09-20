using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenERP.BasicData.Data;
using OpenERP.HR.Models.Entities;
using OpenERP.Web.Data.HR;
using System.Security.Claims;

namespace OpenERP.Web.Controllers;

/// <summary>
/// 公司组织银行账号接口控制器（提供账务信息页签的查询、保存与删除接口）。
/// </summary>
[ApiController]
[Route("api/companyorganizationbankaccount")]
[Authorize]
public class CompanyOrganizationBankAccountApiController : ControllerBase
{
    /// <summary>
    /// 银行基础数据类型编码（对应基础数据字典 BANK）。
    /// </summary>
    private const string BankTypeCode = "BANK";

    private static readonly IReadOnlyList<BankAccountOptionItem> CurrencyOptions =
    [
        new("RMB", "RMB"),
        new("USD", "USD"),
        new("HKD", "HKD")
    ];

    private static readonly IReadOnlyList<BankAccountOptionItem> StatusOptions =
    [
        new("NORMAL", "正常"),
        new("DISABLED", "已停用"),
        new("CANCELLED", "已注销")
    ];

    private readonly IHrRepository _hrRepository;
    private readonly IBasicDataRepository _basicDataRepository;

    public CompanyOrganizationBankAccountApiController(
        IHrRepository hrRepository,
        IBasicDataRepository basicDataRepository)
    {
        _hrRepository = hrRepository;
        _basicDataRepository = basicDataRepository;
    }

    /// <summary>
    /// 查询公司组织银行账号分页数据。
    /// </summary>
    [HttpGet("{companyOrganizationId:int}")]
    public async Task<IActionResult> Get(
        int companyOrganizationId,
        [FromQuery] string? keyword = null,
        [FromQuery] string? statusCode = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        if (companyOrganizationId <= 0)
        {
            return BadRequest(new { message = "请先保存公司组织后再维护银行账号。" });
        }

        var organization = await _hrRepository.GetCompanyOrganizationByIdAsync(companyOrganizationId);
        if (organization is null)
        {
            return NotFound(new { message = "公司组织不存在或已删除。" });
        }

        var result = await _hrRepository.GetCompanyOrganizationBankAccountsAsync(
            companyOrganizationId,
            keyword,
            statusCode,
            pageNumber,
            pageSize);

        return Ok(new
        {
            result.PageNumber,
            result.PageSize,
            result.TotalCount,
            items = result.Items.Select(item => new
            {
                item.Id,
                item.CompanyOrganizationId,
                item.AccountNumber,
                item.BankId,
                item.BankName,
                item.BranchName,
                item.BranchAddress,
                item.CurrencyCode,
                item.StatusCode,
                item.SubjectCode,
                item.SubjectName,
                item.Remarks,
                item.IsDefault,
                createdAt = item.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
            }),
            bankOptions = await BuildBankOptionsAsync(),
            currencyOptions = CurrencyOptions,
            statusOptions = StatusOptions
        });
    }

    /// <summary>
    /// 保存公司组织银行账号记录。
    /// </summary>
    [HttpPost("{companyOrganizationId:int}")]
    public async Task<IActionResult> Save(int companyOrganizationId, [FromBody] SaveCompanyOrganizationBankAccountRequest request)
    {
        if (companyOrganizationId <= 0)
        {
            return BadRequest(new { message = "请先保存公司组织后再维护银行账号。" });
        }

        var organization = await _hrRepository.GetCompanyOrganizationByIdAsync(companyOrganizationId);
        if (organization is null)
        {
            return NotFound(new { message = "公司组织不存在或已删除。" });
        }

        var accountNumber = NormalizeRequiredText(request.AccountNumber);
        if (accountNumber is null)
        {
            return BadRequest(new { message = "请输入银行账号。" });
        }

        var branchName = NormalizeRequiredText(request.BranchName);
        if (branchName is null)
        {
            return BadRequest(new { message = "请输入开户支行名称。" });
        }

        var bankAccount = new CompanyOrganizationBankAccount
        {
            Id = request.Id,
            CompanyOrganizationId = companyOrganizationId,
            AccountNumber = accountNumber,
            BankId = request.BankId,
            BranchName = branchName,
            BranchAddress = NormalizeOptionalText(request.BranchAddress),
            CurrencyCode = NormalizeOptionCode(request.CurrencyCode, CurrencyOptions, "RMB"),
            StatusCode = NormalizeOptionCode(request.StatusCode, StatusOptions, "NORMAL"),
            SubjectCode = NormalizeOptionalText(request.SubjectCode),
            SubjectName = NormalizeOptionalText(request.SubjectName),
            Remarks = NormalizeOptionalText(request.Remarks),
            IsDefault = request.IsDefault
        };

        var operatorName = ResolveOperatorName();
        if (bankAccount.Id > 0)
        {
            bankAccount.UpdatedBy = operatorName;
        }
        else
        {
            bankAccount.CreatedBy = operatorName;
        }

        var savedId = await _hrRepository.SaveCompanyOrganizationBankAccountAsync(bankAccount);
        if (savedId <= 0)
        {
            return BadRequest(new { message = "银行账号保存失败，请刷新后重试。" });
        }

        return Ok(new { success = true, id = savedId });
    }

    /// <summary>
    /// 删除公司组织银行账号记录。
    /// </summary>
    [HttpPost("{companyOrganizationId:int}/delete")]
    public async Task<IActionResult> Delete(int companyOrganizationId, [FromBody] DeleteCompanyOrganizationBankAccountsRequest request)
    {
        if (companyOrganizationId <= 0)
        {
            return BadRequest(new { message = "公司组织参数无效。" });
        }

        var ids = request.Ids?.Where(id => id > 0).Distinct().ToList() ?? [];
        if (ids.Count == 0)
        {
            return BadRequest(new { message = "请选择要删除的银行账号记录。" });
        }

        var affected = await _hrRepository.DeleteCompanyOrganizationBankAccountsAsync(companyOrganizationId, ids);
        return Ok(new { success = true, affected });
    }

    /// <summary>
    /// 构建银行下拉选项（来自基础数据 BANK 字典）。
    /// </summary>
    private async Task<IReadOnlyList<BankAccountOptionItem>> BuildBankOptionsAsync()
    {
        var type = await _basicDataRepository.GetTypeByCodeAsync(BankTypeCode);
        if (type is null)
        {
            return [];
        }

        var items = await _basicDataRepository.GetItemsByTypeIdAsync(type.Id);
        return items
            .Where(item => !item.IsDeleted && item.IsActive)
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.ItemName)
            .Select(item => new BankAccountOptionItem(item.Id.ToString(), item.ItemName))
            .ToList();
    }

    /// <summary>
    /// 解析当前操作人名称（优先使用登录账号声明）。
    /// </summary>
    private string ResolveOperatorName()
        => User.FindFirstValue("erp:account")
            ?? User.Identity?.Name
            ?? "system";

    /// <summary>
    /// 规范必填文本输入（去除空白后为空则返回 null）。
    /// </summary>
    private static string? NormalizeRequiredText(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>
    /// 规范可空文本输入（去除首尾空格，空白值统一转为 null）。
    /// </summary>
    private static string? NormalizeOptionalText(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>
    /// 规范固定选项编码（若不在允许列表中则回退到默认值）。
    /// </summary>
    private static string NormalizeOptionCode(string? value, IReadOnlyList<BankAccountOptionItem> options, string defaultCode)
    {
        var normalized = value?.Trim().ToUpperInvariant();
        return options.Any(option => string.Equals(option.Value, normalized, StringComparison.OrdinalIgnoreCase))
            ? normalized!
            : defaultCode;
    }
}

/// <summary>
/// 通用下拉选项项（用于银行、货币与状态下拉）。
/// </summary>
public class BankAccountOptionItem
{
    /// <summary>
    /// 选项值（编码或ID字符串）。
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// 选项文本（页面展示名称）。
    /// </summary>
    public string Text { get; set; } = string.Empty;

    public BankAccountOptionItem()
    {
    }

    public BankAccountOptionItem(string value, string text)
    {
        Value = value;
        Text = text;
    }
}

/// <summary>
/// 保存公司组织银行账号请求（对应账务信息编辑表单）。
/// </summary>
public class SaveCompanyOrganizationBankAccountRequest
{
    /// <summary>
    /// 记录ID（0 表示新增）。
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 银行账号（公司对外收付款账号）。
    /// </summary>
    public string? AccountNumber { get; set; }

    /// <summary>
    /// 开户银行ID（对应基础数据字典 BANK）。
    /// </summary>
    public int? BankId { get; set; }

    /// <summary>
    /// 开户支行名称。
    /// </summary>
    public string? BranchName { get; set; }

    /// <summary>
    /// 开户支行地址。
    /// </summary>
    public string? BranchAddress { get; set; }

    /// <summary>
    /// 货币编码。
    /// </summary>
    public string? CurrencyCode { get; set; }

    /// <summary>
    /// 状态编码。
    /// </summary>
    public string? StatusCode { get; set; }

    /// <summary>
    /// 科目编号。
    /// </summary>
    public string? SubjectCode { get; set; }

    /// <summary>
    /// 科目名称。
    /// </summary>
    public string? SubjectName { get; set; }

    /// <summary>
    /// 备注。
    /// </summary>
    public string? Remarks { get; set; }

    /// <summary>
    /// 是否默认账号。
    /// </summary>
    public bool IsDefault { get; set; }
}

/// <summary>
/// 删除公司组织银行账号请求（批量删除选中记录）。
/// </summary>
public class DeleteCompanyOrganizationBankAccountsRequest
{
    /// <summary>
    /// 要删除的记录ID集合。
    /// </summary>
    public List<int> Ids { get; set; } = [];
}
