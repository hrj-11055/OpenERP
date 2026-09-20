using System.ComponentModel.DataAnnotations;

namespace OpenERP.BasicData.Models;

/// <summary>
/// 基础数据选项实体（某个基础数据类型下的具体字典项）。
/// </summary>
public class BasicDataItem
{
    /// <summary>
    /// 基础数据选项ID（主键）。
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 基础数据类型ID（外键，对应 BasicDataType 实体）。
    /// </summary>
    [Required]
    [Display(Name = "所属类型")]
    public int TypeId { get; set; }

    /// <summary>
    /// 所属类型名称（只读展示字段，不入库）。
    /// </summary>
    [StringLength(200)]
    [Display(Name = "所属类型")]
    public string? TypeName { get; set; }

    /// <summary>
    /// 选项编码（同类型内唯一编码）。
    /// </summary>
    [Required]
    [StringLength(100)]
    [Display(Name = "选项编码")]
    public string ItemCode { get; set; } = string.Empty;

    /// <summary>
    /// 选项名称（业务展示名称）。
    /// </summary>
    [Required]
    [StringLength(200)]
    [Display(Name = "选项名称")]
    public string ItemName { get; set; } = string.Empty;

    /// <summary>
    /// 上级选项ID（外键，对应 BasicDataItem 实体；国家地区为空、城市对应国家地区、县域对应城市）。
    /// </summary>
    [Display(Name = "上级选项")]
    public int? ParentId { get; set; }

    /// <summary>
    /// 上级选项名称（只读展示字段，不入库）。
    /// </summary>
    [StringLength(200)]
    [Display(Name = "上级选项")]
    public string? ParentName { get; set; }

    /// <summary>
    /// 排序号（数值越小越靠前）。
    /// </summary>
    [Display(Name = "排序")]
    public int SortOrder { get; set; }

    /// <summary>
    /// 启用标记（true 启用，false 停用）。
    /// </summary>
    [Display(Name = "启用")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 备注（业务补充说明）。
    /// </summary>
    [StringLength(500)]
    [Display(Name = "备注")]
    public string? Remark { get; set; }

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
