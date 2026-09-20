using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenERP.HR.Models.Entities;

/// <summary>
/// 公司组织单号规则实体（对应 HR_CompanyOrganizationDocumentNumberRule 单号规则表）。
/// </summary>
[Table("HR_CompanyOrganizationDocumentNumberRule")]
public class CompanyOrganizationDocumentNumberRule : BaseEntity
{
    /// <summary>
    /// 公司组织ID（对应 CompanyOrganization 实体主键）。
    /// </summary>
    public int CompanyOrganizationId { get; set; }

    /// <summary>
    /// 单据功能编码（系统内部功能编码，如 SALES_ORDER）。
    /// </summary>
    [StringLength(50)]
    public string DocumentTypeCode { get; set; } = string.Empty;

    /// <summary>
    /// 单号前缀（生成单号时拼接在日期前方的固定文本）。
    /// </summary>
    [StringLength(20)]
    public string Prefix { get; set; } = string.Empty;

    /// <summary>
    /// 日期格式编码（固定选项，如 NONE、yyMM、yyyyMM、yyyyMMdd）。
    /// </summary>
    [StringLength(20)]
    public string DateFormatCode { get; set; } = "yyMM";

    /// <summary>
    /// 流水位数（流水号左侧补零后的总位数）。
    /// </summary>
    public int SequenceLength { get; set; } = 5;

    /// <summary>
    /// 最近一次生成单号时使用的日期片段（用于日期切换时重置流水号）。
    /// </summary>
    [StringLength(20)]
    public string? LastDateSegment { get; set; }

    /// <summary>
    /// 当前日期片段下最后一次使用的流水号数值。
    /// </summary>
    public int LastSequenceValue { get; set; }

    /// <summary>
    /// 最后一个已生成单号（用于页面展示和追溯）。
    /// </summary>
    [StringLength(80)]
    public string? LastGeneratedNumber { get; set; }

    /// <summary>
    /// 单据功能名称（只读/不入库，页面展示用）。
    /// </summary>
    [NotMapped]
    public string? DocumentTypeName { get; set; }

    /// <summary>
    /// 单号例子（只读/不入库，页面展示用）。
    /// </summary>
    [NotMapped]
    public string? SampleNumber { get; set; }
}
