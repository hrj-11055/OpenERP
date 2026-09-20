namespace OpenERP.Web.Areas.HR.ViewModels.CompanyOrganizations;

/// <summary>
/// 公司组织员工条目视图模型（用于右侧人员管理卡片中的员工列表）。
/// </summary>
public class CompanyOrganizationEmployeeListItemViewModel
{
    /// <summary>
    /// 员工ID（对应 Employee 实体主键）。
    /// </summary>
    public int EmployeeId { get; set; }

    /// <summary>
    /// 员工编号（业务编码，未配置时显示演示编码）。
    /// </summary>
    public string EmployeeCode { get; set; } = string.Empty;

    /// <summary>
    /// 员工姓名（右侧卡片展示姓名）。
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 职务名称（优先职位名称或手工维护职务）。
    /// </summary>
    public string JobTitle { get; set; } = string.Empty;

    /// <summary>
    /// 部门名称（用于卡片中展示员工所属部门）。
    /// </summary>
    public string DepartmentName { get; set; } = string.Empty;

    /// <summary>
    /// 在职状态名称（例如在职、离职）。
    /// </summary>
    public string EmploymentStatusName { get; set; } = string.Empty;

    /// <summary>
    /// 联系电话（优先手机号，未配置时回退占位符）。
    /// </summary>
    public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// 电子邮件（用于快速识别员工联系方式）。
    /// </summary>
    public string Email { get; set; } = string.Empty;
}
