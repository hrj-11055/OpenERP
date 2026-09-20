using OpenERP.HR.Models.Entities;

namespace OpenERP.Web.Data.HR;

/// <summary>
/// 员工培训历程分页结果（封装当前页记录与总记录数）。
/// </summary>
public class EmployeeTrainingExperiencePageResult
{
    /// <summary>
    /// 当前页记录（当前筛选条件下返回的员工培训历程列表）。
    /// </summary>
    public List<EmployeeTrainingExperience> Records { get; set; } = [];

    /// <summary>
    /// 总记录数（当前筛选条件下的全部记录数量）。
    /// </summary>
    public int TotalCount { get; set; }
}
