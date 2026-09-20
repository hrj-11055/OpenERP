using System.ComponentModel.DataAnnotations;

namespace OpenERP.Logistics.Models.Entities
{
    /// <summary>
    /// 产品售价设置明细表（记录不同价格类别下的产品售价）。
    /// </summary>
    public class MaterialItemPriceLine : BaseEntity
    {
        /// <summary>
        /// 共享资料主表ID（对应 BD_ItemMaster 主表实体）。
        /// </summary>
        public int MaterialItemId { get; set; }

        /// <summary>
        /// 价格类别（例如中国区价格表、华东区价格表）。
        /// </summary>
        [Required]
        [StringLength(100)]
        public string PriceCategory { get; set; } = string.Empty;

        /// <summary>
        /// 数量（该价格适用的数量阶梯）。
        /// </summary>
        public decimal Quantity { get; set; } = 1M;

        /// <summary>
        /// 价格（该价格类别下的产品售价）。
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// 货币（售价币种）。
        /// </summary>
        [StringLength(20)]
        public string Currency { get; set; } = "RMB";

        /// <summary>
        /// 排序号（售价明细显示顺序）。
        /// </summary>
        public int SortOrder { get; set; }

        /// <summary>
        /// 共享资料主表导航属性（对应 BD_ItemMaster 主表实体）。
        /// </summary>
        public MaterialItem? MaterialItem { get; set; }
    }
}
