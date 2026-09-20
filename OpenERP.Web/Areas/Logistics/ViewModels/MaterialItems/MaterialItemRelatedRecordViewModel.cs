namespace OpenERP.Web.Areas.Logistics.ViewModels.MaterialItems
{
    /// <summary>
    /// 产品关联业务记录模型（用于列表底部页签展示历史单据）。
    /// </summary>
    public class MaterialItemRelatedRecordViewModel
    {
        /// <summary>
        /// 序号（关联记录显示顺序）。
        /// </summary>
        public int SequenceNo { get; set; }

        /// <summary>
        /// 单号（销售、采购、入库或出库单号）。
        /// </summary>
        public string DocumentNumber { get; set; } = string.Empty;

        /// <summary>
        /// 档案按钮文案（关联单据查看入口）。
        /// </summary>
        public string ArchiveLabel { get; set; } = "查看";

        /// <summary>
        /// 单据日期（关联业务发生日期）。
        /// </summary>
        public string DocumentDate { get; set; } = string.Empty;

        /// <summary>
        /// 状态（关联单据处理状态）。
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 往来单位编号（供应商或客户编号）。
        /// </summary>
        public string PartnerCode { get; set; } = string.Empty;

        /// <summary>
        /// 往来单位名称（供应商或客户名称）。
        /// </summary>
        public string PartnerName { get; set; } = string.Empty;

        /// <summary>
        /// 数量（关联单据数量）。
        /// </summary>
        public string Quantity { get; set; } = string.Empty;

        /// <summary>
        /// 单位（关联单据计量单位）。
        /// </summary>
        public string Unit { get; set; } = string.Empty;

        /// <summary>
        /// 单价（关联单据价格）。
        /// </summary>
        public string UnitPrice { get; set; } = string.Empty;

        /// <summary>
        /// 金额（关联单据金额）。
        /// </summary>
        public string Amount { get; set; } = string.Empty;

        /// <summary>
        /// 备注（关联记录补充说明）。
        /// </summary>
        public string Remarks { get; set; } = string.Empty;
    }
}
