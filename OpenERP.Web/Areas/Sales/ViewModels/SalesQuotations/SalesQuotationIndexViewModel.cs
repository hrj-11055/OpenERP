namespace OpenERP.Web.Areas.Sales.ViewModels.SalesQuotations
{
    /// <summary>
    /// 销售报价列表页模型（包含报价主表列表与当前报价货品明细）。
    /// </summary>
    public class SalesQuotationIndexViewModel
    {
        /// <summary>
        /// 查询范围（全部、报价单号、客户编号、客户名称等）。
        /// </summary>
        public string Scope { get; set; } = "全部";

        /// <summary>
        /// 查询关键字（用于过滤报价主表记录）。
        /// </summary>
        public string? Keyword { get; set; }

        /// <summary>
        /// 状态过滤（全部、正常、作废、失效）。
        /// </summary>
        public string StatusFilter { get; set; } = "全部";

        /// <summary>
        /// 当前选中报价单ID。
        /// </summary>
        public int? SelectedQuotationId { get; set; }

        /// <summary>
        /// 当前选中报价单号（用于下方区域标题显示）。
        /// </summary>
        public string SelectedQuotationNumber { get; set; } = "未选择报价单";

        /// <summary>
        /// 当前激活的页签名称（货品清单、下款内容、任务跟进、关联单据）。
        /// </summary>
        public string ActiveTab { get; set; } = "items";

        /// <summary>
        /// 报价主表列表。
        /// </summary>
        public List<SalesQuotationListItemViewModel> Quotations { get; set; } = [];

        /// <summary>
        /// 当前报价单货品明细列表。
        /// </summary>
        public List<SalesQuotationItemViewModel> Items { get; set; } = [];
    }

    /// <summary>
    /// 销售报价列表行模型。
    /// </summary>
    public class SalesQuotationListItemViewModel
    {
        /// <summary>
        /// 报价单ID（对应 SA_SalesQuotation 主键）。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 序号（当前列表显示顺序）。
        /// </summary>
        public int SequenceNo { get; set; }

        /// <summary>
        /// 报价单号（业务唯一编码）。
        /// </summary>
        public string QuotationNumber { get; set; } = string.Empty;

        /// <summary>
        /// 报价日期。
        /// </summary>
        public string QuotationDate { get; set; } = string.Empty;

        /// <summary>
        /// 客户编号。
        /// </summary>
        public string CustomerCode { get; set; } = string.Empty;

        /// <summary>
        /// 客户名称。
        /// </summary>
        public string CustomerName { get; set; } = string.Empty;

        /// <summary>
        /// 状态（正常、作废、失效）。
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 有效截止日期。
        /// </summary>
        public string ValidUntil { get; set; } = string.Empty;

        /// <summary>
        /// 账单联系人。
        /// </summary>
        public string BillingContact { get; set; } = string.Empty;

        /// <summary>
        /// 电话。
        /// </summary>
        public string Phone { get; set; } = string.Empty;

        /// <summary>
        /// 电邮。
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// 备注。
        /// </summary>
        public string Remarks { get; set; } = string.Empty;

        /// <summary>
        /// 最后修改人。
        /// </summary>
        public string LastModifiedBy { get; set; } = string.Empty;
    }

    /// <summary>
    /// 销售报价货品明细行模型。
    /// </summary>
    public class SalesQuotationItemViewModel
    {
        /// <summary>
        /// 货品明细ID。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 序号。
        /// </summary>
        public int SequenceNo { get; set; }

        /// <summary>
        /// 货品编号。
        /// </summary>
        public string ProductCode { get; set; } = string.Empty;

        /// <summary>
        /// 货品名称。
        /// </summary>
        public string ProductName { get; set; } = string.Empty;

        /// <summary>
        /// 数量。
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// 单位。
        /// </summary>
        public string Unit { get; set; } = string.Empty;

        /// <summary>
        /// 单价。
        /// </summary>
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// 折扣百分比。
        /// </summary>
        public decimal DiscountPercent { get; set; }

        /// <summary>
        /// 金额。
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// 货币。
        /// </summary>
        public string Currency { get; set; } = string.Empty;

        /// <summary>
        /// 汇率。
        /// </summary>
        public decimal ExchangeRate { get; set; }

        /// <summary>
        /// 附注。
        /// </summary>
        public string Notes { get; set; } = string.Empty;
    }
}
