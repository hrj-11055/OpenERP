namespace OpenERP.Web.Areas.HR.ViewModels.Employees;

/// <summary>
/// 人员管理列表项视图模型（用于员工列表表格展示）。
/// </summary>
public class EmployeeListItemViewModel
{
    /// <summary>
    /// 员工ID（对应 Employee 实体主键）。
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 序号（列表展示序号，不入库）。
    /// </summary>
    public int SequenceNo { get; set; }

    /// <summary>
    /// 员工编号（业务编码，未配置时显示演示编码）。
    /// </summary>
    public string EmployeeCode { get; set; } = string.Empty;

    /// <summary>
    /// 员工姓名（列表展示姓名）。
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 在职状态名称（例如在职、离职、已停用）。
    /// </summary>
    public string EmploymentStatusName { get; set; } = string.Empty;

    /// <summary>
    /// 档案标签文本（当前页面统一显示为档案入口）。
    /// </summary>
    public string ArchiveLabel { get; set; } = string.Empty;

    /// <summary>
    /// 员工卡号（未配置时使用演示格式）。
    /// </summary>
    public string CardNumber { get; set; } = string.Empty;

    /// <summary>
    /// 性别名称（用于列表展示）。
    /// </summary>
    public string GenderName { get; set; } = string.Empty;

    /// <summary>
    /// 所属组织名称（优先展示组织简称）。
    /// </summary>
    public string OrganizationName { get; set; } = string.Empty;

    /// <summary>
    /// 部门名称（未配置时显示默认部门）。
    /// </summary>
    public string DepartmentName { get; set; } = string.Empty;

    /// <summary>
    /// 组别名称（未配置时显示默认组别）。
    /// </summary>
    public string GroupName { get; set; } = string.Empty;

    /// <summary>
    /// 职务名称（优先职位名称或手工维护职务）。
    /// </summary>
    public string JobTitle { get; set; } = string.Empty;

    /// <summary>
    /// 联系电话（未维护时显示演示号码）。
    /// </summary>
    public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// 传真号码（未维护时显示演示号码）。
    /// </summary>
    public string Fax { get; set; } = string.Empty;

    /// <summary>
    /// 电子邮箱（未维护时显示演示邮箱）。
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// 备注说明（未填写时显示占位文本）。
    /// </summary>
    public string Remarks { get; set; } = string.Empty;

    /// <summary>
    /// 最后修改人（优先 UpdatedBy，回退 CreatedBy）。
    /// </summary>
    public string LastModifiedBy { get; set; } = string.Empty;

    /// <summary>
    /// 最后修改时间文本（优先 UpdatedAt，回退 CreatedAt）。
    /// </summary>
    public string LastModifiedAtText { get; set; } = string.Empty;

    /// <summary>
    /// 前端检索文本（用于关键字筛选）。
    /// </summary>
    public string SearchText { get; set; } = string.Empty;
}
