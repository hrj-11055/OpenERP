/*
 * File: OpenERP.HR/Models/Entities/Permission.cs
 * Description: 功能权限实体（对应系统导航菜单的功能权限定义）。
 */

using System.ComponentModel.DataAnnotations;

namespace OpenERP.HR.Models.Entities;

/// <summary>
/// 功能权限实体（对应 HR_Permission 表，定义系统各功能模块的访问权限）。
/// </summary>
public class Permission : BaseEntity
{
    // 权限编码（唯一标识，如 DASHBOARD、SALES）
    [StringLength(100)]
    public string PermissionCode { get; set; } = string.Empty;

    // 权限名称（显示名称，如"仪表盘"、"销售管理"）
    [StringLength(200)]
    public string PermissionName { get; set; } = string.Empty;

    // 权限分类（如"业务模块"、"系统管理"等分组）
    [StringLength(100)]
    public string? Category { get; set; }

    // 排序序号（用于权限列表排序显示）
    public int SortOrder { get; set; }
}
