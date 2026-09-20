using System.ComponentModel.DataAnnotations;

namespace OpenERP.Web.Areas.Logistics.ViewModels.MaterialItems
{
    /// <summary>
    /// 产品 BOM 明细输入模型（用于详情页 BOM 清单页签）。
    /// </summary>
    public class MaterialItemBomLineInputModel
    {
        /// <summary>
        /// BOM 明细ID（对应 BD_ItemBomLine 主键；新增时为空）。
        /// </summary>
        public int? Id { get; set; }

        /// <summary>
        /// 删除标记（页面软删除明细行）。
        /// </summary>
        public bool IsDeleted { get; set; }

        /// <summary>
        /// 物料编号（BOM 子物料编码）。
        /// </summary>
        [StringLength(50, ErrorMessage = "物料编号不能超过 50 个字符。")]
        public string? ComponentCode { get; set; }

        /// <summary>
        /// 物料名称（BOM 子物料名称）。
        /// </summary>
        [StringLength(200, ErrorMessage = "物料名称不能超过 200 个字符。")]
        public string? ComponentName { get; set; }

        /// <summary>
        /// 物料描述（BOM 子物料说明）。
        /// </summary>
        public string? ComponentDescription { get; set; }

        /// <summary>
        /// 用量（生产一个产品需要的数量）。
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// 损耗率百分比（生产损耗比例）。
        /// </summary>
        public decimal LossRatePercent { get; set; }

        /// <summary>
        /// 用量及损耗（含损耗后的需求数量）。
        /// </summary>
        public decimal QuantityWithLoss { get; set; }

        /// <summary>
        /// 单位（BOM 子物料计量单位）。
        /// </summary>
        public string? Unit { get; set; } = "部";

        /// <summary>
        /// 成本单价（子物料成本价）。
        /// </summary>
        public decimal CostPrice { get; set; }

        /// <summary>
        /// 成本金额（用量及损耗乘以成本单价）。
        /// </summary>
        public decimal CostAmount { get; set; }

        /// <summary>
        /// 备注（BOM 明细补充说明）。
        /// </summary>
        public string? Remarks { get; set; }
    }
}
