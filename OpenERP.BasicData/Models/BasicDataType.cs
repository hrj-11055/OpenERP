using System.ComponentModel.DataAnnotations;

namespace OpenERP.BasicData.Models;

/// <summary>
/// 基础数据类型实体（用于定义某一类字典，如职位/民族/国家地区）。
/// </summary>
public class BasicDataType
{
    /// <summary>
    /// 基础数据类型ID（主键）。
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 类型编码（业务唯一编码，如 POSITION）。
    /// </summary>
    [Required]
    [StringLength(100)]
    [Display(Name = "类型编码")]
    public string TypeCode { get; set; } = string.Empty;

    /// <summary>
    /// 类型名称（业务展示名称）。
    /// </summary>
    [Required]
    [StringLength(200)]
    [Display(Name = "类型名称")]
    public string TypeName { get; set; } = string.Empty;

    /// <summary>
    /// 类型说明（描述业务用途和数据来源）。
    /// </summary>
    [StringLength(500)]
    [Display(Name = "说明")]
    public string? Description { get; set; }

    /// <summary>
    /// 创建时间（入库时间）。
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新时间（最后修改时间）。
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 创建人（操作账号）。
    /// </summary>
    [StringLength(50)]
    public string? CreatedBy { get; set; }

    /// <summary>
    /// 更新人（最后修改账号）。
    /// </summary>
    [StringLength(50)]
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// 逻辑删除标记（true 表示已删除）。
    /// </summary>
    public bool IsDeleted { get; set; }
}
