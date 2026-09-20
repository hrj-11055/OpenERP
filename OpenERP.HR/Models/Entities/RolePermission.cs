/*
 * File: OpenERP.HR/Models/Entities/RolePermission.cs
 * Description: 角色权限关联实体（角色与权限的多对多关系）。
 */

namespace OpenERP.HR.Models.Entities;

/// <summary>
/// 角色权限关联实体（对应 HR_RolePermission 表，定义角色拥有哪些功能权限）。
/// </summary>
public class RolePermission
{
    // 角色ID（对应 HR_Role 表主键）
    public int RoleId { get; set; }

    // 权限ID（对应 HR_Permission 表主键）
    public int PermissionId { get; set; }
}
