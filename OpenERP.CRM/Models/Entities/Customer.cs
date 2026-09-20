using System.ComponentModel.DataAnnotations;

namespace OpenERP.CRM.Models.Entities
{
    /// <summary>
    /// 客户资料主表（记录客户/友商的基础资料、地址资料与账务资料）。
    /// </summary>
    public class Customer : BaseEntity
    {
        /// <summary>
        /// 客户编号（业务唯一编码，用于列表检索与单据引用）。
        /// </summary>
        [Required]
        [StringLength(50)]
        public string CustomerCode { get; set; } = string.Empty;

        /// <summary>
        /// 客户名称（客户/友商的正式名称）。
        /// </summary>
        [Required]
        [StringLength(200)]
        public string CustomerName { get; set; } = string.Empty;

        /// <summary>
        /// 客户状态（例如合作中、暂停、终止）。
        /// </summary>
        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "合作中";

        /// <summary>
        /// 客户性质（例如企业、个人、政府机构）。
        /// </summary>
        [Required]
        [StringLength(30)]
        public string CustomerNature { get; set; } = "企业";

        /// <summary>
        /// 客户角色（为 true 时表示该资料可在客户资料功能中使用）。
        /// </summary>
        public bool IsCustomerRole { get; set; } = true;

        /// <summary>
        /// 供应商角色（为 true 时表示该资料同时可作为供应商资料使用）。
        /// </summary>
        public bool IsSupplierRole { get; set; }

        /// <summary>
        /// 企业类型（例如电子、科技、贸易等企业属性）。
        /// </summary>
        [StringLength(50)]
        public string? EnterpriseType { get; set; }

        /// <summary>
        /// 别名或简称（客户常用简称）。
        /// </summary>
        [StringLength(100)]
        public string? AliasName { get; set; }

        /// <summary>
        /// 商业登记号（客户工商或商业登记编号）。
        /// </summary>
        [StringLength(80)]
        public string? BusinessRegistrationNumber { get; set; }

        /// <summary>
        /// 商业登记证明日期（客户登记资料的证明日期）。
        /// </summary>
        public DateOnly? BusinessRegistrationDate { get; set; }

        /// <summary>
        /// 所属集团编码（客户集团或母公司编码，可为空）。
        /// </summary>
        [StringLength(50)]
        public string? GroupCode { get; set; }

        /// <summary>
        /// 所属集团名称（客户集团或母公司名称）。
        /// </summary>
        [StringLength(200)]
        public string? GroupName { get; set; }

        /// <summary>
        /// 国家/地区（客户所在地国家或大区）。
        /// </summary>
        [StringLength(80)]
        public string? CountryRegion { get; set; }

        /// <summary>
        /// 城市（客户所在地城市）。
        /// </summary>
        [StringLength(80)]
        public string? City { get; set; }

        /// <summary>
        /// 县/区（客户所在地行政区）。
        /// </summary>
        [StringLength(80)]
        public string? District { get; set; }

        /// <summary>
        /// 地址（客户详细地址）。
        /// </summary>
        [StringLength(500)]
        public string? Address { get; set; }

        /// <summary>
        /// 负责人（客户内部或我方负责对接人员）。
        /// </summary>
        [StringLength(80)]
        public string? Manager { get; set; }

        /// <summary>
        /// 电话（客户主要联系电话）。
        /// </summary>
        [StringLength(80)]
        public string? Phone { get; set; }

        /// <summary>
        /// 传真（客户传真号码）。
        /// </summary>
        [StringLength(80)]
        public string? Fax { get; set; }

        /// <summary>
        /// 电邮（客户主要邮箱）。
        /// </summary>
        [EmailAddress]
        [StringLength(150)]
        public string? Email { get; set; }

        /// <summary>
        /// 网站（客户官方网站或业务网址）。
        /// </summary>
        [StringLength(200)]
        public string? Website { get; set; }

        /// <summary>
        /// 备注（客户基础资料补充说明）。
        /// </summary>
        [StringLength(1000)]
        public string? Remarks { get; set; }

        /// <summary>
        /// 存档路径（旧字段保留；实际附件统一使用通用文档管理）。
        /// </summary>
        [StringLength(300)]
        public string? ArchivePath { get; set; }

        /// <summary>
        /// 付款单位编码（账务付款主体编码）。
        /// </summary>
        [StringLength(50)]
        public string? PayerCode { get; set; }

        /// <summary>
        /// 单位名称（账务付款主体名称）。
        /// </summary>
        [StringLength(200)]
        public string? PayerName { get; set; }

        /// <summary>
        /// 银行账户（客户收付款银行账号）。
        /// </summary>
        [StringLength(100)]
        public string? BankAccount { get; set; }

        /// <summary>
        /// 开户银行（银行账户对应开户行）。
        /// </summary>
        [StringLength(120)]
        public string? BankName { get; set; }

        /// <summary>
        /// 转款代码（银行转账或内部付款代码）。
        /// </summary>
        [StringLength(80)]
        public string? TransferCode { get; set; }

        /// <summary>
        /// 货币（客户账务默认币种）。
        /// </summary>
        [StringLength(20)]
        public string? Currency { get; set; } = "RMB";

        /// <summary>
        /// 信贷限额（允许客户赊账的最高额度）。
        /// </summary>
        public decimal CreditLimit { get; set; }

        /// <summary>
        /// 信贷期限（客户账期或付款期限）。
        /// </summary>
        [StringLength(50)]
        public string? CreditTerm { get; set; }

        /// <summary>
        /// 默认计价（客户默认报价或价目表）。
        /// </summary>
        [StringLength(50)]
        public string? DefaultPriceCategory { get; set; }

        /// <summary>
        /// 付款方式（客户默认付款方式）。
        /// </summary>
        [StringLength(50)]
        public string? PaymentMethod { get; set; }

        /// <summary>
        /// 最少订货金额（客户订单最低金额限制）。
        /// </summary>
        public decimal MinimumOrderAmount { get; set; }

        /// <summary>
        /// 默认税率（客户账务默认税率文本）。
        /// </summary>
        [StringLength(50)]
        public string? DefaultTaxRate { get; set; }

        /// <summary>
        /// 冻结账户（冻结后用于限制后续交易）。
        /// </summary>
        public bool IsAccountFrozen { get; set; }

        /// <summary>
        /// 账户备注（客户账务资料补充说明）。
        /// </summary>
        [StringLength(1000)]
        public string? AccountRemarks { get; set; }

        /// <summary>
        /// 客户联络人集合（客户资料从表）。
        /// </summary>
        public ICollection<CustomerContact> Contacts { get; set; } = new List<CustomerContact>();
    }
}
