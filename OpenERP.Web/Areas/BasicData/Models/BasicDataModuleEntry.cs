namespace OpenERP.Web.Areas.BasicData.Models;

/// <summary>
/// 基础数据模块首页入口项（用于展示常用基础数据项目）。
/// </summary>
public class BasicDataModuleEntry
{
    /// <summary>
    /// 基础数据类型编码（对应 BasicDataTypes.TypeCode）。
    /// </summary>
    public string TypeCode { get; init; } = string.Empty;

    /// <summary>
    /// 入口显示名称（业务中文名称）。
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// 入口描述（说明数据用途和来源）。
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// 图标样式（Bootstrap Icon 类名）。
    /// </summary>
    public string IconCss { get; init; } = "bi bi-database";

    /// <summary>
    /// 类型ID（对应基础数据类型主键，空值表示类型尚未初始化）。
    /// </summary>
    public int? TypeId { get; init; }
}
