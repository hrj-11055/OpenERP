using OpenERP.HR.Models.Entities;

namespace OpenERP.Web.Data.HR;

/// <summary>
/// 公司组织银行账号分页结果（用于账务信息页签列表展示）。
/// </summary>
public class CompanyOrganizationBankAccountPageResult
{
    /// <summary>
    /// 当前页码（从 1 开始）。
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// 当前页大小。
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// 总记录数。
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 当前页记录集合。
    /// </summary>
    public IReadOnlyList<CompanyOrganizationBankAccount> Items { get; set; } = [];
}
