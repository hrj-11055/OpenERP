using Microsoft.AspNetCore.Mvc;
using OpenERP.HR.Models.Entities;
using OpenERP.Web.Data.HR;
using System.Security.Claims;

namespace OpenERP.Web.Controllers;

/// <summary>
/// 公司组织单号规则接口控制器（提供单号规则查询与保存接口）。
/// </summary>
[ApiController]
[Route("api/companyorganizationdocumentnumberrule")]
public class CompanyOrganizationDocumentNumberRuleApiController : ControllerBase
{
    /// <summary>
    /// 日期格式固定选项（与单号规则页签下拉框保持一致）。
    /// </summary>
    private static readonly IReadOnlyList<DocumentNumberRuleOptionItem> DateFormatOptions =
    [
        new("NONE", "无日期"),
        new("yyMM", "yyMM"),
        new("yyyyMM", "yyyyMM"),
        new("yyyyMMdd", "yyyyMMdd")
    ];

    /// <summary>
    /// 已接入的单据功能定义列表（当前系统中会实际调用规则生成单号的功能）。
    /// </summary>
    private static readonly IReadOnlyList<CompanyOrganizationDocumentNumberRuleDefinition> RuleDefinitions =
    [
        new()
        {
            DocumentTypeCode = "SALES_ORDER",
            DocumentTypeName = "销售订单",
            DefaultPrefix = "SO",
            DefaultDateFormatCode = "yyMM",
            DefaultSequenceLength = 5
        },
        new()
        {
            DocumentTypeCode = "PURCHASE_ORDER",
            DocumentTypeName = "采购订单",
            DefaultPrefix = "PO",
            DefaultDateFormatCode = "yyMM",
            DefaultSequenceLength = 5
        },
        new()
        {
            DocumentTypeCode = "PRODUCTION_ORDER",
            DocumentTypeName = "生产工单",
            DefaultPrefix = "MO",
            DefaultDateFormatCode = "yyMM",
            DefaultSequenceLength = 5
        }
    ];

    /// <summary>
    /// 人资仓储（读取公司组织与单号规则数据）。
    /// </summary>
    private readonly IHrRepository _hrRepository;

    public CompanyOrganizationDocumentNumberRuleApiController(IHrRepository hrRepository)
    {
        _hrRepository = hrRepository;
    }

    /// <summary>
    /// 查询公司组织单号规则列表。
    /// </summary>
    [HttpGet("{companyOrganizationId:int}")]
    public async Task<IActionResult> Get(int companyOrganizationId)
    {
        if (companyOrganizationId <= 0)
        {
            return BadRequest(new { message = "请先保存公司组织后再维护单号规则。" });
        }

        var organization = await _hrRepository.GetCompanyOrganizationByIdAsync(companyOrganizationId);
        if (organization is null)
        {
            return NotFound(new { message = "公司组织不存在或已删除。" });
        }

        var savedRules = await _hrRepository.GetCompanyOrganizationDocumentNumberRulesAsync(companyOrganizationId);
        var rules = RuleDefinitions.Select(definition =>
        {
            var savedRule = savedRules.FirstOrDefault(item =>
                string.Equals(item.DocumentTypeCode, definition.DocumentTypeCode, StringComparison.OrdinalIgnoreCase));

            var rule = savedRule ?? new CompanyOrganizationDocumentNumberRule
            {
                CompanyOrganizationId = companyOrganizationId,
                DocumentTypeCode = definition.DocumentTypeCode,
                Prefix = definition.DefaultPrefix,
                DateFormatCode = definition.DefaultDateFormatCode,
                SequenceLength = definition.DefaultSequenceLength
            };

            return new
            {
                rule.Id,
                rule.CompanyOrganizationId,
                documentTypeCode = definition.DocumentTypeCode,
                documentTypeName = definition.DocumentTypeName,
                prefix = string.IsNullOrWhiteSpace(rule.Prefix) ? definition.DefaultPrefix : rule.Prefix,
                dateFormatCode = string.IsNullOrWhiteSpace(rule.DateFormatCode) ? definition.DefaultDateFormatCode : rule.DateFormatCode,
                sequenceLength = rule.SequenceLength <= 0 ? definition.DefaultSequenceLength : rule.SequenceLength,
                sampleNumber = BuildSampleNumber(
                    string.IsNullOrWhiteSpace(rule.Prefix) ? definition.DefaultPrefix : rule.Prefix,
                    string.IsNullOrWhiteSpace(rule.DateFormatCode) ? definition.DefaultDateFormatCode : rule.DateFormatCode,
                    rule.SequenceLength <= 0 ? definition.DefaultSequenceLength : rule.SequenceLength),
                lastGeneratedNumber = rule.LastGeneratedNumber
            };
        });

        return Ok(new
        {
            items = rules,
            dateFormatOptions = DateFormatOptions
        });
    }

    /// <summary>
    /// 保存公司组织单号规则列表。
    /// </summary>
    [HttpPost("{companyOrganizationId:int}")]
    public async Task<IActionResult> Save(int companyOrganizationId, [FromBody] SaveCompanyOrganizationDocumentNumberRulesRequest request)
    {
        if (companyOrganizationId <= 0)
        {
            return BadRequest(new { message = "请先保存公司组织后再维护单号规则。" });
        }

        var organization = await _hrRepository.GetCompanyOrganizationByIdAsync(companyOrganizationId);
        if (organization is null)
        {
            return NotFound(new { message = "公司组织不存在或已删除。" });
        }

        var items = request.Items?.ToList() ?? [];
        if (items.Count == 0)
        {
            return BadRequest(new { message = "请至少保留一条单号规则记录。" });
        }

        var duplicatedCodes = items
            .GroupBy(item => NormalizeDocumentTypeCode(item.DocumentTypeCode), StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();
        if (duplicatedCodes.Count > 0)
        {
            return BadRequest(new { message = "单据功能不可重复配置单号规则。" });
        }

        var allowedCodes = RuleDefinitions
            .Select(definition => definition.DocumentTypeCode)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var operatorName = ResolveOperatorName();
        var rules = new List<CompanyOrganizationDocumentNumberRule>();

        foreach (var item in items)
        {
            var normalizedCode = NormalizeDocumentTypeCode(item.DocumentTypeCode);
            if (!allowedCodes.Contains(normalizedCode))
            {
                return BadRequest(new { message = $"不支持的单据功能：{normalizedCode}" });
            }

            var definition = RuleDefinitions.First(def => string.Equals(def.DocumentTypeCode, normalizedCode, StringComparison.OrdinalIgnoreCase));
            var normalizedPrefix = string.IsNullOrWhiteSpace(item.Prefix)
                ? definition.DefaultPrefix
                : item.Prefix.Trim().ToUpperInvariant();
            var normalizedDateFormatCode = NormalizeDateFormatCode(item.DateFormatCode, definition.DefaultDateFormatCode);
            var normalizedSequenceLength = item.SequenceLength switch
            {
                < 1 => definition.DefaultSequenceLength,
                > 12 => 12,
                _ => item.SequenceLength
            };

            rules.Add(new CompanyOrganizationDocumentNumberRule
            {
                Id = item.Id,
                CompanyOrganizationId = companyOrganizationId,
                DocumentTypeCode = normalizedCode,
                Prefix = normalizedPrefix,
                DateFormatCode = normalizedDateFormatCode,
                SequenceLength = normalizedSequenceLength,
                CreatedBy = operatorName,
                UpdatedBy = operatorName
            });
        }

        var affected = await _hrRepository.SaveCompanyOrganizationDocumentNumberRulesAsync(companyOrganizationId, rules);
        return Ok(new { success = true, affected });
    }

    /// <summary>
    /// 解析当前操作人名称（优先使用登录账号声明）。
    /// </summary>
    private string ResolveOperatorName()
        => User.FindFirstValue("erp:account")
            ?? User.Identity?.Name
            ?? "system";

    /// <summary>
    /// 生成页面展示用的单号例子（不占用真实流水号）。
    /// </summary>
    private static string BuildSampleNumber(string prefix, string dateFormatCode, int sequenceLength)
    {
        var dateSegment = dateFormatCode switch
        {
            "NONE" => string.Empty,
            "yyyyMM" => DateTime.Today.ToString("yyyyMM"),
            "yyyyMMdd" => DateTime.Today.ToString("yyyyMMdd"),
            _ => DateTime.Today.ToString("yyMM")
        };

        return $"{prefix}{dateSegment}{1.ToString().PadLeft(Math.Max(sequenceLength, 1), '0')}";
    }

    /// <summary>
    /// 规范单据功能编码（统一转为大写形式）。
    /// </summary>
    private static string NormalizeDocumentTypeCode(string? documentTypeCode)
        => string.IsNullOrWhiteSpace(documentTypeCode)
            ? string.Empty
            : documentTypeCode.Trim().ToUpperInvariant();

    /// <summary>
    /// 规范日期格式编码（不在允许选项中时回退到默认值）。
    /// </summary>
    private static string NormalizeDateFormatCode(string? dateFormatCode, string defaultCode)
    {
        var normalizedCode = string.IsNullOrWhiteSpace(dateFormatCode)
            ? defaultCode
            : dateFormatCode.Trim();

        return DateFormatOptions.Any(option => string.Equals(option.Value, normalizedCode, StringComparison.OrdinalIgnoreCase))
            ? normalizedCode
            : defaultCode;
    }
}

/// <summary>
/// 单号规则固定选项项（用于日期格式下拉框）。
/// </summary>
public class DocumentNumberRuleOptionItem
{
    /// <summary>
    /// 选项值（固定编码）。
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// 选项文本（页面显示名称）。
    /// </summary>
    public string Text { get; set; } = string.Empty;

    public DocumentNumberRuleOptionItem()
    {
    }

    public DocumentNumberRuleOptionItem(string value, string text)
    {
        Value = value;
        Text = text;
    }
}

/// <summary>
/// 保存公司组织单号规则请求（对应单号规则页签整表保存）。
/// </summary>
public class SaveCompanyOrganizationDocumentNumberRulesRequest
{
    /// <summary>
    /// 单号规则列表（按单据功能逐条传回）。
    /// </summary>
    public List<SaveCompanyOrganizationDocumentNumberRuleItem> Items { get; set; } = [];
}

/// <summary>
/// 保存公司组织单号规则请求项（对应一条单据功能规则）。
/// </summary>
public class SaveCompanyOrganizationDocumentNumberRuleItem
{
    /// <summary>
    /// 规则记录ID（0 表示新增）。
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 单据功能编码（固定功能编码）。
    /// </summary>
    public string? DocumentTypeCode { get; set; }

    /// <summary>
    /// 单号前缀（生成时拼接在日期片段前）。
    /// </summary>
    public string? Prefix { get; set; }

    /// <summary>
    /// 日期格式编码（固定选项）。
    /// </summary>
    public string? DateFormatCode { get; set; }

    /// <summary>
    /// 流水位数（流水号左侧补零位数）。
    /// </summary>
    public int SequenceLength { get; set; }
}
