using OpenERP.HR.Models.Entities;

namespace OpenERP.Web.Data.HR;

/// <summary>
/// 任务跟进分页结果（封装当前页记录与总记录数）。
/// </summary>
public class TaskFollowUpPageResult
{
    /// <summary>
    /// 当前页记录（当前查询条件下返回的任务跟进列表）。
    /// </summary>
    public List<TaskFollowUp> Records { get; set; } = [];

    /// <summary>
    /// 总记录数（当前查询条件下的全部记录数）。
    /// </summary>
    public int TotalCount { get; set; }
}
