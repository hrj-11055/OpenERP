/*
 * File: OpenERP.HR/Models/Entities/TaskFollowUp.cs
 * Description: 通用任务跟进实体（可复用于员工、客户、合同等业务资料的跟进记录）。
 */

using System.ComponentModel.DataAnnotations;

namespace OpenERP.HR.Models.Entities;

/// <summary>
/// 通用任务跟进实体（对应共享任务跟进表，用 FeatureCode + EntityId 关联具体业务资料）。
/// </summary>
public class TaskFollowUp : BaseEntity
{
    /// <summary>
    /// 功能编码（用于区分来源功能，例如 HR_EMPLOYEE）。
    /// </summary>
    [StringLength(50)]
    public string FeatureCode { get; set; } = string.Empty;

    /// <summary>
    /// 所属个体ID（对应具体业务实体主键，例如员工ID）。
    /// </summary>
    public int EntityId { get; set; }

    /// <summary>
    /// 任务编号（业务展示编号，默认由系统生成）。
    /// </summary>
    [StringLength(30)]
    public string TaskCode { get; set; } = string.Empty;

    /// <summary>
    /// 档案路径（附件或档案入口，留空表示暂无档案）。
    /// </summary>
    [StringLength(500)]
    public string? ArchivePath { get; set; }

    /// <summary>
    /// 状态编码（任务状态固定选项，例如 PENDING、COMPLETED）。
    /// </summary>
    [StringLength(30)]
    public string StatusCode { get; set; } = string.Empty;

    /// <summary>
    /// 计划日期（任务预计执行或计划完成日期）。
    /// </summary>
    public DateTime? PlannedDate { get; set; }

    /// <summary>
    /// 类型编码（任务类型固定选项，例如 TASK、REMINDER）。
    /// </summary>
    [StringLength(30)]
    public string TaskTypeCode { get; set; } = string.Empty;

    /// <summary>
    /// 执行人（任务实际执行人姓名）。
    /// </summary>
    [StringLength(100)]
    public string? ExecutorName { get; set; }

    /// <summary>
    /// 任务描述（任务内容说明）。
    /// </summary>
    [StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// 优先级编码（固定选项，例如 NORMAL、URGENT）。
    /// </summary>
    [StringLength(30)]
    public string PriorityCode { get; set; } = string.Empty;

    /// <summary>
    /// 执行进程（0-100 的百分比整数）。
    /// </summary>
    public int ProgressPercent { get; set; }

    /// <summary>
    /// 完成日期（任务实际完成日期）。
    /// </summary>
    public DateTime? CompletedDate { get; set; }

    /// <summary>
    /// 项目编号（关联项目或外部编号，留空表示未关联）。
    /// </summary>
    [StringLength(50)]
    public string? ProjectCode { get; set; }

    /// <summary>
    /// 发起人（任务创建或指派发起人姓名）。
    /// </summary>
    [StringLength(100)]
    public string? InitiatorName { get; set; }
}
