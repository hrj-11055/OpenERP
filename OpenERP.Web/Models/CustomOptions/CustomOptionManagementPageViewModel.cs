using System.ComponentModel.DataAnnotations;

namespace OpenERP.Web.Models.CustomOptions;

/// <summary>
/// 通用自定义选项维护页视图模型（用于职位、民族、职称等选项的共用管理页面）。
/// </summary>
public class CustomOptionManagementPageViewModel
{
    /// <summary>
    /// 选项来源键（用于区分职位、民族、职称等数据来源）。
    /// </summary>
    public string SourceKey { get; set; } = string.Empty;

    /// <summary>
    /// 页面标题（用于弹窗或独立页顶部展示）。
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 业务名称（例如“职务”“民族”“职称”）。
    /// </summary>
    public string SourceName { get; set; } = string.Empty;

    /// <summary>
    /// 说明字段标签（不同来源可显示“说明”或“备注”）。
    /// </summary>
    public string DescriptionLabel { get; set; } = "说明";

    /// <summary>
    /// 是否显示编码字段（基础数据项需要编码，职位可隐藏）。
    /// </summary>
    public bool SupportsCode { get; set; }

    /// <summary>
    /// 是否显示排序字段（基础数据项需要排序，职位可隐藏）。
    /// </summary>
    public bool SupportsSortOrder { get; set; }

    /// <summary>
    /// 是否显示启用字段（基础数据项可维护启用状态，职位可隐藏）。
    /// </summary>
    public bool SupportsIsActive { get; set; }

    /// <summary>
    /// 当前列表数据（用于左侧展示已维护记录）。
    /// </summary>
    public IReadOnlyList<CustomOptionListItemViewModel> Items { get; set; } = [];

    /// <summary>
    /// 当前编辑器数据（用于右侧新增/修改表单）。
    /// </summary>
    public CustomOptionEditorInput Editor { get; set; } = new();
}

/// <summary>
/// 通用自定义选项列表项视图模型（统一职位和基础数据项列表展示字段）。
/// </summary>
public class CustomOptionListItemViewModel
{
    /// <summary>
    /// 记录ID（对应职位实体或基础数据项实体主键）。
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 选项编码（基础数据来源使用；职位来源为空）。
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// 选项名称（页面主展示字段）。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 排序号（基础数据来源使用；职位来源为空）。
    /// </summary>
    public int? SortOrder { get; set; }

    /// <summary>
    /// 启用标记（基础数据来源使用；职位来源为空）。
    /// </summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// 说明文本（职位使用说明，基础数据使用备注）。
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// 通用自定义选项编辑输入模型（统一接收新增和修改表单）。
/// </summary>
public class CustomOptionEditorInput
{
    /// <summary>
    /// 记录ID（0 表示新增；大于 0 表示编辑已有记录）。
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 选项来源键（用于识别当前表单操作的数据来源）。
    /// </summary>
    [Required]
    public string SourceKey { get; set; } = string.Empty;

    /// <summary>
    /// 选项编码（基础数据来源使用；对应基础数据项编码）。
    /// </summary>
    [StringLength(100)]
    public string? Code { get; set; }

    /// <summary>
    /// 选项名称（职位名称或基础数据项名称）。
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 排序号（基础数据来源使用；职位来源忽略）。
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// 启用标记（基础数据来源使用；职位来源忽略）。
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 说明文本（职位说明或基础数据备注）。
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }
}
