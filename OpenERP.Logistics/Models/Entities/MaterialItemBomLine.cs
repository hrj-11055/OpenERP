using System.ComponentModel.DataAnnotations;

namespace OpenERP.Logistics.Models.Entities
{
    /// <summary>
    /// 产品 BOM 明细表（记录一个产品使用的组成物料）。
    /// </summary>
    public class MaterialItemBomLine : BaseEntity
    {
        /// <summary>
        /// 共享资料主表ID（对应 BD_ItemMaster 主表实体）。
        /// </summary>
        public int MaterialItemId { get; set; }

        /// <summary>
        /// 子物料编号（BOM 中被使用的物料编码）。
        /// </summary>
        [Required]
        [StringLength(50)]
        public string ComponentCode { get; set; } = string.Empty;

        /// <summary>
        /// 子物料名称（BOM 中被使用的物料名称）。
        /// </summary>
        [Required]
        [StringLength(200)]
        public string ComponentName { get; set; } = string.Empty;

        /// <summary>
        /// 子物料描述（用于展示物料用途或规格说明）。
        /// </summary>
        [StringLength(300)]
        public string? ComponentDescription { get; set; }

        /// <summary>
        /// 用量（生产一个产品需要的子物料数量）。
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// 损耗率百分比（生产损耗比例）。
        /// </summary>
        public decimal LossRatePercent { get; set; }

        /// <summary>
        /// 用量及损耗（用量加损耗后的计划需求数量）。
        /// </summary>
        public decimal QuantityWithLoss { get; set; }

        /// <summary>
        /// 单位（子物料计量单位）。
        /// </summary>
        [StringLength(30)]
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
        [StringLength(500)]
        public string? Remarks { get; set; }

        /// <summary>
        /// 排序号（BOM 明细显示顺序）。
        /// </summary>
        public int SortOrder { get; set; }

        /// <summary>
        /// 共享资料主表导航属性（对应 BD_ItemMaster 主表实体）。
        /// </summary>
        public MaterialItem? MaterialItem { get; set; }
    }
}
