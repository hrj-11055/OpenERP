namespace OpenERP.Web.Data.HR;

/// <summary>
/// 公司组织单号规则定义（描述某个单据功能的默认规则与展示名称）。
/// </summary>
public class CompanyOrganizationDocumentNumberRuleDefinition
{
    /// <summary>
    /// 单据功能编码（系统内部唯一编码）。
    /// </summary>
    public string DocumentTypeCode { get; set; } = string.Empty;

    /// <summary>
    /// 单据功能名称（页面展示名称）。
    /// </summary>
    public string DocumentTypeName { get; set; } = string.Empty;

    /// <summary>
    /// 默认前缀（首次生成规则时使用）。
    /// </summary>
    public string DefaultPrefix { get; set; } = string.Empty;

    /// <summary>
    /// 默认日期格式编码（首次生成规则时使用）。
    /// </summary>
    public string DefaultDateFormatCode { get; set; } = "yyMM";

    /// <summary>
    /// 默认流水位数（首次生成规则时使用）。
    /// </summary>
    public int DefaultSequenceLength { get; set; } = 5;
}
