/*
 * File: OpenERP.HR/Models/Entities/Role.cs
 * Description: 角色实体（用于用户权限分组，如系统管理员、普通用户等）。
 */

using System.ComponentModel.DataAnnotations;

namespace OpenERP.HR.Models.Entities;

/// <summary>
/// 角色实体（对应 HR_Role 表，定义系统角色以分组管理用户权限）。
/// </summary>
public class Role : BaseEntity
{
    // 角色编码（唯一标识，如 ADMIN、USER）
    [StringLength(50)]
    public string RoleCode { get; set; } = string.Empty;

    // 角色名称（显示名称，如"系统管理员"、"普通用户"）
    [StringLength(100)]
    public string RoleName { get; set; } = string.Empty;

    // 角色描述（角色的用途说明）
    [StringLength(500)]
    public string? Description { get; set; }

    // 排序序号（用于下拉列表排序）
    public int SortOrder { get; set; }
}
