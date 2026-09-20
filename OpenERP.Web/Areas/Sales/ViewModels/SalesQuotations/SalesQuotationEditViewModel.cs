using System.ComponentModel.DataAnnotations;

namespace OpenERP.Web.Areas.Sales.ViewModels.SalesQuotations
{
    /// <summary>
    /// 销售报价详情页编辑模型（包含报价主表字段与货品明细行）。
    /// </summary>
    public class SalesQuotationEditViewModel
    {
        /// <summary>
        /// 报价单ID（对应 SA_SalesQuotation 主键；新增时为空）。
        /// </summary>
        public int? Id { get; set; }

        /// <summary>
        /// 报价单号（业务唯一编码）。
        /// </summary>
        [Required(ErrorMessage = "请输入报价单号。")]
        [StringLength(30)]
        public string QuotationNumber { get; set; } = string.Empty;

        /// <summary>
        /// 报价日期（报价单开立日期）。
        /// </summary>
        [Required(ErrorMessage = "请选择报价日期。")]
        [DataType(DataType.Date)]
        public DateTime QuotationDate { get; set; } = DateTime.Today;

        /// <summary>
        /// 客户编号（对应客户资料中的客户编码）。
        /// </summary>
        [StringLength(30)]
        public string? CustomerCode { get; set; }

        /// <summary>
        /// 客户名称（报价客户全称或简称）。
        /// </summary>
        [Required(ErrorMessage = "请输入客户名称。")]
        [StringLength(200)]
        public string CustomerName { get; set; } = string.Empty;

        /// <summary>
        /// 状态（例如进行中、已确认、已作废、已失效）。
        /// </summary>
        [Required(ErrorMessage = "请选择状态。")]
        [StringLength(20)]
        public string Status { get; set; } = "进行中";

        /// <summary>
        /// 有效截止日期（报价有效期的最后一天）。
        /// </summary>
        [DataType(DataType.Date)]
        public DateTime? ValidUntil { get; set; }

        /// <summary>
        /// 账单联系人（客户方对接人员姓名）。
        /// </summary>
        [StringLength(50)]
        public string? BillingContact { get; set; }

        /// <summary>
        /// 电话（联系人电话）。
        /// </summary>
        [StringLength(30)]
        public string? Phone { get; set; }

        /// <summary>
        /// 电邮（联系人邮箱）。
        /// </summary>
        [EmailAddress(ErrorMessage = "请输入有效的电邮地址。")]
        [StringLength(100)]
        public string? Email { get; set; }

        /// <summary>
        /// 备注（报价单补充说明）。
        /// </summary>
        [StringLength(500)]
        public string? Remarks { get; set; }

        /// <summary>
        /// 报价单货品明细行。
        /// </summary>
        public List<SalesQuotationItemInputModel> Items { get; set; } = [];
    }

    /// <summary>
    /// 销售报价货品明细编辑行模型。
    /// </summary>
    public class SalesQuotationItemInputModel
    {
        /// <summary>
        /// 明细行ID（对应 SA_SalesQuotationItem 主键；新增时为空）。
        /// </summary>
        public int? Id { get; set; }

        /// <summary>
        /// 软删除标记（前端隐藏字段，提交时标记要删除的明细行）。
        /// </summary>
        public bool IsDeleted { get; set; }

        /// <summary>
        /// 货品编号（产品唯一编码）。
        /// </summary>
        [Required(ErrorMessage = "请输入货品编号。")]
        [StringLength(30)]
        public string ProductCode { get; set; } = string.Empty;

        /// <summary>
        /// 货品名称（产品描述）。
        /// </summary>
        [Required(ErrorMessage = "请输入货品名称。")]
        [StringLength(200)]
        public string ProductName { get; set; } = string.Empty;

        /// <summary>
        /// 数量（报价数量）。
        /// </summary>
        [Required(ErrorMessage = "请输入数量。")]
        [Range(0.01, 999999999, ErrorMessage = "数量必须大于 0。")]
        public decimal Quantity { get; set; }

        /// <summary>
        /// 单位（计量单位）。
        /// </summary>
        [StringLength(20)]
        public string Unit { get; set; } = string.Empty;

        /// <summary>
        /// 单价（每单位报价）。
        /// </summary>
        [Required(ErrorMessage = "请输入单价。")]
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// 折扣百分比（100 表示无折扣）。
        /// </summary>
        public decimal DiscountPercent { get; set; } = 100m;

        /// <summary>
        /// 金额（数量 x 单价 x 折扣 / 100）。
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// 货币（例如 RMB、USD）。
        /// </summary>
        [StringLength(10)]
        public string Currency { get; set; } = "RMB";

        /// <summary>
        /// 汇率（外币对本币的换算比率）。
        /// </summary>
        public decimal ExchangeRate { get; set; } = 1.0000m;

        /// <summary>
        /// 附注（货品行补充说明）。
        /// </summary>
        [StringLength(500)]
        public string? Notes { get; set; }
    }
}
