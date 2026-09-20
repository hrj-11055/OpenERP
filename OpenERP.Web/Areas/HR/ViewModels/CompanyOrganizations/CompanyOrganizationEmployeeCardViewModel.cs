namespace OpenERP.Web.Areas.HR.ViewModels.CompanyOrganizations;

/// <summary>
/// 公司组织对应的人员管理卡片视图模型（右侧边栏员工摘要）。
/// </summary>
public class CompanyOrganizationEmployeeCardViewModel
{
    /// <summary>
    /// 组织ID（对应 CompanyOrganization 实体主键）。
    /// </summary>
    public int OrganizationId { get; set; }

    /// <summary>
    /// 组织编码（用于卡片头部展示和前端切换定位）。
    /// </summary>
    public string OrganizationCode { get; set; } = string.Empty;

    /// <summary>
    /// 组织名称（用于右侧人员管理卡片标题）。
    /// </summary>
    public string OrganizationName { get; set; } = string.Empty;

    /// <summary>
    /// 员工总数（当前组织下的员工记录总数）。
    /// </summary>
    public int EmployeeCount { get; set; }

    /// <summary>
    /// 在职人数（未离职且未删除的员工数量）。
    /// </summary>
    public int ActiveEmployeeCount { get; set; }

    /// <summary>
    /// 涉及部门数（用于卡片摘要展示组织分布）。
    /// </summary>
    public int DepartmentCount { get; set; }

    /// <summary>
    /// 员工卡片明细集合（用于右侧员工条目列表）。
    /// </summary>
    public IReadOnlyList<CompanyOrganizationEmployeeListItemViewModel> Employees { get; set; } = [];
}
