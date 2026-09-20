using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenERP.HR.Models.Entities;

/// <summary>
/// 公司组织银行账号记录实体（对应 HR_CompanyOrganizationBankAccount 财务账号明细表）。
/// </summary>
[Table("HR_CompanyOrganizationBankAccount")]
public class CompanyOrganizationBankAccount : BaseEntity
{
    /// <summary>
    /// 公司组织ID（对应 CompanyOrganization 实体主键）。
    /// </summary>
    public int CompanyOrganizationId { get; set; }

    /// <summary>
    /// 银行账号（公司对外收付款账号，必填）。
    /// </summary>
    [Required]
    [StringLength(80)]
    public string AccountNumber { get; set; } = string.Empty;

    /// <summary>
    /// 开户银行ID（对应基础数据字典 BANK，可为空）。
    /// </summary>
    public int? BankId { get; set; }

    /// <summary>
    /// 开户银行名称（只读展示字段，来自基础数据字典，不入库）。
    /// </summary>
    [NotMapped]
    public string? BankName { get; set; }

    /// <summary>
    /// 开户支行名称（银行支行/分行名称，必填）。
    /// </summary>
    [Required]
    [StringLength(200)]
    public string BranchName { get; set; } = string.Empty;

    /// <summary>
    /// 开户支行地址（开户地址说明，可为空）。
    /// </summary>
    [StringLength(300)]
    public string? BranchAddress { get; set; }

    /// <summary>
    /// 货币编码（固定选项，如 RMB/USD/HKD）。
    /// </summary>
    [Required]
    [StringLength(10)]
    public string CurrencyCode { get; set; } = "RMB";

    /// <summary>
    /// 状态编码（固定选项，如 NORMAL/DISABLED/CANCELLED）。
    /// </summary>
    [Required]
    [StringLength(20)]
    public string StatusCode { get; set; } = "NORMAL";

    /// <summary>
    /// 科目编号（财务科目标识，可为空）。
    /// </summary>
    [StringLength(50)]
    public string? SubjectCode { get; set; }

    /// <summary>
    /// 科目名称（财务科目名称，可为空）。
    /// </summary>
    [StringLength(100)]
    public string? SubjectName { get; set; }

    /// <summary>
    /// 备注（账号补充说明，可为空）。
    /// </summary>
    [StringLength(500)]
    public string? Remarks { get; set; }

    /// <summary>
    /// 是否默认账号（true 表示当前组织默认银行账号）。
    /// </summary>
    public bool IsDefault { get; set; }
}
