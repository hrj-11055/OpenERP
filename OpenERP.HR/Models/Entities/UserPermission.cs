/*
 * File: OpenERP.HR/Models/Entities/UserPermission.cs
 * Description: 用户权限关联实体（用户个人与权限的关联，用于个性化权限覆盖）。
 */

namespace OpenERP.HR.Models.Entities;

/// <summary>
/// 用户权限关联实体（对应 HR_UserPermission 表，定义用户个人拥有哪些功能权限）。
/// </summary>
public class UserPermission
{
    // 员工ID（对应 HR_Employee 表主键）
    public int EmployeeId { get; set; }

    // 权限ID（对应 HR_Permission 表主键）
    public int PermissionId { get; set; }
}
