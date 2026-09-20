/*
 * File: OpenERP.HR/Models/Entities/Employee.cs
 * Description: Domain entity definition for Employee in the OpenERP.HR module.
 */

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenERP.HR.Models.Entities
{
    public class Employee : BaseEntity
    {
        // 员工编号（业务唯一编码）
        [StringLength(30)]
        public string? EmployeeCode { get; set; }

        // 名（员工英文名或拼音名）
        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        // 姓（员工英文姓或拼音姓）
        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        // 所属组织ID（对应组织实体 CompanyOrganization）
        public int? OrganizationId { get; set; }

        [ForeignKey("OrganizationId")]
        public virtual CompanyOrganization? Organization { get; set; }

        // 邮箱（员工主邮箱）
        [Required]
        [EmailAddress]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        // 电话1（主联系电话）
        [Phone]
        [StringLength(50)]
        public string? PhoneNumber { get; set; }

        // 电话2（备用联系电话）
        [Phone]
        [StringLength(50)]
        public string? PhoneNumber2 { get; set; }

        // 电话3（备用联系电话）
        [Phone]
        [StringLength(50)]
        public string? PhoneNumber3 { get; set; }

        // 职务/岗位名称（文本）
        [StringLength(100)]
        public string? JobTitle { get; set; }

        // 部门ID（对应部门实体 Department）
        public int? DepartmentId { get; set; }

        [ForeignKey("DepartmentId")]
        public virtual Department? Department { get; set; }

        // 职位ID（对应职位实体 Position）
        public int? PositionId { get; set; }

        [ForeignKey("PositionId")]
        public virtual Position? Position { get; set; }

        // 组别ID（对应基础数据字典：员工组别）
        public int? GroupId { get; set; }

        // 在职状态ID（对应基础数据字典：在职状态）
        public int? EmploymentStatusId { get; set; }

        // 工资级别ID（对应基础数据字典：工资级别）
        public int? SalaryGradeId { get; set; }

        // 支薪公司ID（对应组织实体 CompanyOrganization）
        public int? PayrollCompanyId { get; set; }

        [ForeignKey("PayrollCompanyId")]
        public virtual CompanyOrganization? PayrollCompany { get; set; }

        // 银行账号（员工收款银行账号）
        [StringLength(80)]
        public string? BankAccountNumber { get; set; }

        // 账户名称（银行开户名）
        [StringLength(100)]
        public string? BankAccountName { get; set; }

        // 所属银行ID（对应基础数据字典：银行）
        public int? BankId { get; set; }

        // 别名（员工内部简称）
        [StringLength(100)]
        public string? AliasName { get; set; }

        // 性别ID（对应基础数据字典：性别）
        public int? GenderId { get; set; }

        // 出生日期
        public DateTime? BirthDate { get; set; }

        // 身份证号
        [StringLength(30)]
        public string? IdCardNumber { get; set; }

        // 员工卡号（员工实体卡号，可为空；在职员工范围内需唯一）
        [StringLength(50)]
        public string? CardNumber { get; set; }

        // 民族ID（对应基础数据字典：民族）
        public int? EthnicityId { get; set; }

        // 婚姻状况ID（对应基础数据字典：婚姻状况）
        public int? MaritalStatusId { get; set; }

        // 学历ID（对应基础数据字典：学历）
        public int? EducationLevelId { get; set; }

        // 学历证书号
        [StringLength(100)]
        public string? EducationCertificateNumber { get; set; }

        // 职称ID（对应基础数据字典：职称）
        public int? ProfessionalTitleId { get; set; }

        // 国家/地区ID（对应基础数据字典：国家地区）
        public int? CountryRegionId { get; set; }

        // 城市ID（对应基础数据字典：城市）
        public int? CityId { get; set; }

        // 区县ID（对应基础数据字典：区县）
        public int? CountyId { get; set; }

        // 地址
        [StringLength(300)]
        public string? Address { get; set; }

        // 备注
        [StringLength(1000)]
        public string? Remarks { get; set; }

        // 紧急联系人
        [StringLength(50)]
        public string? EmergencyContact { get; set; }

        // 紧急联系人电话
        [Phone]
        [StringLength(50)]
        public string? EmergencyContactPhone { get; set; }

        // 介绍人
        [StringLength(50)]
        public string? Referrer { get; set; }

        // 存档路径（电子档案目录）
        [StringLength(500)]
        public string? ArchivePath { get; set; }

        // 员工照片路径
        [StringLength(500)]
        public string? PhotoPath { get; set; }

        // 登录账号（用于系统登录认证，建议唯一）
        [StringLength(100)]
        public string? LoginAccount { get; set; }

        // 登录密码（建议存储加密摘要，不保存明文）
        [StringLength(200)]
        public string? LoginPassword { get; set; }

        // 强制查看记录日数（登录后需要查看的历史记录天数）
        public int? ForceViewRecordDays { get; set; }

        // 角色ID（对应权限角色字典/角色实体）
        public int? RoleId { get; set; }

        // 有效日期（账号失效日期）
        public DateTime? AccountValidUntil { get; set; }

        // 冻结账号（true 表示账号被冻结）
        public bool IsAccountFrozen { get; set; } = false;

        // 入职日期
        public DateTime? HireDate { get; set; }

        // 离职日期
        public DateTime? LeaveDate { get; set; }

        // 受雇方式ID（对应基础数据字典：受雇方式）
        public int? EmploymentTypeId { get; set; }

        // 津贴待遇ID（对应基础数据字典：津贴待遇）
        public int? AllowancePackageId { get; set; }

        // 试用期日期（通常为试用期截止日期）
        public DateTime? ProbationEndDate { get; set; }

        // 年假计算方式ID（对应基础数据字典：年假计算方式）
        public int? AnnualLeaveCalculationMethodId { get; set; }

        // 是否考勤（true 表示纳入考勤管理）
        public bool IsAttendanceRequired { get; set; } = true;

        // 本年享有年假日数（当前自然年度可享有年假总天数）
        public decimal? CurrentYearAnnualLeaveDays { get; set; }

        // 年假最多累积日数（允许跨年累计的最大年假天数）
        public decimal? AnnualLeaveMaxAccumulatedDays { get; set; }

        // 剩余年假日数（当前剩余可用年假天数）
        public decimal? AnnualLeaveRemainingDays { get; set; }

        // 入职期满年数可递增年假（达到该年数后开始递增年假）
        public int? AnnualLeaveIncrementStartYears { get; set; }

        // 每年递增年假日数（入职满年后每年递增的年假天数）
        public decimal? AnnualLeaveIncrementPerYearDays { get; set; }

        // 年假封顶日数（年假总额上限天数）
        public decimal? AnnualLeaveCapDays { get; set; }

        // 默认上班班次ID（对应基础数据字典：班次）
        public int? DefaultShiftId { get; set; }

        // 排班组别ID（对应基础数据字典：排班组别）
        public int? SchedulingGroupId { get; set; }

        // 自动排班（true 表示参与系统自动排班）
        public bool IsAutoSchedulingEnabled { get; set; } = false;

        // 只读显示名（不入库）
        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";
    }
}
