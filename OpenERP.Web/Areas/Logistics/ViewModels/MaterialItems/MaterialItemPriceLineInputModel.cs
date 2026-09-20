namespace OpenERP.Web.Areas.Logistics.ViewModels.MaterialItems
{
    /// <summary>
    /// 产品售价设置输入模型（用于详情页售价设置页签）。
    /// </summary>
    public class MaterialItemPriceLineInputModel
    {
        /// <summary>
        /// 售价明细ID（对应 BD_ItemPriceLine 主键；新增时为空）。
        /// </summary>
        public int? Id { get; set; }

        /// <summary>
        /// 删除标记（页面软删除明细行）。
        /// </summary>
        public bool IsDeleted { get; set; }

        /// <summary>
        /// 价格类别（价格表名称）。
        /// </summary>
        public string? PriceCategory { get; set; }

        /// <summary>
        /// 数量（价格阶梯数量）。
        /// </summary>
        public decimal Quantity { get; set; } = 1M;

        /// <summary>
        /// 价格（当前价格类别售价）。
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// 货币（售价币种）。
        /// </summary>
        public string Currency { get; set; } = "RMB";
    }
}
