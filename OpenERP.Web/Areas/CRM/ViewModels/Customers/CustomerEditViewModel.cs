using System.ComponentModel.DataAnnotations;

namespace OpenERP.Web.Areas.CRM.ViewModels.Customers
{
    /// <summary>
    /// 客户资料详情页编辑模型（包含客户主表字段与联络人明细行）。
    /// </summary>
    public class CustomerEditViewModel
    {
        /// <summary>
        /// 客户ID（对应 CRM_Customer 主键；新增时为空）。
        /// </summary>
        public int? Id { get; set; }

        /// <summary>
        /// 客户编号（业务唯一编码）。
        /// </summary>
        [Required(ErrorMessage = "请输入客户编号。")]
        [StringLength(50, ErrorMessage = "客户编号不能超过 50 个字符。")]
        public string CustomerCode { get; set; } = string.Empty;

        /// <summary>
        /// 客户名称（客户正式名称）。
        /// </summary>
        [Required(ErrorMessage = "请输入客户名称。")]
        [StringLength(200, ErrorMessage = "客户名称不能超过 200 个字符。")]
        public string CustomerName { get; set; } = string.Empty;

        /// <summary>
        /// 客户状态（合作中、暂停、终止等）。
        /// </summary>
        [Required(ErrorMessage = "请选择客户状态。")]
        public string Status { get; set; } = "合作中";

        /// <summary>
        /// 客户性质（企业、个人等）。
        /// </summary>
        [Required(ErrorMessage = "请选择客户性质。")]
        public string CustomerNature { get; set; } = "企业";

        /// <summary>
        /// 客户角色（客户资料页面新增时默认启用）。
        /// </summary>
        public bool IsCustomerRole { get; set; } = true;

        /// <summary>
        /// 供应商角色（勾选“兼顾供应商”时启用）。
        /// </summary>
        public bool IsSupplierRole { get; set; }

        /// <summary>
        /// 企业类型（客户企业属性）。
        /// </summary>
        public string? EnterpriseType { get; set; } = "电子";

        /// <summary>
        /// 别名或简称（客户常用简称）。
        /// </summary>
        public string? AliasName { get; set; }

        /// <summary>
        /// 商业登记号（工商或商业登记编号）。
        /// </summary>
        public string? BusinessRegistrationNumber { get; set; }

        /// <summary>
        /// 商业登记证明日期（登记资料证明日期）。
        /// </summary>
        [DataType(DataType.Date)]
        public DateOnly? BusinessRegistrationDate { get; set; }

        /// <summary>
        /// 所属集团编码（客户集团编码）。
        /// </summary>
        public string? GroupCode { get; set; }

        /// <summary>
        /// 所属集团名称（客户集团名称）。
        /// </summary>
        public string? GroupName { get; set; }

        /// <summary>
        /// 国家/地区（客户所在地国家或大区）。
        /// </summary>
        public string? CountryRegion { get; set; } = "中国华南";

        /// <summary>
        /// 城市（客户所在地城市）。
        /// </summary>
        public string? City { get; set; } = "广州市";

        /// <summary>
        /// 县/区（客户所在地行政区）。
        /// </summary>
        public string? District { get; set; } = "天河区";

        /// <summary>
        /// 地址（客户详细地址）。
        /// </summary>
        public string? Address { get; set; }

        /// <summary>
        /// 负责人（客户主要负责人或我方对接人）。
        /// </summary>
        public string? Manager { get; set; }

        /// <summary>
        /// 电话（客户主要电话）。
        /// </summary>
        public string? Phone { get; set; }

        /// <summary>
        /// 传真（客户传真）。
        /// </summary>
        public string? Fax { get; set; }

        /// <summary>
        /// 电邮（客户主要邮箱）。
        /// </summary>
        [EmailAddress(ErrorMessage = "请输入有效的电邮地址。")]
        public string? Email { get; set; }

        /// <summary>
        /// 网站（客户官方网站）。
        /// </summary>
        public string? Website { get; set; }

        /// <summary>
        /// 备注（客户基础资料补充说明）。
        /// </summary>
        public string? Remarks { get; set; }

        /// <summary>
        /// 存档路径（旧字段保留；附件统一走通用文档管理）。
        /// </summary>
        public string? ArchivePath { get; set; }

        /// <summary>
        /// 付款单位编码（账务付款主体编码）。
        /// </summary>
        public string? PayerCode { get; set; }

        /// <summary>
        /// 单位名称（账务付款主体名称）。
        /// </summary>
        public string? PayerName { get; set; }

        /// <summary>
        /// 银行账户（客户收付款账号）。
        /// </summary>
        public string? BankAccount { get; set; }

        /// <summary>
        /// 开户银行（银行账户开户行）。
        /// </summary>
        public string? BankName { get; set; }

        /// <summary>
        /// 转款代码（银行转账或内部付款代码）。
        /// </summary>
        public string? TransferCode { get; set; }

        /// <summary>
        /// 货币（默认账务币种）。
        /// </summary>
        public string? Currency { get; set; } = "RMB";

        /// <summary>
        /// 信贷限额（允许客户赊账的最高额度）。
        /// </summary>
        public decimal CreditLimit { get; set; } = 1000000M;

        /// <summary>
        /// 信贷期限（账期或付款期限）。
        /// </summary>
        public string? CreditTerm { get; set; } = "现结";

        /// <summary>
        /// 默认计价（默认报价策略）。
        /// </summary>
        public string? DefaultPriceCategory { get; set; } = "计价一";

        /// <summary>
        /// 付款方式（默认收付款方式）。
        /// </summary>
        public string? PaymentMethod { get; set; } = "现金";

        /// <summary>
        /// 最少订货金额（订单最低金额限制）。
        /// </summary>
        public decimal MinimumOrderAmount { get; set; } = 100000M;

        /// <summary>
        /// 默认税率（客户默认税率）。
        /// </summary>
        public string? DefaultTaxRate { get; set; }

        /// <summary>
        /// 冻结账户（冻结后限制交易）。
        /// </summary>
        public bool IsAccountFrozen { get; set; }

        /// <summary>
        /// 账户备注（账务资料补充说明）。
        /// </summary>
        public string? AccountRemarks { get; set; }

        /// <summary>
        /// 客户联络人明细行。
        /// </summary>
        public List<CustomerContactInputModel> Contacts { get; set; } = [];
    }
}
