namespace OpenERP.Web.Areas.HR.ViewModels.Employees;

/// <summary>
/// 人员管理列表页视图模型（包含员工列表和统计信息）。
/// </summary>
public class EmployeeIndexViewModel
{
    /// <summary>
    /// 员工列表记录集合（表格数据源）。
    /// </summary>
    public IReadOnlyList<EmployeeListItemViewModel> Records { get; set; } = [];

    /// <summary>
    /// 当前初始筛选结果总数（前端加载时与总数一致）。
    /// </summary>
    public int FilteredCount { get; set; }

    /// <summary>
    /// 员工记录总数（用于列表统计展示）。
    /// </summary>
    public int TotalCount { get; set; }
}
