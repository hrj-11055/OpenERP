using Microsoft.AspNetCore.Mvc.Rendering;

namespace OpenERP.Web.Areas.HR.ViewModels.CompanyOrganizations;

/// <summary>
/// 公司组织列表页视图模型（包含查询条件、筛选项和表格数据）。
/// </summary>
public class CompanyOrganizationIndexViewModel
{
    /// <summary>
    /// 关键字（用于组织编号、组织名称、公司名称、负责人检索）。
    /// </summary>
    public string Keyword { get; set; } = string.Empty;

    /// <summary>
    /// 状态ID（对应基础数据字典 ORG_STATUS）。
    /// </summary>
    public int? StatusId { get; set; }

    /// <summary>
    /// 状态下拉选项（来自基础数据字典 ORG_STATUS）。
    /// </summary>
    public IReadOnlyList<SelectListItem> StatusOptions { get; set; } = [];

    /// <summary>
    /// 公司组织记录集合（表格数据源）。
    /// </summary>
    public IReadOnlyList<CompanyOrganizationListItemViewModel> Records { get; set; } = [];

    /// <summary>
    /// 人员管理卡片集合（按组织汇总对应员工侧边栏数据）。
    /// </summary>
    public IReadOnlyList<CompanyOrganizationEmployeeCardViewModel> EmployeeCards { get; set; } = [];

    /// <summary>
    /// 默认选中的组织ID（用于初始化右侧人员管理卡片）。
    /// </summary>
    public int? DefaultOrganizationId { get; set; }

    /// <summary>
    /// 当前筛选结果总数（列表展示统计）。
    /// </summary>
    public int FilteredCount { get; set; }

    /// <summary>
    /// 全部记录总数（未筛选统计）。
    /// </summary>
    public int TotalCount { get; set; }
}
