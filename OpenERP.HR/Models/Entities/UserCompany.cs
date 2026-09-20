/*
 * File: OpenERP.HR/Models/Entities/UserCompany.cs
 * Description: 用户管辖公司关联实体（定义用户可以登录哪些公司组织）。
 */

namespace OpenERP.HR.Models.Entities;

/// <summary>
/// 用户管辖公司关联实体（对应 HR_UserCompany 表，定义用户可登录的公司组织列表）。
/// </summary>
public class UserCompany
{
    // 员工ID（对应 HR_Employee 表主键）
    public int EmployeeId { get; set; }

    // 公司组织ID（对应 HR_CompanyOrganization 表主键）
    public int CompanyOrganizationId { get; set; }
}
