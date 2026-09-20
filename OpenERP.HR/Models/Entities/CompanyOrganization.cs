/*
 * File: OpenERP.HR/Models/Entities/CompanyOrganization.cs
 * Description: Domain entity definition for CompanyOrganization in the OpenERP.HR module.
 */

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenERP.HR.Models.Entities
{
    /// <summary>
    /// 公司组织实体映射表（对应 HR_CompanyOrganization 组织主数据表）。
    /// </summary>
    [Table("HR_CompanyOrganization")]
    public class CompanyOrganization : BaseEntity
    {
        // 组织编号（业务唯一编码）
        [Required]
        [StringLength(10)]
        [Display(Name = "组织编号")]
        public string OrganizationCode { get; set; } = string.Empty;

        // 组织名称（公司/组织全称）
        [Required]
        [StringLength(100)]
        [Display(Name = "组织名称")]
        public string OrganizationName { get; set; } = string.Empty;

        // 公司性质ID（对应基础数据字典：公司性质）
        [Display(Name = "公司性质")]
        public int? CompanyNatureId { get; set; }

        // 状态ID（对应基础数据字典：组织状态）
        [Display(Name = "状态")]
        public int? StatusId { get; set; }

        // 企业类型ID（对应基础数据字典：企业类型）
        [Display(Name = "企业类型")]
        public int? EnterpriseTypeId { get; set; }

        // 商业登记证号
        [StringLength(50)]
        [Display(Name = "商业登记证号")]
        public string? BusinessRegistrationNumber { get; set; }

        // 商业登记证到期日
        [Display(Name = "商业登记证到期日")]
        public DateTime? BusinessRegistrationExpiryDate { get; set; }

        // 地区ID（对应基础数据字典：地区）
        [Display(Name = "地区")]
        public int? RegionId { get; set; }

        // 城市ID（对应基础数据字典：城市）
        [Display(Name = "城市")]
        public int? CityId { get; set; }

        // 县区ID（对应基础数据字典：县区）
        [Display(Name = "县区")]
        public int? CountyId { get; set; }

        // 地址
        [StringLength(300)]
        [Display(Name = "地址")]
        public string? Address { get; set; }

        // 负责人
        [StringLength(50)]
        [Display(Name = "负责人")]
        public string? Principal { get; set; }

        // 电话
        [Phone]
        [StringLength(50)]
        [Display(Name = "电话")]
        public string? Phone { get; set; }

        // 传真
        [StringLength(50)]
        [Display(Name = "传真")]
        public string? Fax { get; set; }

        // 邮箱
        [EmailAddress]
        [StringLength(150)]
        [Display(Name = "邮箱")]
        public string? Email { get; set; }

        // 网址
        [Url]
        [StringLength(200)]
        [Display(Name = "网址")]
        public string? Website { get; set; }

        // 每周工作天数（例如 5、6 或其他自定义天数）
        [Display(Name = "每周工作天数")]
        public decimal? WeeklyWorkDays { get; set; }

        // 计根天数方式（1=工作天，2=按月）
        [Display(Name = "计根天数方式")]
        public int? LeaveCountBasisType { get; set; }

        // 假期类型（1=银行假，2=劳工假）
        [Display(Name = "假期类型")]
        public int? HolidayType { get; set; }

        // 计算年假日期（月/日，格式 MM/dd）
        [StringLength(5)]
        [Display(Name = "计算年假日期(月/日)")]
        public string? AnnualLeaveCalculationMonthDay { get; set; }

        // 计算员工每年年假方式（1=日期(月/日)，2=以入职日计，3=以入职月份计）
        [Display(Name = "计算员工每年年假方式")]
        public int? AnnualLeaveGrantRule { get; set; }

        // 当年年假按固定日期计算时使用的日期（月/日，格式 MM/dd）
        [StringLength(5)]
        [Display(Name = "年假固定计算日期(月/日)")]
        public string? AnnualLeaveGrantMonthDay { get; set; }

        // 是否启用清空年假日期
        [Display(Name = "清空年假日期开关")]
        public bool IsAnnualLeaveClearEnabled { get; set; } = false;

        // 清空年假日期（月/日，格式 MM/dd）
        [StringLength(5)]
        [Display(Name = "清空年假日期(月/日)")]
        public string? AnnualLeaveClearMonthDay { get; set; }

        // 本年放上年年假（是否允许年假累积）
        [Display(Name = "允许年假累积")]
        public bool IsCarryForwardAnnualLeaveAllowed { get; set; } = false;

        // 基本年假天数
        [Display(Name = "基本年假天数")]
        public decimal? BaseAnnualLeaveDays { get; set; }

        // 服务满多少年后递增年假
        [Display(Name = "服务满年后递增年假")]
        public int? AnnualLeaveIncrementStartYears { get; set; }

        // 每年递增年假天数
        [Display(Name = "每年递增年假天数")]
        public decimal? AnnualLeaveIncrementPerYearDays { get; set; }

        // 年假封顶天数
        [Display(Name = "年假封顶天数")]
        public decimal? AnnualLeaveCapDays { get; set; }

        // 年假最多累积天数
        [Display(Name = "年假最多累积天数")]
        public decimal? AnnualLeaveMaxAccumulatedDays { get; set; }

        // 每年有薪病假天数
        [Display(Name = "每年有薪病假天数")]
        public decimal? PaidSickLeaveDaysPerYear { get; set; }

        // 有薪病假工资比率（百分比数值，例如 80.00 代表 80%）
        [Display(Name = "有薪病假工资比率")]
        public decimal? PaidSickLeaveSalaryRatio { get; set; }

        // 计算有薪病假日期（月/日，格式 MM/dd）
        [StringLength(5)]
        [Display(Name = "计算有薪病假日期(月/日)")]
        public string? PaidSickLeaveCalculationMonthDay { get; set; }

        // 是否启用有薪病假清空日期
        [Display(Name = "清空有薪病假日期开关")]
        public bool IsPaidSickLeaveClearEnabled { get; set; } = false;

        // 清空有薪病假日期（月/日，格式 MM/dd）
        [StringLength(5)]
        [Display(Name = "清空有薪病假日期(月/日)")]
        public string? PaidSickLeaveClearMonthDay { get; set; }

        // 员工供 MPF 最低薪金（MPF=强积金 Mandatory Provident Fund）
        [Display(Name = "员工供MPF最低薪金")]
        public decimal? EmployeeMpfMinimumSalary { get; set; }

        // 备注
        [StringLength(1000)]
        [Display(Name = "备注")]
        public string? Remarks { get; set; }

        // 存档路径（电子档案目录）
        [StringLength(500)]
        [Display(Name = "存档路径")]
        public string? ArchivePath { get; set; }

        // 打印页头内容（公司级打印模板公共页头，多行文本入库）
        [StringLength(4000)]
        [Display(Name = "打印页头")]
        public string? PrintHeaderContent { get; set; }

        // 打印页脚内容（公司级打印模板公共页脚，多行文本入库）
        [StringLength(4000)]
        [Display(Name = "打印页脚")]
        public string? PrintFooterContent { get; set; }
    }
}
