using System.ComponentModel.DataAnnotations;

namespace OpenERP.HR.Models.Entities;

/// <summary>
/// 员工培训历程实体（对应员工的工作经历、培训经历与教育经历记录）。
/// </summary>
public class EmployeeTrainingExperience : BaseEntity
{
    /// <summary>
    /// 员工ID（对应 Employee 实体主键）。
    /// </summary>
    public int EmployeeId { get; set; }

    /// <summary>
    /// 历程类型编码（固定选项：工作经历、培训经历、教育经历）。
    /// </summary>
    [StringLength(30)]
    public string ExperienceTypeCode { get; set; } = string.Empty;

    /// <summary>
    /// 开始日期（历程开始生效日期）。
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// 结束日期（历程结束日期，可为空表示持续至今）。
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// 历程描述（记录工作内容、培训主题或教育内容）。
    /// </summary>
    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 荣誉证书或培训证书（记录相关证书编号或名称，可为空）。
    /// </summary>
    [StringLength(200)]
    public string? CertificateName { get; set; }

    /// <summary>
    /// 培训/工作机构名称（记录学校、培训机构或任职单位名称）。
    /// </summary>
    [StringLength(200)]
    public string? OrganizationName { get; set; }

    /// <summary>
    /// 档案路径（附件、证明材料或档案入口，可为空）。
    /// </summary>
    [StringLength(500)]
    public string? ArchivePath { get; set; }
}
